using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Yogurting.Core.Logging;
using Yogurting.Core.Models;
using Yogurting.Core.Network;
using Yogurting.Data.Loaders;
using Yogurting.Data.Repositories;
using Yogurting.Server.Handlers.Field;
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
            var tradeHandlers = new TradeHandlers(
                id => _activeSessions.Values.FirstOrDefault(s => s.Player.CharacterId == id || s.Player.CharaId == id),
                fieldId => _activeSessions.Values.Where(s => s.Player.FieldId == fieldId).ToList(),
                _repository);
            var storageHandlers = new StorageAndRefinementHandlers(BroadcastToFieldAsync, _repository, _gameDb);
            var lobbyHandlers = new LobbyAndEpisodeRoomHandlers(
                id => _activeSessions.Values.FirstOrDefault(s => s.Player.CharacterId == id || s.Player.CharaId == id),
                BroadcastToFieldAsync,
                _repository,
                _gameDb);
            var capsuleHandlers = new CapsuleAndInteractionHandlers(BroadcastToFieldAsync, _repository, _gameDb);

            _dispatcher.RegisterHandlers(equipHandlers);
            _dispatcher.RegisterHandlers(movementHandlers);
            _dispatcher.RegisterHandlers(npcHandlers);
            _dispatcher.RegisterHandlers(shopHandlers);
            _dispatcher.RegisterHandlers(combatHandlers);
            _dispatcher.RegisterHandlers(tradeHandlers);
            _dispatcher.RegisterHandlers(storageHandlers);
            _dispatcher.RegisterHandlers(lobbyHandlers);
            _dispatcher.RegisterHandlers(capsuleHandlers);

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
            if (opcode == 0x4E21 || opcode == (ushort)PacketOpcode.MsgCheckVersionNtf)
            {
                // Delphi 0x006BF550: Client version check notification (no-op)
                return;
            }

            if (opcode == 0x4E26 || opcode == (ushort)PacketOpcode.MsgTimeNtf)
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
                Logger.Warn($"[FieldServer] Rejected unauthorized Opcode 0x{opcode:X4} ({opcode}) from unauthenticated session {session.RemoteEndPoint}");
                return;
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
            player.GaugeCurrent = 0;
            player.ChargePoint = 0;

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
                if (_gameDb == null || _worldManager == null) return;

                // Dedicated per-field lane execution (parallel across all active fields)
                var activeFields = _worldManager.AllFields.Where(f => f.Players.Count > 0).ToList();
                Parallel.ForEach(activeFields, field =>
                {
                    try
                    {
                        field.Update(_gameDb);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"[FieldServer] Field {field.FieldId} update error: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] OnMonsterAiTick dispatcher error: {ex.Message}");
            }
        }
    }
}