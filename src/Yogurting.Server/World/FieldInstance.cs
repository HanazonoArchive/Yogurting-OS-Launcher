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

            // 4. Field Monsters (0x796E + 0x7969)
            await SpawnMonstersAsync(session, gameDb);
        }

        /// <summary>
        /// Validates that every point along the straight line from (fromX, fromY) to (toX, toY) is walkable,
        /// not just the destination tile. Prevents monsters from wandering/warping straight through thin walls
        /// or obstacles that sit between their current position and a randomly chosen destination.
        /// </summary>
        private bool IsPathWalkable(float fromX, float fromY, float toX, float toY)
        {
            float dx = toX - fromX;
            float dy = toY - fromY;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist <= 0.01f)
            {
                return MapGridManager.IsWalkable(FieldId, toX, toY);
            }

            // Sample roughly every half-tile along the path so no obstacle can be skipped between samples.
            int steps = Math.Max(1, (int)MathF.Ceiling(dist / 0.5f));
            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                float x = fromX + dx * t;
                float y = fromY + dy * t;
                if (!MapGridManager.IsWalkable(FieldId, x, y))
                {
                    return false;
                }
            }
            return true;
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
                            mon.NextWanderInterval = Random.Shared.Next(16, 25); // 4.0 - 6.0s pacing (Delphi 150 frames)

                            byte[] respawnNtf = YogurtingPackets.MakeGameMonInfoNtf(mon);
                            _ = BroadcastToAreaAsync(respawnNtf, mon.X, mon.Y, 35f);
                            Logger.Debug($"[FieldServer] '{mon.Name}' (ID {mon.EntityId}) respawned at ({mon.SpawnX}, {mon.SpawnY}) in Field {FieldId}.");
                        }
                        continue;
                    }

                    switch (mon.State)
                    {
                        // =========================================================================
                        // State 0: stWait (Idle / Anchor Wait) - Delphi _Unit49.pas:18360-18420
                        // =========================================================================
                        case MonsterState.Wait:
                        {
                            // 1. Pursue if monster was targeted/attacked by a player (retaliatory aggro)
                            PlayerSessionState? target = null;
                            if (mon.TargetPlayerId > 0)
                            {
                                target = playersList.Find(p => (p.Player.CharacterId == mon.TargetPlayerId || p.Player.CharaId == mon.TargetPlayerId) && p.Player.CurrentHp > 0 && p.PendingWarpFieldId == 0);
                            }

                            if (target != null)
                            {
                                mon.State = MonsterState.Chase;
                                mon.Frame = 0;
                                break;
                            }
                            else
                            {
                                mon.TargetPlayerId = 0;
                            }

                            mon.Frame++;

                            // 2. Peaceful Idle Wandering with 8-tile radius around spawn anchor (_Unit49.pas:0060C61A)
                            if (mon.Frame >= (uint)mon.NextWanderInterval)
                            {
                                mon.Frame = 0;
                                mon.NextWanderInterval = Random.Shared.Next(16, 25); // 4.0 - 6.25s (150 frames in Delphi)

                                float chosenDestX = mon.SpawnX;
                                float chosenDestY = mon.SpawnY;
                                bool foundWalkable = false;

                                for (int attempt = 0; attempt < 8; attempt++)
                                {
                                    // Delphi _Unit49.pas:0060C61A RandomRange(-8, 8) around FBasePoint
                                    float offX = Random.Shared.Next(-8, 9);
                                    float offY = Random.Shared.Next(-8, 9);
                                    float tryX = mon.SpawnX + offX;
                                    float tryY = mon.SpawnY + offY;

                                    if (((int)tryX != (int)mon.X || (int)tryY != (int)mon.Y) && IsPathWalkable(mon.X, mon.Y, tryX, tryY))
                                    {
                                        chosenDestX = tryX;
                                        chosenDestY = tryY;
                                        foundWalkable = true;
                                        break;
                                    }
                                }

                                if (foundWalkable)
                                {
                                    int curX = (int)mon.X;
                                    int curY = (int)mon.Y;
                                    int tX = (int)chosenDestX;
                                    int tY = (int)chosenDestY;

                                    if (curX != tX || curY != tY)
                                    {
                                        float dx = chosenDestX - mon.X;
                                        float dy = chosenDestY - mon.Y;
                                        float walkLen = MathF.Sqrt(dx * dx + dy * dy);

                                        mon.StartX = mon.X;
                                        mon.StartY = mon.Y;
                                        mon.DestX = chosenDestX;
                                        mon.DestY = chosenDestY;
                                        mon.WalkLength = walkLen;
                                        mon.MoveMotion = 1;      // Motion 1: Walk
                                        mon.MoveSpeedRate = 80;  // 80%: 0.6 tiles/s -> 0.15 tiles per 250ms tick
                                        mon.DeltaX = (dx / Math.Max(0.01f, walkLen)) * 0.15f;
                                        mon.DeltaY = (dy / Math.Max(0.01f, walkLen)) * 0.15f;
                                        mon.State = MonsterState.Walk;
                                        mon.Frame = 0;

                                        byte[] moveNtf = YogurtingPackets.MakeGameMonMoveNtf(mon.EntityId, curX, curY, tX, tY, mon.MoveMotion, mon.MoveSpeedRate);
                                        _ = BroadcastToAreaAsync(moveNtf, mon.X, mon.Y, 30f);
                                    }
                                }
                            }
                            break;
                        }

                        // =========================================================================
                        // State 1: stWalk (Gradual Vector Stepping) - Delphi _Unit49.pas:18420-18520
                        // =========================================================================
                        case MonsterState.Walk:
                        {
                            // 1. Immediately interrupt walk into Chase if attacked
                            PlayerSessionState? target = null;
                            if (mon.TargetPlayerId > 0)
                            {
                                target = playersList.Find(p => (p.Player.CharacterId == mon.TargetPlayerId || p.Player.CharaId == mon.TargetPlayerId) && p.Player.CurrentHp > 0 && p.PendingWarpFieldId == 0);
                            }

                            if (target != null)
                            {
                                mon.State = MonsterState.Chase;
                                mon.Frame = 0;
                                break;
                            }
                            else
                            {
                                mon.TargetPlayerId = 0;
                            }

                            // 2. Advance coordinates incrementally along Delta vector (_Unit49.pas:0060C6BC)
                            mon.Frame++;
                            float nextX = mon.StartX + mon.DeltaX * mon.Frame;
                            float nextY = mon.StartY + mon.DeltaY * mon.Frame;

                            if (IsPathWalkable(mon.X, mon.Y, nextX, nextY))
                            {
                                mon.X = nextX;
                                mon.Y = nextY;
                            }

                            float remainingDist = MathF.Sqrt(MathF.Pow(mon.DestX - mon.X, 2) + MathF.Pow(mon.DestY - mon.Y, 2));
                            if (remainingDist <= 0.25f || (mon.Frame * 0.15f) >= mon.WalkLength)
                            {
                                // Destination reached, settle into Wait
                                mon.X = mon.DestX;
                                mon.Y = mon.DestY;
                                mon.State = MonsterState.Wait;
                                mon.Frame = 0;
                                mon.NextWanderInterval = Random.Shared.Next(16, 25);
                            }
                            break;
                        }

                        // =========================================================================
                        // State 2: stChase (Target Pursuit & Leash) - Delphi _Unit49.pas:18550-18650
                        // =========================================================================
                        case MonsterState.Chase:
                        {
                            PlayerSessionState? target = null;
                            if (mon.TargetPlayerId > 0)
                            {
                                target = playersList.Find(p => (p.Player.CharacterId == mon.TargetPlayerId || p.Player.CharaId == mon.TargetPlayerId) && p.Player.CurrentHp > 0 && p.PendingWarpFieldId == 0);
                            }

                            if (target == null)
                            {
                                // Target disconnected, dead, or left field: release ownership ring (0x7A01)
                                if (mon.TargetPlayerId > 0)
                                {
                                    byte[] lostNtf = YogurtingPackets.MakeGameMonsterOwnershipLostNtf(mon.EntityId);
                                    _ = BroadcastToAreaAsync(lostNtf, mon.X, mon.Y, 35f);
                                }

                                mon.TargetPlayerId = 0;
                                mon.State = MonsterState.Wait;
                                mon.Frame = 0;
                                if (mon.CurrentHp < mon.MaxHp)
                                {
                                    mon.CurrentHp = mon.MaxHp;
                                    byte[] hpNtf = YogurtingPackets.MakeGameMonHpInfoNtf(mon.EntityId, mon.CurrentHp, mon.MaxHp);
                                    _ = BroadcastToAreaAsync(hpNtf, mon.X, mon.Y, 35f);
                                }
                                break;
                            }

                            float pX = (float)target.Player.Position.X;
                            float pY = (float)target.Player.Position.Y;
                            float spawnDx = MathF.Abs(pX - mon.SpawnX);
                            float spawnDy = MathF.Abs(pY - mon.SpawnY);

                            // Authentic Delphi Leash: 16 tiles from Spawn Anchor FBasePoint (_Unit49.pas:0060C394 / 0060C3CC)
                            if (spawnDx >= 16.0f || spawnDy >= 16.0f)
                            {
                                // 1. Broadcast Ownership Lost (0x7A01) to remove client green lock ring (_Unit49.pas:0060C875)
                                byte[] lostNtf = YogurtingPackets.MakeGameMonsterOwnershipLostNtf(mon.EntityId);
                                _ = BroadcastToAreaAsync(lostNtf, mon.X, mon.Y, 35f);

                                // 2. Restore HP to full and broadcast 0x79E8 (_Unit49.pas:0060C895)
                                mon.TargetPlayerId = 0;
                                mon.CurrentHp = mon.MaxHp;
                                mon.State = MonsterState.Wait;
                                mon.Frame = 0;
                                mon.NextWanderInterval = 20;

                                bool canWalkToSpawn = IsPathWalkable(mon.X, mon.Y, mon.SpawnX, mon.SpawnY);
                                byte[] resetMoveNtf = canWalkToSpawn
                                    ? YogurtingPackets.MakeGameMonMoveNtf(mon.EntityId, (int)mon.X, (int)mon.Y, (int)mon.SpawnX, (int)mon.SpawnY, 1, 80)
                                    : YogurtingPackets.MakeGameMonMoveNtf(mon.EntityId, (int)mon.SpawnX, (int)mon.SpawnY, (int)mon.SpawnX, (int)mon.SpawnY, 0, 80);
                                byte[] hpNtf = YogurtingPackets.MakeGameMonHpInfoNtf(mon.EntityId, mon.CurrentHp, mon.MaxHp);
                                mon.X = mon.SpawnX;
                                mon.Y = mon.SpawnY;
                                _ = BroadcastToAreaAsync(resetMoveNtf, mon.SpawnX, mon.SpawnY, 35f);
                                _ = BroadcastToAreaAsync(hpNtf, mon.SpawnX, mon.SpawnY, 35f);
                                break;
                            }

                            float dx = pX - mon.X;
                            float dy = pY - mon.Y;

                            // Chebyshev Melee Combat Distance: adjacent 8 tiles (_Unit49.pas:0060CACB / 0060CAE9 abs(dx) < 2 && abs(dy) < 2)
                            if (MathF.Abs(dx) < 1.9f && MathF.Abs(dy) < 1.9f)
                            {
                                mon.State = MonsterState.Attack;
                                mon.Frame = 0;
                                break;
                            }

                            // Run Speed (Delphi 2.0 tiles/s -> 0.5f tiles per 250ms tick at Motion 2: _Unit49.pas:0060C15B)
                            float dist = MathF.Sqrt(dx * dx + dy * dy);
                            float dirX = dx / Math.Max(0.01f, dist);
                            float dirY = dy / Math.Max(0.01f, dist);
                            float step = MathF.Min(MathF.Max(0.1f, dist - 1.0f), 0.5f);

                            float nextChaseX = mon.X + dirX * step;
                            float nextChaseY = mon.Y + dirY * step;

                            if (IsPathWalkable(mon.X, mon.Y, nextChaseX, nextChaseY))
                            {
                                mon.X = nextChaseX;
                                mon.Y = nextChaseY;
                            }
                            else if (IsPathWalkable(mon.X, mon.Y, nextChaseX, mon.Y))
                            {
                                mon.X = nextChaseX;
                            }
                            else if (IsPathWalkable(mon.X, mon.Y, mon.X, nextChaseY))
                            {
                                mon.Y = nextChaseY;
                            }

                            mon.DirX = (int)(dirX * 100);
                            mon.DirY = (int)(dirY * 100);

                            // Send Chase Movement packet with Motion 2 (Run animation per Delphi 0060C15B)
                            if (mon.Frame >= 8 || MathF.Abs(mon.DestX - pX) > 1.2f || MathF.Abs(mon.DestY - pY) > 1.2f)
                            {
                                mon.Frame = 0;
                                mon.DestX = pX;
                                mon.DestY = pY;
                                byte[] moveNtf = YogurtingPackets.MakeGameMonMoveNtf(mon.EntityId, (int)mon.X, (int)mon.Y, (int)pX, (int)pY, 2, 100);
                                _ = BroadcastToAreaAsync(moveNtf, mon.X, mon.Y, 35f);
                            }
                            else
                            {
                                mon.Frame++;
                            }
                            break;
                        }

                        // =========================================================================
                        // State 3: stAttack (Melee Engagement) - Delphi _Unit49.pas:18780-18900
                        // =========================================================================
                        case MonsterState.Attack:
                        {
                            PlayerSessionState? target = null;
                            if (mon.TargetPlayerId > 0)
                            {
                                target = playersList.Find(p => (p.Player.CharacterId == mon.TargetPlayerId || p.Player.CharaId == mon.TargetPlayerId) && p.Player.CurrentHp > 0 && p.PendingWarpFieldId == 0);
                            }

                            if (target == null)
                            {
                                if (mon.TargetPlayerId > 0)
                                {
                                    byte[] lostNtf = YogurtingPackets.MakeGameMonsterOwnershipLostNtf(mon.EntityId);
                                    _ = BroadcastToAreaAsync(lostNtf, mon.X, mon.Y, 35f);
                                }

                                mon.TargetPlayerId = 0;
                                mon.State = MonsterState.Wait;
                                mon.Frame = 0;
                                break;
                            }

                            float pX = (float)target.Player.Position.X;
                            float pY = (float)target.Player.Position.Y;
                            float spawnDx = MathF.Abs(pX - mon.SpawnX);
                            float spawnDy = MathF.Abs(pY - mon.SpawnY);

                            // Leash check while in attack stance
                            if (spawnDx >= 16.0f || spawnDy >= 16.0f)
                            {
                                byte[] lostNtf = YogurtingPackets.MakeGameMonsterOwnershipLostNtf(mon.EntityId);
                                _ = BroadcastToAreaAsync(lostNtf, mon.X, mon.Y, 35f);

                                mon.TargetPlayerId = 0;
                                mon.CurrentHp = mon.MaxHp;
                                mon.State = MonsterState.Wait;
                                mon.Frame = 0;
                                break;
                            }

                            float dx = pX - mon.X;
                            float dy = pY - mon.Y;

                            // If player moves out of adjacent melee range, immediately chase to close distance
                            if (MathF.Abs(dx) >= 1.9f || MathF.Abs(dy) >= 1.9f)
                            {
                                mon.State = MonsterState.Chase;
                                mon.Frame = 8;
                                break;
                            }

                            if ((DateTime.UtcNow - mon.LastAttackTime).TotalSeconds >= 2.0)
                            {
                                mon.LastAttackTime = DateTime.UtcNow;
                                target.Player.LastCombatTime = DateTime.UtcNow;
                                target.Player.HpRegainAccumulator = 0f;

                                int typeHit = 0;
                                int finalDmg = 0;

                                // Delphi _Unit49.pas:0060CBC3 Random(100) < 20 (20% miss chance)
                                if (Random.Shared.Next(0, 100) < 20)
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
                                    // Delphi _Unit49.pas:0060CC52 Minimum 1 damage
                                    finalDmg = Math.Max(1, rawDmg - defMitigation);
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

                                    // Broadcast 0x7A01 Ownership Lost on player defeat (_Unit49.pas:0060CA1C)
                                    byte[] lostNtf = YogurtingPackets.MakeGameMonsterOwnershipLostNtf(mon.EntityId);
                                    _ = BroadcastToAreaAsync(lostNtf, mon.X, mon.Y, 35f);

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
