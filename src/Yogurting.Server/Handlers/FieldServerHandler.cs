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

            // Active Monster AI & Movement loop (ticks every 1.5 seconds)
            _monsterAiTimer = new System.Threading.Timer(OnMonsterAiTick, null, 2000, 1500);
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
                _ = _repository.SaveAsync(state.Player);
            }
        }

        private async Task HandleSchoolEnterAsync(ClientSession session, byte[] packetData)
        {
            int charaId = packetData.Length >= 10 ? BitConverter.ToInt32(packetData, 6) : 1;
            int authToken = packetData.Length >= 38 ? BitConverter.ToInt32(packetData, 34) : 0;

            Logger.Info($"[FieldServer] School Server Handshake (0x5211/0x5213) from {session.RemoteEndPoint} - CharaId={charaId}, AuthToken={authToken}");

            var player = await _repository.GetByUsernameAsync(session.AccountId ?? "test") ?? new Player("test", session.CharacterName ?? "Hanazono");

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
            await session.SendAsync(YogurtingPackets.MakeGameAtkMovChangeNtf(player.CharaId, 1.0f, 1.0f));

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
            if (_gameDb != null && _gameDb.Fields.TryGetValue(player.FieldId, out var curF))
            {
                isHuntField = curF.IsHuntField;
                monCount = curF.Monsters.Count;
            }
            await session.SendAsync(YogurtingPackets.MakeGameFieldLoadingStartNtf(player.FieldId, player.Position.X, player.Position.Y, isHuntField, monCount));

            // 9. TMsgGameCharaNameInfoNtf (0x7963) - Exact 56B match with Delphi Quartet
            await session.SendAsync(YogurtingPackets.MakeGameCharaNameInfoNtf(-1, (int)player.School, 0, string.Empty, 0, null, string.Empty));

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

            // 5. MsgGameFieldEnterStatReadyNtf (0x520B / 21003) - Activates Client Live Inventory & Paperdoll Sync
            await session.SendAsync(YogurtingPackets.MakeGameFieldEnterStatReadyNtf(0.3f));

            // 6. MsgGameFieldViewRangeNtf (0x79D4 / 31188) - Field View Range (400)
            await session.SendAsync(YogurtingPackets.MakeGameFieldViewRangeNtf(400));

            // 8. TMsgGameTriggerActionNtf (0x795C / 31068) - Play Campus Background Music (BGM)
            int bgmNo = _gameDb != null ? _gameDb.GetFieldBgm(player.FieldId) : 6;
            await session.SendAsync(YogurtingPackets.MakeGameTriggerBgmNtf(bgmNo));

            // 10. Set State (HP, SP, Stats - 0x520F)
            await session.SendAsync(YogurtingPackets.MakeGameSetStateNtf(player));
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
                        bool isHunt = _gameDb.Fields.TryGetValue(p.FieldId, out var f) && f.IsHuntField;
                        // In Campus: 2.2 HP/sec. In Hunt: 0.3 HP/sec. Timer interval is 1.5 seconds.
                        float regainRate = (isHunt ? 0.3f : 2.2f) * 1.5f;
                        p.CurrentHp = Math.Min(p.MaxHp, (int)Math.Ceiling(p.CurrentHp + regainRate));
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

                            // 1. Passive Retaliation AI (Delphi style): Only attack if specifically aggroed by player attack
                            PlayerSessionState? targetedPlayer = null;
                            if (mon.TargetPlayerId > 0)
                            {
                                targetedPlayer = playersInField.Find(p => p.Player.CharacterId == mon.TargetPlayerId || p.Player.CharaId == mon.TargetPlayerId);
                            }

                            if (targetedPlayer != null && targetedPlayer.Player.CurrentHp > 0 && targetedPlayer.PendingWarpFieldId == 0)
                            {
                                // Agro: move towards target player
                                float pX = (float)targetedPlayer.Player.Position.X;
                                float pY = (float)targetedPlayer.Player.Position.Y;
                                float dx = pX - mon.X;
                                float dy = pY - mon.Y;
                                float dist = MathF.Sqrt(dx * dx + dy * dy);

                                if (dist > 20.0f)
                                {
                                    // Leash: Player ran too far away, monster drops agro and returns to spawn
                                    mon.TargetPlayerId = 0;
                                    mon.X = mon.SpawnX;
                                    mon.Y = mon.SpawnY;
                                    byte[] resetMoveNtf = YogurtingPackets.MakeGameMonMoveNtf(mon.EntityId, (int)pX, (int)pY, (int)mon.SpawnX, (int)mon.SpawnY, 1, 80);
                                    var field = _worldManager.GetOrCreateField(fieldId);
                                    _ = field.BroadcastAsync(resetMoveNtf);
                                    continue;
                                }

                                if (dist > 1.8f) // Walk closer
                                {
                                    int curX = (int)mon.X;
                                    int curY = (int)mon.Y;

                                    float dirX = dx / dist;
                                    float dirY = dy / dist;

                                    float step = MathF.Min(dist - 1.2f, 1.2f);
                                    mon.X += dirX * step;
                                    mon.Y += dirY * step;
                                    mon.DirX = (int)(dirX * 100);
                                    mon.DirY = (int)(dirY * 100);

                                    int destX = (int)mon.X;
                                    int destY = (int)mon.Y;

                                    byte[] moveNtf = YogurtingPackets.MakeGameMonMoveNtf(mon.EntityId, curX, curY, destX, destY, 1, 80);
                                    var field = _worldManager.GetOrCreateField(fieldId);
                                    _ = field.BroadcastAsync(moveNtf);
                                }
                                else
                                {
                                    // Melee retaliation / attack with animation (0x796A)
                                    if ((DateTime.UtcNow - mon.LastAttackTime).TotalSeconds >= 1.5)
                                    {
                                        mon.LastAttackTime = DateTime.UtcNow;
                                        int monAtk = Math.Max(3, mon.AttackPower - (targetedPlayer.Player.Defense / 4));
                                        int monDmg = Math.Max(2, monAtk + Random.Shared.Next(-2, 4));
                                        targetedPlayer.Player.CurrentHp = Math.Max(0, targetedPlayer.Player.CurrentHp - monDmg);

                                        byte[] monAtkNtf = YogurtingPackets.MakeGameMonAttackNtf(
                                            mon.EntityId, 
                                            (int)mon.X, 
                                            (int)mon.Y, 
                                            targetedPlayer.Player.CharaId, 
                                            monDmg, 
                                            mon.MotionType > 0 ? mon.MotionType : 300011, 
                                            1);
                                        var field = _worldManager.GetOrCreateField(fieldId);
                                        _ = field.BroadcastAsync(monAtkNtf);
                                        _ = targetedPlayer.Session.SendAsync(YogurtingPackets.MakeGameSetHpNtf((ushort)targetedPlayer.Player.CurrentHp));
                                        _ = targetedPlayer.Session.SendAsync(YogurtingPackets.MakeGameSetStateNtf(targetedPlayer.Player));

                                        // Player Death Notification (0x791B)
                                        if (targetedPlayer.Player.CurrentHp <= 0)
                                        {
                                            Logger.Info($"[FieldServer] '{targetedPlayer.Player.CharacterName}' was knocked down by '{mon.Name}'!");
                                            byte[] dieNtf = YogurtingPackets.MakeGameDieCharNtf(
                                                targetedPlayer.Player.CharaId,
                                                (int)targetedPlayer.Player.Position.X,
                                                (int)targetedPlayer.Player.Position.Y);
                                            _ = targetedPlayer.Session.SendAsync(dieNtf);
                                            _ = field.BroadcastAsync(dieNtf);
                                            
                                            // Drop aggro on all monsters in zone
                                            foreach (var m in fieldDef.Monsters)
                                            {
                                                if (m.TargetPlayerId == targetedPlayer.Player.CharaId)
                                                {
                                                    m.TargetPlayerId = 0;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                mon.TargetPlayerId = 0;

                                // Peaceful idle wander: 25% chance to wander within 3 tiles of spawn
                                if (Random.Shared.Next(0, 4) == 0)
                                {
                                    int curX = (int)mon.X;
                                    int curY = (int)mon.Y;

                                    float offsetX = Random.Shared.Next(-3, 4);
                                    float offsetY = Random.Shared.Next(-3, 4);
                                    float targetX = MathF.Max(5f, mon.SpawnX + offsetX);
                                    float targetY = MathF.Max(5f, mon.SpawnY + offsetY);

                                    mon.X = targetX;
                                    mon.Y = targetY;
                                    int destX = (int)targetX;
                                    int destY = (int)targetY;

                                    byte[] moveNtf = YogurtingPackets.MakeGameMonMoveNtf(mon.EntityId, curX, curY, destX, destY, 1, 80);
                                    var field = _worldManager.GetOrCreateField(fieldId);
                                    _ = field.BroadcastAsync(moveNtf);
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
