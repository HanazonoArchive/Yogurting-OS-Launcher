using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Yogurting.Core.Logging;
using Yogurting.Core.Models;
using Yogurting.Core.Network;
using Yogurting.Data.Loaders;

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

            // 1. Episode Gates, Hairdressers, and Campus Terminal Objects (0x521B + 0x5227)
            foreach (var obj in fieldDef.TerminalObjects)
            {
                await session.SendAsync(YogurtingPackets.MakeObjectCreateNtf(
                    obj.ObjectId, obj.ObjectType, obj.SubId, obj.CliId, obj.ShellId, obj.X, obj.Y, (byte)obj.Dir, 1, 1));
                await session.SendAsync(YogurtingPackets.MakeGameObjectStateNtf(obj.ObjectId, 1));
            }

            // 2. Visual and Campus NPCs (0x7942)
            foreach (var npc in fieldDef.Npcs)
            {
                await session.SendAsync(YogurtingPackets.MakeGameNpcCreateNtf(
                    npc.NpcId, npc.ShellType, npc.X, npc.Y, npc.Dir));
            }

            // 3. Campus Warp Portals & Gates (0x795C) - Driven dynamically from Map XML definition
            foreach (var gate in fieldDef.WarpGates)
            {
                await session.SendAsync(YogurtingPackets.MakeGameWarpGateSpawnNtf(
                    gate.Id, gate.X, gate.Y, gate.Shell, gate.CliId, gate.Dir, gate.DestFieldId));
            }
        }

        public Task BroadcastToAreaAsync(byte[] packet, float x, float y, float radius = 30f)
        {
            float r2 = radius * radius;
            var tasks = new List<Task>();
            foreach (var pState in _players.Values)
            {
                var p = pState.Player;
                if (p == null) continue;
                float dx = (float)p.Position.X - x;
                float dy = (float)p.Position.Y - y;
                if (dx * dx + dy * dy <= r2)
                {
                    tasks.Add(pState.Session.SendAsync(packet));
                }
            }
            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Spawns field monsters matching Quartet's exact 0x796E (MonInfo) + 0x7969 (MonMove) sequence for nearby Area of Interest.
        /// </summary>
        public async Task SpawnMonstersAsync(ClientSession session, Yogurting.Data.Loaders.GameDatabase? gameDb = null)
        {
            if (gameDb == null || !gameDb.Fields.TryGetValue(FieldId, out var fieldDef) || fieldDef.Monsters.Count == 0)
            {
                return;
            }

            var player = _players.Values.FirstOrDefault(p => p.Session.Id == session.Id)?.Player;
            float px = player != null ? (float)player.Position.X : 50f;
            float py = player != null ? (float)player.Position.Y : 50f;

            FieldMonster[] monstersSnapshot;
            lock (fieldDef.Monsters)
            {
                monstersSnapshot = fieldDef.Monsters.ToArray();
            }

            foreach (var mon in monstersSnapshot)
            {
                if (!mon.IsDead)
                {
                    // 1. Monster Spawn Info (0x796E - 32 bytes)
                    await session.SendAsync(YogurtingPackets.MakeGameMonInfoNtf(mon));

                    // 2. Synchronize 3D position & initial motion (0x7969 - 20 bytes)
                    int curX = (int)mon.X;
                    int curY = (int)mon.Y;
                    int destX = (int)mon.DestX;
                    int destY = (int)mon.DestY;
                    await session.SendAsync(YogurtingPackets.MakeGameMonMoveNtf(mon.EntityId, curX, curY, destX, destY, mon.MoveMotion, mon.MoveSpeedRate));
                }
            }
        }

        /// <summary>
        /// Dedicated per-field lane execution loop.
        /// Handles local monster AI, wall collision with map.db, and out-of-combat HP regain for players in this field.
        /// </summary>
        public void Update(Yogurting.Data.Loaders.GameDatabase gameDb)
        {
            if (_players.IsEmpty) return;

            var playersList = _players.Values.ToList();
            if (playersList.Count == 0) return;

            // 1. Natural HP Regain tick for active players in this field (only when out of combat for >= 30 seconds)
            foreach (var pState in playersList)
            {
                var p = pState.Player;
                if (p != null && p.CurrentHp > 0 && p.CurrentHp < p.MaxHp)
                {
                    if ((DateTime.UtcNow - p.LastCombatTime).TotalSeconds >= 30.0)
                    {
                        p.HpRegainAccumulator += 0.25f;
                        if (p.HpRegainAccumulator >= 3.0f)
                        {
                            p.HpRegainAccumulator = 0f;
                            bool isHunt = gameDb.Fields.TryGetValue(FieldId, out var f) && f.IsHuntField;
                            int regainAmount = isHunt ? 2 : 5;
                            p.CurrentHp = Math.Min(p.MaxHp, p.CurrentHp + regainAmount);
                            _ = pState.Session.SendAsync(YogurtingPackets.MakeGameSetHpNtf((ushort)p.CurrentHp));
                        }
                    }
                }
            }

            if (!gameDb.Fields.TryGetValue(FieldId, out var fieldDef) || fieldDef.Monsters.Count == 0)
            {
                return;
            }

            lock (fieldDef.Monsters)
            {
                foreach (var mon in fieldDef.Monsters)
                {
                    // 1. Authoritative Monster Respawn Loop (_Unit49.pas:1060-1065)
                    if (mon.IsDead)
                    {
                        if ((DateTime.UtcNow - mon.DeathTime).TotalSeconds >= mon.RespawnSeconds)
                        {
                            mon.Respawn();
                            mon.Frame = 0;
                            mon.NextWanderInterval = Random.Shared.Next(15, 35);
                            byte[] respawnNtf = YogurtingPackets.MakeGameMonInfoNtf(mon);
                            _ = BroadcastToAreaAsync(respawnNtf, mon.X, mon.Y, 35f);
                            byte[] moveNtf = YogurtingPackets.MakeGameMonMoveNtf(mon.EntityId, (int)mon.X, (int)mon.Y, (int)mon.DestX, (int)mon.DestY, mon.MoveMotion, mon.MoveSpeedRate);
                            _ = BroadcastToAreaAsync(moveNtf, mon.X, mon.Y, 35f);
                            Logger.Debug($"[FieldServer] '{mon.Name}' (ID {mon.EntityId}) respawned at ({mon.X}, {mon.Y}) in Field {FieldId}.");
                        }
                        continue;
                    }

                    mon.Frame++;

                    switch (mon.State)
                    {
                        case MonsterState.Wait:
                        {
                            // 1. Only pursue if monster was explicitly targeted/attacked by a player (retaliatory aggro)
                            PlayerSessionState? target = null;
                            if (mon.TargetPlayerId > 0)
                            {
                                target = playersList.Find(p => (p.Player.CharacterId == mon.TargetPlayerId || p.Player.CharaId == mon.TargetPlayerId) && p.Player.CurrentHp > 0 && p.PendingWarpFieldId == 0);
                            }

                            if (target != null)
                            {
                                mon.State = MonsterState.Chase;
                                mon.Frame = 6;
                                break;
                            }
                            else
                            {
                                mon.TargetPlayerId = 0;
                            }

                            // 2. Peaceful Idle Wandering with map.db wall collision (_Unit49.pas:14081-14093)
                            if (mon.Frame >= mon.NextWanderInterval)
                            {
                                mon.Frame = 0;
                                mon.NextWanderInterval = Random.Shared.Next(15, 35);

                                float destX = mon.SpawnX;
                                float destY = mon.SpawnY;
                                bool foundWalkable = false;

                                for (int attempt = 0; attempt < 6; attempt++)
                                {
                                    float offX = Random.Shared.Next(-4, 5); // 4-tile territory tether around spawn anchor
                                    float offY = Random.Shared.Next(-4, 5);
                                    float tryX = mon.SpawnX + offX;
                                    float tryY = mon.SpawnY + offY;

                                    if (MapGridManager.IsWalkable(FieldId, tryX, tryY))
                                    {
                                        destX = tryX;
                                        destY = tryY;
                                        foundWalkable = true;
                                        break;
                                    }
                                }

                                if (foundWalkable)
                                {
                                    int curX = (int)mon.X;
                                    int curY = (int)mon.Y;
                                    int tX = (int)destX;
                                    int tY = (int)destY;

                                    if (curX != tX || curY != tY)
                                    {
                                        mon.StartX = mon.X;
                                        mon.StartY = mon.Y;
                                        mon.DestX = destX;
                                        mon.DestY = destY;
                                        mon.X = destX;
                                        mon.Y = destY;
                                        mon.MoveMotion = 1;
                                        mon.MoveSpeedRate = 80;

                                        byte[] moveNtf = YogurtingPackets.MakeGameMonMoveNtf(mon.EntityId, curX, curY, tX, tY, mon.MoveMotion, mon.MoveSpeedRate);
                                        _ = BroadcastToAreaAsync(moveNtf, mon.X, mon.Y, 30f);
                                    }
                                }
                            }
                            break;
                        }

                        case MonsterState.Chase:
                        {
                            PlayerSessionState? target = null;
                            if (mon.TargetPlayerId > 0)
                            {
                                target = playersList.Find(p => (p.Player.CharacterId == mon.TargetPlayerId || p.Player.CharaId == mon.TargetPlayerId) && p.Player.CurrentHp > 0 && p.PendingWarpFieldId == 0);
                            }

                            if (target == null)
                            {
                                mon.TargetPlayerId = 0;
                                mon.State = MonsterState.Wait;
                                mon.Frame = 0;
                                break;
                            }

                            float pX = (float)target.Player.Position.X;
                            float pY = (float)target.Player.Position.Y;
                            float dx = pX - mon.X;
                            float dy = pY - mon.Y;
                            float dist = MathF.Sqrt(dx * dx + dy * dy);

                            if (dist > 30.0f)
                            {
                                mon.TargetPlayerId = 0;
                                mon.CurrentHp = mon.MaxHp;
                                mon.State = MonsterState.Wait;
                                mon.Frame = 0;

                                byte[] resetMoveNtf = YogurtingPackets.MakeGameMonMoveNtf(mon.EntityId, (int)mon.X, (int)mon.Y, (int)mon.SpawnX, (int)mon.SpawnY, 1, 80);
                                mon.X = mon.SpawnX;
                                mon.Y = mon.SpawnY;
                                _ = BroadcastAsync(resetMoveNtf);
                                break;
                            }

                            // Strict Melee Distance (Delphi _Unit49.pas:0060CACB / 0060CAE9 abs(dx) < 2 && abs(dy) < 2)
                            if (dist <= 1.4f)
                            {
                                mon.State = MonsterState.Attack;
                                mon.Frame = 0;
                                break;
                            }

                            float moveSpeed = 0.9f;
                            float dirX = dx / Math.Max(0.01f, dist);
                            float dirY = dy / Math.Max(0.01f, dist);
                            float step = MathF.Min(MathF.Max(0.1f, dist - 1.0f), moveSpeed);

                            float nextX = mon.X + dirX * step;
                            float nextY = mon.Y + dirY * step;

                            if (MapGridManager.IsWalkable(FieldId, nextX, nextY))
                            {
                                mon.X = nextX;
                                mon.Y = nextY;
                            }
                            else if (MapGridManager.IsWalkable(FieldId, nextX, mon.Y))
                            {
                                mon.X = nextX; // Slide along X axis
                            }
                            else if (MapGridManager.IsWalkable(FieldId, mon.X, nextY))
                            {
                                mon.Y = nextY; // Slide along Y axis
                            }
                            else
                            {
                                // Slide around diagonal obstacles
                                float altX = mon.X + MathF.Sign(dirX) * 0.5f;
                                float altY = mon.Y + MathF.Sign(dirY) * 0.5f;
                                if (MapGridManager.IsWalkable(FieldId, altX, mon.Y)) mon.X = altX;
                                else if (MapGridManager.IsWalkable(FieldId, mon.X, altY)) mon.Y = altY;
                            }

                            mon.DirX = (int)(dirX * 100);
                            mon.DirY = (int)(dirY * 100);

                            if (mon.Frame >= 2 || MathF.Abs(mon.DestX - pX) > 1.2f || MathF.Abs(mon.DestY - pY) > 1.2f)
                            {
                                mon.Frame = 0;
                                mon.DestX = pX;
                                mon.DestY = pY;
                                byte[] moveNtf = YogurtingPackets.MakeGameMonMoveNtf(mon.EntityId, (int)mon.X, (int)mon.Y, (int)pX, (int)pY, 1, 100);
                                _ = BroadcastToAreaAsync(moveNtf, mon.X, mon.Y, 35f);
                            }
                            else
                            {
                                mon.Frame++;
                            }
                            break;
                        }

                        case MonsterState.Attack:
                        {
                            PlayerSessionState? target = null;
                            if (mon.TargetPlayerId > 0)
                            {
                                target = playersList.Find(p => (p.Player.CharacterId == mon.TargetPlayerId || p.Player.CharaId == mon.TargetPlayerId) && p.Player.CurrentHp > 0 && p.PendingWarpFieldId == 0);
                            }

                            if (target == null)
                            {
                                mon.TargetPlayerId = 0;
                                mon.State = MonsterState.Wait;
                                mon.Frame = 0;
                                break;
                            }

                            float pX = (float)target.Player.Position.X;
                            float pY = (float)target.Player.Position.Y;
                            float dx = pX - mon.X;
                            float dy = pY - mon.Y;
                            float dist = MathF.Sqrt(dx * dx + dy * dy);

                            // If player moves out of adjacent melee range, immediately chase to close the distance
                            if (dist > 1.6f)
                            {
                                mon.State = MonsterState.Chase;
                                mon.Frame = 6;
                                break;
                            }

                            if ((DateTime.UtcNow - mon.LastAttackTime).TotalSeconds >= 2.0)
                            {
                                mon.LastAttackTime = DateTime.UtcNow;
                                target.Player.LastCombatTime = DateTime.UtcNow;
                                target.Player.HpRegainAccumulator = 0f;

                                int typeHit = 0;
                                int finalDmg = 0;

                                if (Random.Shared.Next(0, 100) < 10)
                                {
                                    typeHit = 2; // Miss!
                                    finalDmg = 0;
                                }
                                else
                                {
                                    int rawDmg;
                                    if (mon.AttackPower > 0)
                                    {
                                        int variance = Math.Max(1, mon.AttackPower / 10);
                                        rawDmg = mon.AttackPower + Random.Shared.Next(-variance, variance + 1);
                                    }
                                    else
                                    {
                                        int variance = Math.Max(1, mon.MaxHp / 100);
                                        rawDmg = Math.Max(1, (mon.MaxHp / 7) + Random.Shared.Next(-variance, variance + 1));
                                    }

                                    int defMitigation = target.Player.Defense / 200;
                                    finalDmg = Math.Max(2, rawDmg - defMitigation);
                                    target.Player.CurrentHp = Math.Max(0, target.Player.CurrentHp - finalDmg);
                                }

                                byte[] monAtkNtf = YogurtingPackets.MakeGameMonAttackNtf(
                                    mon.EntityId,
                                    (int)mon.X,
                                    (int)mon.Y,
                                    target.Player.CharaId,
                                    finalDmg,
                                    mon.MotionType,
                                    (byte)typeHit);

                                _ = BroadcastToAreaAsync(monAtkNtf, mon.X, mon.Y, 35f);
                                _ = target.Session.SendAsync(YogurtingPackets.MakeGameSetHpNtf((ushort)target.Player.CurrentHp));

                                if (target.Player.CurrentHp <= 0)
                                {
                                    Logger.Info($"[FieldServer] '{target.Player.CharacterName}' was knocked down by '{mon.Name}'!");
                                    byte[] dieNtf = YogurtingPackets.MakeGameDieCharNtf(
                                        target.Player.CharaId,
                                        (int)target.Player.Position.X,
                                        (int)target.Player.Position.Y);
                                    _ = target.Session.SendAsync(dieNtf);
                                    _ = BroadcastAsync(dieNtf);

                                    foreach (var m in fieldDef.Monsters)
                                    {
                                        if (m.TargetPlayerId == target.Player.CharaId)
                                        {
                                            m.TargetPlayerId = 0;
                                            m.State = MonsterState.Wait;
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
            }
        }
    }
}
