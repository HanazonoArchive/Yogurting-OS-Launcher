using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Yogurting.Core.Logging;
using Yogurting.Core.Models;
using Yogurting.Core.Network;
using Yogurting.Data.Loaders;
using Yogurting.Data.Repositories;
using Yogurting.Server.Handlers.Field;
using Yogurting.Server.Handlers.Npc;
using Yogurting.Server.World;

namespace Yogurting.Server.Handlers
{
    /// <summary>
    /// Master coordinator for 3D Campus Lobbies, Navigation, Synchronization, Equipment, NPCs, and Portals (Port 10002).
    /// Dispatches incoming packets to modular, domain-specific handlers.
    /// </summary>
    public sealed class FieldServerHandler
    {
        private readonly IAccountRepository _repository;
        private readonly WorldManager _worldManager;
        private readonly PacketDispatcher<PlayerSessionState> _dispatcher = new();
        private readonly ConcurrentDictionary<Guid, PlayerSessionState> _activeSessions = new();
        private int _nextEntityId = 1000;

        private readonly GameDatabase? _gameDb;
        private readonly System.Threading.Timer _monsterAiTimer;

        public WorldManager World => _worldManager;

        public FieldServerHandler(IAccountRepository repository, WorldManager worldManager, GameDatabase? gameDb = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _worldManager = worldManager ?? throw new ArgumentNullException(nameof(worldManager));
            _gameDb = gameDb;

            // Register modular domain handlers
            var equipHandlers = new EquipmentHandlers(BroadcastToFieldAsync, _repository, _gameDb);
            var movementHandlers = new MovementAndFieldHandlers(BroadcastToFieldAsync, SpawnCampusEntitiesAsync, _gameDb, _worldManager, _repository);
            var npcHandlers = new NpcAndDialogueHandlers(_gameDb, _repository, BroadcastToFieldAsync);
            var shopHandlers = new ShopHandlers(BroadcastToFieldAsync, _repository, _gameDb);
            var combatHandlers = new CombatHandlers(BroadcastToFieldAsync, _repository, _gameDb);

            _dispatcher.RegisterHandlers(equipHandlers);
            _dispatcher.RegisterHandlers(movementHandlers);
            _dispatcher.RegisterHandlers(npcHandlers);
            _dispatcher.RegisterHandlers(shopHandlers);
            _dispatcher.RegisterHandlers(combatHandlers);

            // Active Monster AI & Movement loop (ticks every 250ms for smooth 4Hz AI responsiveness)
            _monsterAiTimer = new System.Threading.Timer(OnMonsterAiTick, null, 1000, 250);
        }

        public Task HandleClientConnectedAsync(ClientSession session)
        {
            Logger.Info($"[FieldServer] TCP Connection from {session.RemoteEndPoint} established. Awaiting school enter handshake...");
            return Task.CompletedTask;
        }

        public async Task HandlePacketAsync(ClientSession session, byte[] packetData)
        {
            if (packetData == null || packetData.Length < 4) return;

            ushort opcode = packetData.Length >= 6 ? BitConverter.ToUInt16(packetData, 4) : (ushort)0;

            // 1. Initial Handshake & Ping
            if (opcode == (ushort)PacketOpcode.MsgCheckVersionNtf || opcode == 0x4E21 || opcode == 0x4E26)
            {
                await session.SendAsync(YogurtingPackets.MakeTimeNtf());
                return;
            }

            // 2. School Entry Handshake (0x5211 in Delphi 0x006BF574)
            if (opcode == 0x5211 || opcode == (ushort)PacketOpcode.MsgPingTimeReq)
            {
                await HandleSchoolEnterAsync(session, packetData);
                return;
            }

            if (opcode == 0x5213 || opcode == (ushort)PacketOpcode.MsgEnterScsReq)
            {
                // Delphi 0x006BF7AC: No-op acknowledgement
                return;
            }

            if (!_activeSessions.TryGetValue(session.Id, out var state))
            {
                var player = await _repository.GetByUsernameAsync(session.AccountId ?? "test") ?? new Player("test", "Hanazono");
                state = new PlayerSessionState(session, player, System.Threading.Interlocked.Increment(ref _nextEntityId));
                _activeSessions[session.Id] = state;
                _worldManager.GetOrCreateField(player.FieldId).AddPlayer(state);
            }

            state.LastPacketAt = DateTime.UtcNow;

            // Dispatch to registered domain handlers
            bool handled = await _dispatcher.DispatchAsync(state, opcode, packetData);
            if (!handled)
            {
                Logger.Debug($"[FieldServer] Unhandled Opcode 0x{opcode:X4} ({opcode}) from '{state.Player.CharacterName}'");
            }
        }

        public async Task HandleClientDisconnectedAsync(ClientSession session)
        {
            if (_activeSessions.TryRemove(session.Id, out var state))
            {
                Logger.Info($"[FieldServer] '{state.Player.CharacterName}' left the campus.");

                _worldManager.RemovePlayer(state);

                // Despawn this player for everyone else on the same map
                byte[] despawnPacket = YogurtingPackets.MakeObjectDestroyNtf(state.EntityId);
                var field = _worldManager.GetOrCreateField(state.Player.FieldId);
                await field.BroadcastAsync(despawnPacket, state.Player.CharaId);

                // Save character position and state to disk
                await _repository.SaveAsync(state.Player);
            }
        }

        private async Task HandleSchoolEnterAsync(ClientSession session, byte[] packetData)
        {
            int charaId = packetData.Length >= 10 ? BitConverter.ToInt32(packetData, 6) : 1;
            int authToken = packetData.Length >= 38 ? BitConverter.ToInt32(packetData, 34) : 0;

            Logger.Info($"[FieldServer] School Server Handshake (0x5211/0x5213) from {session.RemoteEndPoint} - CharaId={charaId}, AuthToken={authToken}");

            Player? player = null;
            if (authToken > 0)
            {
                player = await _repository.GetBySessionKeyAsync(authToken);
            }
            if (player == null && !string.IsNullOrEmpty(session.AccountId))
            {
                player = await _repository.GetByUsernameAsync(session.AccountId);
            }
            if (player == null)
            {
                player = await _repository.GetByUsernameAsync("test") ?? new Player("test", session.CharacterName ?? "Hanazono");
            }

            // Reset to campus plaza on login if previously saved in a temporary mob/hunt field or if coordinates are invalid
            bool wasSavedInHunt = _gameDb != null && _gameDb.Fields.TryGetValue(player.FieldId, out var savedFDef) && savedFDef.IsHuntField;
            if (wasSavedInHunt || player.FieldId <= 0 || player.Position.X <= 0 || player.Position.Y <= 0)
            {
                bool hasValidSave = player.SaveFieldId > 0 && (_gameDb == null || !_gameDb.Fields.TryGetValue(player.SaveFieldId, out var sf) || !sf.IsHuntField);
                if (hasValidSave && player.SavePosition.X > 0 && player.SavePosition.Y > 0)
                {
                    player.FieldId = player.SaveFieldId;
                    player.Position = new Position(player.SavePosition.X, player.SavePosition.Y, 0f);
                }
                else
                {
                    var spawn = Yogurting.Core.Models.StarterConfigLoader.GetSpawnPoint(player.School);
                    player.FieldId = spawn.FieldId;
                    player.Position = new Position(spawn.X, spawn.Y, 0f);
                    player.SaveFieldId = spawn.FieldId;
                    player.SavePosition = player.Position;
                }
            }

            session.AccountId = player.AccountId;
            session.CharacterName = player.CharacterName;

            int entityId = System.Threading.Interlocked.Increment(ref _nextEntityId);
            var state = new PlayerSessionState(session, player, entityId);
            _activeSessions[session.Id] = state;
            _worldManager.GetOrCreateField(player.FieldId).AddPlayer(state);

            // Execute exact packet sequence from Delphi TSchoolSession.sub_006BF574:
            // 1. TMsgTimeNtf (0x4E26)
            await session.SendAsync(YogurtingPackets.MakeTimeNtf());
            
            // 2. TMsgWorldTimeNtf (0x4E25) - Season 3 (Clear Standard Daylight), Clock 0 (Daylight)
            await session.SendAsync(YogurtingPackets.MakeWorldTimeNtf(3, 0));

            // 3. TMsgGameAtkMovChangeNtf (0x799F)
            await session.SendAsync(YogurtingPackets.MakeGameAtkMovChangeNtf(player.CharaId, player.AtkSpeedF, player.MoveSpeedF));

            // 4. TMsgEnterScsNtf (0x5212)
            await session.SendAsync(YogurtingPackets.MakeEnterScsNtf());

            // 5. TMsgGameCharInfoNtf (0x7952)
            await session.SendAsync(YogurtingPackets.MakeGameCharInfoNtf(player));

            // 6. TMsgGamePromoteInfoNtf (0x79B2)
            await session.SendAsync(YogurtingPackets.MakeGamePromoteInfoNtf(player.CharaId, player.Grade, 1));

            // 7. TMsgGameEquipTitleAns (0x79A3)
            await session.SendAsync(YogurtingPackets.MakeGameEquipTitleAns(player.CharaId, 0));

            // 8. TMsgGameFieldLoadingStartNtf (0x795A) - Triggers the loading screen in client!
            bool isHuntField = false;
            int monCount = 0;
            string fieldName = string.Empty;
            if (_gameDb != null && _gameDb.Fields.TryGetValue(player.FieldId, out var curF))
            {
                isHuntField = curF.IsHuntField;
                monCount = curF.Monsters.Count;
                fieldName = curF.Name;
            }
            await session.SendAsync(YogurtingPackets.MakeGameFieldLoadingStartNtf(player.FieldId, player.Position.X, player.Position.Y, isHuntField, monCount));

            // 9. TMsgGameCharaNameInfoNtf (0x7963) - Player Welcome Greeting (80B exact Delphi match)
            string welcomeGreeting = $"{player.CharacterName}さんこんにちは.";
            await session.SendAsync(YogurtingPackets.MakeGameCharaNameInfoNtf(-1, (int)player.School, 0, string.Empty, 0, null, welcomeGreeting));

            Logger.Info($"[FieldServer] '{player.CharacterName}' (Entity #{entityId}) field loading sequence dispatched for Field {player.FieldId} ({player.Position})! Awaiting map load (0x795B)...");
        }

        private async Task SendFieldInfoDoneNotifyAsync(PlayerSessionState state)
        {
            var session = state.Session;
            var player = state.Player;

            // 1. School Info (0x5264)
            await session.SendAsync(YogurtingPackets.MakeGameSchoolInfoNtf((int)player.School));

            // 2. Spawn Fixed NPCs & Campus Objects for this field
            await SpawnCampusEntitiesAsync(session, player.FieldId);

            // 3. Spawn Other Active Players for this client
            var field = _worldManager.GetOrCreateField(player.FieldId);
            foreach (var other in field.Players)
            {
                if (other.Session.Id != session.Id)
                {
                    // Spawn other player for new client
                    byte[] spawnOther = YogurtingPackets.MakeObjectCreateNtf(
                        other.EntityId, 1, 0, 0, 0,
                        other.Player.Position.X, other.Player.Position.Y, 0, 1, 1
                    );
                    await session.SendAsync(spawnOther);

                    // Spawn new client for other player
                    byte[] spawnMe = YogurtingPackets.MakeObjectCreateNtf(
                        state.EntityId, 1, 0, 0, 0,
                        player.Position.X, player.Position.Y, 0, 1, 1
                    );
                    await other.Session.SendAsync(spawnMe);
                }
            }

            // 4. TMsgGameFieldInfoDoneNtf (0x7956) - Reveals 3D campus!
            await session.SendAsync(YogurtingPackets.MakeGameFieldInfoDoneNtf());

            // 5. MsgGameFieldEnterStatReadyNtf (0x520B / 21003) - TMsgGameStartRegainNtf: BasicRegain = Speed / 10.0f (_Unit49.pas:23165)
            int totalSpeed = player.Speed;
            if (_gameDb != null && _gameDb.StatusTable.TryGetValue(player.Level, out var statDef))
            {
                totalSpeed = statDef.Speed;
            }
            float regainRate = (totalSpeed > 0 ? totalSpeed : 34) / 10.0f;
            await session.SendAsync(YogurtingPackets.MakeGameFieldEnterStatReadyNtf(regainRate));

            // 6. MsgGameFieldViewRangeNtf (0x79D4 / 31188) - Field View Range (400)
            await session.SendAsync(YogurtingPackets.MakeGameFieldViewRangeNtf(400));

            // 8. TMsgGameTriggerActionNtf (0x795C / 31068) - Play Campus Background Music (BGM)
            string fieldName = string.Empty;
            GameFieldDef? curF = null;
            if (_gameDb != null && _gameDb.Fields.TryGetValue(player.FieldId, out curF))
            {
                fieldName = curF.Name;
            }
            int bgmNo = curF != null && curF.Bgm > 0 ? curF.Bgm : 6;
            await session.SendAsync(YogurtingPackets.MakeGameTriggerBgmNtf(bgmNo));

            // 7. Spawn Field Monsters (0x796E MonInfo + 0x7969 MonMove) if Hunt Map
            if (curF != null && curF.IsHuntField)
            {
                await field.SpawnMonstersAsync(session, _gameDb);
            }

            // 10. Set State (HP, SP, Stats - 0x520F)
            await session.SendAsync(YogurtingPackets.MakeGameSetStateNtf(player));

            // 11. TMsgGameCharaNameInfoNtf (0x7963) - Field name and zone info (Delphi push 0xFA1 = 4001)
            var zoneTitles = !string.IsNullOrEmpty(fieldName) ? new List<string> { fieldName } : null;
            await session.SendAsync(YogurtingPackets.MakeGameCharaNameInfoNtf(-1, (int)player.School, 0, string.Empty, 4001, zoneTitles, string.Empty));
        }

        public async Task SpawnCampusEntitiesAsync(ClientSession session, int fieldId)
        {
            var field = _worldManager.GetOrCreateField(fieldId);
            await field.SpawnFixedEntitiesAsync(session, _gameDb);
        }

        private async Task BroadcastToFieldAsync(PlayerSessionState state, byte[] packetData)
        {
            var field = _worldManager.GetOrCreateField(state.Player.FieldId);
            await field.BroadcastAsync(packetData, state.Player.CharaId);
        }

        private void OnMonsterAiTick(object? state)
        {
            try
            {
                if (_gameDb == null) return;

                // 1. Natural HP Regain tick for all active players (Delphi TChara.Update / 0x520B Regain)
                foreach (var s in _activeSessions.Values)
                {
                    var p = s.Player;
                    if (p != null && p.CurrentHp > 0 && p.CurrentHp < p.MaxHp)
                    {
                        p.HpRegainAccumulator += 0.25f;
                        if (p.HpRegainAccumulator >= 3.0f)
                        {
                            p.HpRegainAccumulator = 0f;
                            bool isHunt = _gameDb.Fields.TryGetValue(p.FieldId, out var f) && f.IsHuntField;
                            int regainAmount = isHunt ? 1 : 4;
                            p.CurrentHp = Math.Min(p.MaxHp, p.CurrentHp + regainAmount);
                            _ = s.Session.SendAsync(YogurtingPackets.MakeGameSetHpNtf((ushort)p.CurrentHp));
                        }
                    }
                }

                // Only tick fields that currently have active players
                var activeFieldIds = new System.Collections.Generic.HashSet<int>();
                foreach (var s in _activeSessions.Values)
                {
                    activeFieldIds.Add(s.Player.FieldId);
                }

                foreach (int fieldId in activeFieldIds)
                {
                    if (!_gameDb.Fields.TryGetValue(fieldId, out var fieldDef) || fieldDef.Monsters.Count == 0)
                    {
                        continue;
                    }

                    var playersInField = new System.Collections.Generic.List<PlayerSessionState>();
                    foreach (var s in _activeSessions.Values)
                    {
                        if (s.Player.FieldId == fieldId)
                        {
                            playersInField.Add(s);
                        }
                    }

                    if (playersInField.Count == 0) continue;

                    lock (fieldDef.Monsters)
                    {
                        foreach (var mon in fieldDef.Monsters)
                        {
                            if (mon.IsDead) continue;

                            mon.Frame++;

                            switch (mon.State)
                            {
                                case MonsterState.Wait:
                                {
                                    // 1. Check if aggroed by player attack or direct target
                                    PlayerSessionState? target = null;
                                    if (mon.TargetPlayerId > 0)
                                    {
                                        target = playersInField.Find(p => (p.Player.CharacterId == mon.TargetPlayerId || p.Player.CharaId == mon.TargetPlayerId) && p.Player.CurrentHp > 0 && p.PendingWarpFieldId == 0);
                                    }

                                    if (target != null)
                                    {
                                        mon.State = MonsterState.Chase;
                                        mon.Frame = 0;
                                        break;
                                    }

                                    // 2. Peaceful Idle Wandering (_Unit49.pas:18390: desynchronized wander timers 2-5 sec)
                                    if (mon.Frame >= mon.NextWanderInterval)
                                    {
                                        mon.Frame = 0;
                                        mon.NextWanderInterval = Random.Shared.Next(8, 20); // 2.0s to 5.0s at 250ms/tick

                                        // Pick random wander destination within [-8, +8] around SpawnX/SpawnY leash anchor
                                        float offX = Random.Shared.Next(-8, 9);
                                        float offY = Random.Shared.Next(-8, 9);
                                        float destX = Math.Clamp(mon.SpawnX + offX, 5f, 95f);
                                        float destY = Math.Clamp(mon.SpawnY + offY, 5f, 95f);

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
                                            mon.MoveMotion = 1;      // Motion 1 = Walk
                                            mon.MoveSpeedRate = 80;  // 0x50 = 80 decimal (0.8x speed in Delphi)

                                            byte[] moveNtf = YogurtingPackets.MakeGameMonMoveNtf(mon.EntityId, curX, curY, tX, tY, mon.MoveMotion, mon.MoveSpeedRate);
                                            var field = _worldManager.GetOrCreateField(fieldId);
                                            _ = field.BroadcastAsync(moveNtf);
                                        }
                                    }
                                    break;
                                }

                                case MonsterState.Chase:
                                {
                                    PlayerSessionState? target = null;
                                    if (mon.TargetPlayerId > 0)
                                    {
                                        target = playersInField.Find(p => (p.Player.CharacterId == mon.TargetPlayerId || p.Player.CharaId == mon.TargetPlayerId) && p.Player.CurrentHp > 0 && p.PendingWarpFieldId == 0);
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

                                    // Leash Range Exceeded: Drop aggro, heal to full, send 0x7A01 + 0x79E8, return to spawn
                                    if (dist > 18.0f)
                                    {
                                        mon.TargetPlayerId = 0;
                                        mon.CurrentHp = mon.MaxHp;
                                        mon.State = MonsterState.Wait;
                                        mon.Frame = 0;

                                        byte[] dropOwnerNtf = YogurtingPackets.MakeGameMonsterOwnershipLostNtf(mon.EntityId);
                                        _ = target.Session.SendAsync(dropOwnerNtf);

                                        byte[] hpNtf = YogurtingPackets.MakeGameMonHpInfoNtf(mon.EntityId, mon.CurrentHp, mon.MaxHp);
                                        _ = target.Session.SendAsync(hpNtf);

                                        byte[] resetMoveNtf = YogurtingPackets.MakeGameMonMoveNtf(mon.EntityId, (int)mon.X, (int)mon.Y, (int)mon.SpawnX, (int)mon.SpawnY, 1, 80);
                                        mon.X = mon.SpawnX;
                                        mon.Y = mon.SpawnY;
                                        var field = _worldManager.GetOrCreateField(fieldId);
                                        _ = field.BroadcastAsync(resetMoveNtf);
                                        break;
                                    }

                                    // Melee Range Reached: Switch to Attack
                                    if (dist <= 1.8f)
                                    {
                                        mon.State = MonsterState.Attack;
                                        mon.Frame = 60; // Ready for attack
                                        break;
                                    }

                                    // Run towards player target smoothly
                                    float moveSpeed = 0.8f; // ~3.2 units / sec
                                    float dirX = dx / dist;
                                    float dirY = dy / dist;
                                    float step = MathF.Min(dist - 1.2f, moveSpeed);
                                    mon.X += dirX * step;
                                    mon.Y += dirY * step;
                                    mon.DirX = (int)(dirX * 100);
                                    mon.DirY = (int)(dirY * 100);

                                    // Send MoveNtf periodically (every 1.5s or if target position shifted > 3 units)
                                    if (mon.Frame >= 6 || MathF.Abs(mon.DestX - pX) > 3f || MathF.Abs(mon.DestY - pY) > 3f)
                                    {
                                        mon.Frame = 0;
                                        mon.DestX = pX;
                                        mon.DestY = pY;
                                        byte[] moveNtf = YogurtingPackets.MakeGameMonMoveNtf(mon.EntityId, (int)mon.X, (int)mon.Y, (int)pX, (int)pY, 2, 80); // Motion 2 = Run
                                        var fld = _worldManager.GetOrCreateField(fieldId);
                                        _ = fld.BroadcastAsync(moveNtf);
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
                                        target = playersInField.Find(p => (p.Player.CharacterId == mon.TargetPlayerId || p.Player.CharaId == mon.TargetPlayerId) && p.Player.CurrentHp > 0 && p.PendingWarpFieldId == 0);
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

                                    if (dist > 2.0f)
                                    {
                                        mon.State = MonsterState.Chase;
                                        mon.Frame = 0;
                                        break;
                                    }

                                    // Execute Attack on Cooldown (_Unit49.pas:18809: FFrame >= 60 = ~2s)
                                    if ((DateTime.UtcNow - mon.LastAttackTime).TotalSeconds >= 2.0)
                                    {
                                        mon.LastAttackTime = DateTime.UtcNow;

                                        // Authentic Delphi Damage Calculation (_Unit49.pas:18815-18875):
                                        int typeHit = 0; // 0 = Hit, 2 = Miss
                                        int finalDmg = 0;

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
                                            finalDmg = Math.Max(1, rawDmg - defMitigation);
                                            target.Player.CurrentHp = Math.Max(0, target.Player.CurrentHp - finalDmg);
                                        }

                                        // Broadcast Attack Animation & Damage (0x796A)
                                        byte[] monAtkNtf = YogurtingPackets.MakeGameMonAttackNtf(
                                            mon.EntityId,
                                            (int)mon.X,
                                            (int)mon.Y,
                                            target.Player.CharaId,
                                            finalDmg,
                                            mon.MotionType > 0 ? mon.MotionType : 300011,
                                            (byte)typeHit);

                                        var field = _worldManager.GetOrCreateField(fieldId);
                                        _ = field.BroadcastAsync(monAtkNtf);

                                        _ = target.Session.SendAsync(YogurtingPackets.MakeGameSetHpNtf((ushort)target.Player.CurrentHp));

                                        // Player Defeat / Collapse (0x791B)
                                        if (target.Player.CurrentHp <= 0)
                                        {
                                            Logger.Info($"[FieldServer] '{target.Player.CharacterName}' was knocked down by '{mon.Name}'!");
                                            byte[] dieNtf = YogurtingPackets.MakeGameDieCharNtf(
                                                target.Player.CharaId,
                                                (int)target.Player.Position.X,
                                                (int)target.Player.Position.Y);
                                            _ = target.Session.SendAsync(dieNtf);
                                            _ = field.BroadcastAsync(dieNtf);

                                            // Drop aggro on all monsters
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
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] Monster AI tick error: {ex.Message}");
            }
        }
    }
}
