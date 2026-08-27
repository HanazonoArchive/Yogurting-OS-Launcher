using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Yogurting.Core.Logging;
using Yogurting.Core.Models;
using Yogurting.Core.Network;

namespace Yogurting.Server.World
{
    /// <summary>
    /// Represents a live, active campus or dungeon zone (e.g., Estiva Outdoors, So-il Athletic Ground).
    /// Manages players, NPCs, interactive props, and warp portals present on this map.
    /// </summary>
    public sealed class FieldInstance
    {
        public int FieldId { get; }
        public string Name { get; }

        private readonly ConcurrentDictionary<int, PlayerSessionState> _players = new();

        public FieldInstance(int fieldId, string name = "")
        {
            FieldId = fieldId;
            Name = string.IsNullOrEmpty(name) ? $"Field_{fieldId}" : name;
        }

        public IReadOnlyCollection<PlayerSessionState> Players => _players.Values.ToList();

        public void AddPlayer(PlayerSessionState state)
        {
            _players[state.Player.CharaId] = state;
            Logger.Info($"[World] '{state.Player.CharacterName}' entered {Name} ({FieldId}). Active in zone: {_players.Count}");
        }

        public void RemovePlayer(int charaId)
        {
            if (_players.TryRemove(charaId, out var state))
            {
                Logger.Info($"[World] '{state.Player.CharacterName}' left {Name} ({FieldId}). Active in zone: {_players.Count}");
            }
        }

        /// <summary>
        /// Broadcasts raw packet bytes to all players present in this field instance (except the sender if specified).
        /// </summary>
        public async Task BroadcastAsync(byte[] packetData, int excludeCharaId = 0)
        {
            foreach (var player in _players.Values)
            {
                if (player.Player.CharaId != excludeCharaId && player.Session.IsConnected)
                {
                    try
                    {
                        await player.Session.SendAsync(packetData);
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Spawns all visual props, interactive NPCs, terminal kiosks, and warp portals dynamically
        /// from the game database score XML definitions (npc.xml and default.xml).
        /// </summary>
        public async Task SpawnFixedEntitiesAsync(ClientSession session, Yogurting.Data.Loaders.GameDatabase? gameDb = null)
        {
            if (gameDb == null || !gameDb.Fields.TryGetValue(FieldId, out var fieldDef))
            {
                return;
            }

            // 1. Episode Kiosks and Terminal Objects (0x521B + 0x5227)
            foreach (var obj in fieldDef.TerminalObjects)
            {
                float worldX = obj.X * 4.0f;
                float worldY = obj.Y * 4.0f;
                await session.SendAsync(YogurtingPackets.MakeObjectCreateNtf(
                    obj.ObjectId, obj.ObjectType, obj.SubId, obj.CliId, obj.ShellId, worldX, worldY, (byte)obj.Dir, 1, 1));
                await session.SendAsync(YogurtingPackets.MakeGameObjectStateNtf(obj.ObjectId, 1));
            }

            // 2. Visual and Campus NPCs (0x7942)
            foreach (var npc in fieldDef.Npcs)
            {
                await session.SendAsync(YogurtingPackets.MakeGameNpcCreateNtf(
                    npc.NpcId, npc.ShellType, npc.X, npc.Y, npc.Dir));
            }

            // 3. Campus Warp Portals & Gates (0x795C)
            foreach (var gate in fieldDef.WarpGates)
            {
                await session.SendAsync(YogurtingPackets.MakeGameWarpGateSpawnNtf(
                    gate.Id, gate.X, gate.Y, gate.Shell, gate.CliId, gate.Dir, gate.DestFieldId));
            }

            // 4. Field Monsters (Temporarily disabled for isolated mob field testing)
            // foreach (var mon in fieldDef.Monsters)
            // {
            //     if (!mon.IsDead)
            //     {
            //         await session.SendAsync(YogurtingPackets.MakeGameTriggerMobNtf(mon.EntityId));
            //         await session.SendAsync(YogurtingPackets.MakeGameMonInfoNtf(mon));
            //     }
            // }
        }
    }
}
