using System;
using System.Threading.Tasks;
using Yogurting.Core.Logging;
using Yogurting.Core.Models;
using Yogurting.Core.Network;
using Yogurting.Data.Loaders;

namespace Yogurting.Server.Handlers.Field
{
    /// <summary>
    /// Handles Movement, Positioning, Warp Gates, Object Interactions, and Handshakes in the Field Server.
    /// </summary>
    public sealed class MovementAndFieldHandlers
    {
        private readonly Func<PlayerSessionState, byte[], Task> _broadcastDelegate;
        private readonly Func<ClientSession, int, Task> _spawnEntitiesDelegate;
        private readonly Yogurting.Data.Loaders.GameDatabase? _gameDb;
        private readonly Yogurting.Server.World.WorldManager? _worldManager;
        private readonly Yogurting.Data.Repositories.IAccountRepository? _repository;

        public MovementAndFieldHandlers(
            Func<PlayerSessionState, byte[], Task> broadcastDelegate,
            Func<ClientSession, int, Task> spawnEntitiesDelegate,
            Yogurting.Data.Loaders.GameDatabase? gameDb = null,
            Yogurting.Server.World.WorldManager? worldManager = null,
            Yogurting.Data.Repositories.IAccountRepository? repository = null)
        {
            _broadcastDelegate = broadcastDelegate ?? throw new ArgumentNullException(nameof(broadcastDelegate));
            _spawnEntitiesDelegate = spawnEntitiesDelegate ?? throw new ArgumentNullException(nameof(spawnEntitiesDelegate));
            _gameDb = gameDb;
            _worldManager = worldManager;
            _repository = repository;
        }

        /// <summary>
        /// 0x4E21 (20001): MsgCheckVersionNtf - Client version check notification
        /// </summary>
        [PacketHandler(PacketOpcode.MsgCheckVersionNtf)]
        public Task HandleCheckVersionAsync(PlayerSessionState state, byte[] packetData)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 0x4E26 (20006): MsgTimeNtf - Client Time Ping / Echo
        /// </summary>
        [PacketHandler(PacketOpcode.MsgTimeNtf)]
        public async Task HandleTimeSyncAsync(PlayerSessionState state, byte[] packetData)
        {
            await state.Session.SendAsync(YogurtingPackets.MakeTimeNtf());
        }

        /// <summary>
        /// 0x5211 (21009): MsgPingTimeReq - School Entry Handshake
        /// </summary>
        [PacketHandler(PacketOpcode.MsgPingTimeReq)]
        public async Task HandlePingTimeAsync(PlayerSessionState state, byte[] packetData)
        {
            // Echo time sync
            await state.Session.SendAsync(YogurtingPackets.MakeTimeNtf());
        }

        /// <summary>
        /// 0x5213 (21011): MsgEnterScsReq - School Server Enter Acknowledgement
        /// </summary>
        [PacketHandler(PacketOpcode.MsgEnterScsReq)]
        public Task HandleEnterScsReqAsync(PlayerSessionState state, byte[] packetData)
        {
            // Delphi 0x006BF7AC treats this as an empty acknowledgment
            return Task.CompletedTask;
        }

        /// <summary>
        /// 0x79D5 (31189): MsgGameMoveExReq / MsgGameMoveExNtf - Extended 2D/3D Movement Delta Sync
        /// Delphi 0x005AF7F0
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameMoveExReq)]
        public async Task HandleMoveExAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                if (packetData.Length >= 16)
                {
                    int charaId = BitConverter.ToInt32(packetData, 6);
                    ushort px = BitConverter.ToUInt16(packetData, 10);
                    ushort py = BitConverter.ToUInt16(packetData, 12);
                    sbyte dx = (sbyte)packetData[14];
                    sbyte dy = (sbyte)packetData[15];

                    state.Player.Position = new Position(px, py, state.Player.Position.Z, state.Player.Position.Heading);

                    // If dialogue is open while moving, reset dialogue state
                    state.ActiveNpcId = 0;
                    state.CurrentNpcDialogNode = string.Empty;

                    await _broadcastDelegate(state, packetData);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] MoveEx error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x791E (31006): MsgGameMoveStopReq - Character Stop Move
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameMoveStopReq)]
        public async Task HandleMoveStopAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                if (packetData.Length >= 14)
                {
                    int charaId = BitConverter.ToInt32(packetData, 6);
                    ushort px = BitConverter.ToUInt16(packetData, 10);
                    ushort py = BitConverter.ToUInt16(packetData, 12);
                    state.Player.Position = new Position(px, py, state.Player.Position.Z, state.Player.Position.Heading);
                    await _broadcastDelegate(state, packetData);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] MoveStop error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x7922 (31010): MsgGameJumpReq / AOI Block
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameJumpReq)]
        public async Task HandleJumpAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                if (packetData.Length >= 10)
                {
                    ushort px = BitConverter.ToUInt16(packetData, 6);
                    ushort py = BitConverter.ToUInt16(packetData, 8);
                    state.Player.Position = new Position(px, py, state.Player.Position.Z, state.Player.Position.Heading);

                    // If dialogue is open while jumping, reset dialogue state
                    state.ActiveNpcId = 0;
                    state.CurrentNpcDialogNode = string.Empty;

                    await _broadcastDelegate(state, packetData);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] Jump error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x7921 (31009): MsgGamePosSyncReq - Position Periodic Sync
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGamePosSyncReq)]
        public async Task HandlePosSyncAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                if (packetData.Length >= 10)
                {
                    ushort px = BitConverter.ToUInt16(packetData, 6);
                    ushort py = BitConverter.ToUInt16(packetData, 8);
                    state.Player.Position = new Position(px, py, state.Player.Position.Z, state.Player.Position.Heading);
                }
            }
            catch { }
            await Task.CompletedTask;
        }

        /// <summary>
        /// 0x795B (31067): MsgGameFieldLoadingDoneReq / MsgGameEmoteReq - Field loading complete acknowledgement
        /// Phase 2 of Field Entry: Reveals 3D Campus, activates live inventory listener, and sets view distance.
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameEmoteReq)]
        public async Task HandleEmoteAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                var session = state.Session;
                var player = state.Player;
                int schoolId = player.School == SchoolType.EstivaAcademy ? 1 : 2;

                // 1. School Campus Info (0x5264)
                await session.SendAsync(YogurtingPackets.MakeGameSchoolInfoNtf(schoolId));

                // 2. Spawn Fixed NPCs, Campus Objects, & Other Players (0x521B, 0x5227, 0x7942, 0x795C)
                await _spawnEntitiesDelegate(session, player.FieldId);

                // 3. Reveal 3D Campus (0x7956)
                await session.SendAsync(YogurtingPackets.MakeGameFieldInfoDoneNtf());

                // 4. Activate LocalPlayer Live Inventory & Paperdoll Event Listener (0x520B / TMsgGameStartRegainNtf: Speed / 10.0f)
                int totalSpeed = player.Speed;
                if (_gameDb != null && _gameDb.StatusTable.TryGetValue(player.Level, out var statDef))
                {
                    totalSpeed = statDef.Speed;
                }
                float regainRate = (totalSpeed > 0 ? totalSpeed : 34) / 10.0f;
                await session.SendAsync(YogurtingPackets.MakeGameFieldEnterStatReadyNtf(regainRate));

                // 5. Set 3D Field View Range (0x79D4 / 400)
                await session.SendAsync(YogurtingPackets.MakeGameFieldViewRangeNtf(400));

                // 6. Background Music (BGM - 0x795C / Action 0x27)
                int bgmNo = _gameDb != null ? _gameDb.GetFieldBgm(player.FieldId) : 6;
                await session.SendAsync(YogurtingPackets.MakeGameTriggerBgmNtf(bgmNo));

                // 7. TMsgGameCharaNameInfoNtf (0x7963) - Field name and zone info (Delphi push 0xFA1 = 4001)
                string fieldName = string.Empty;
                if (_gameDb != null && _gameDb.Fields.TryGetValue(player.FieldId, out var curF))
                {
                    fieldName = curF.Name;
                }
                var zoneTitles = !string.IsNullOrEmpty(fieldName) ? new List<string> { fieldName } : null;
                await session.SendAsync(YogurtingPackets.MakeGameCharaNameInfoNtf(-1, (int)player.School, 0, string.Empty, 4001, zoneTitles, string.Empty));

                Logger.Info($"[FieldServer] '{player.CharacterName}' completed 3D field loading for Field {player.FieldId}! BGM #{bgmNo} triggered.");
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] Emote/FieldLoading error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x5223 (21027): MsgGameNpcClickReq - Interacting with Campus NPCs
        /// </summary>
        [PacketHandler((PacketOpcode)0x5223)]
        public async Task HandleNpcClickAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                int npcId = packetData.Length >= 10 ? BitConverter.ToInt32(packetData, 6) : 1;
                Logger.Info($"[FieldServer] '{state.Player.CharacterName}' clicked NPC {npcId}");
                await state.Session.SendAsync(YogurtingPackets.MakeGameObjectUseAns(1, npcId));
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] NpcClick error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x521F (21023): MsgObjectClickReq - Field Object / Warp Gate Click Request
        /// </summary>
        [PacketHandler(PacketOpcode.MsgObjectClickReq)]
        public async Task HandleObjectUseAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                int objectId = packetData.Length >= 10 ? BitConverter.ToInt32(packetData, 6) : 1;
                await state.Session.SendAsync(YogurtingPackets.MakeGameObjectUseAns(1, objectId));
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] ObjectUse error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x798C (31116): MsgGameSitDownReq - Character sitting down on chair/bench or floor
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameSitDownReq)]
        public async Task HandleSitDownAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                ushort x = packetData.Length >= 8 ? BitConverter.ToUInt16(packetData, 6) : (ushort)state.Player.Position.X;
                ushort y = packetData.Length >= 10 ? BitConverter.ToUInt16(packetData, 8) : (ushort)state.Player.Position.Y;
                int bChair = packetData.Length >= 14 ? BitConverter.ToInt32(packetData, 10) : 0;

                state.Player.Position = new Position(x, y, state.Player.Position.Z, state.Player.Position.Heading);

                Logger.Info($"[FieldServer] '{state.Player.CharacterName}' sitting down at ({x}, {y}) (Chair={bChair})");

                byte[] ans = YogurtingPackets.MakeGameSitDownAns(1, state.Player.CharaId, x, y, bChair);
                await state.Session.SendAsync(ans);
                await _broadcastDelegate(state, ans);
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] SitDown error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x798E (31118): MsgGameStandUpNtf - Character standing up
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameStandUpNtf)]
        public async Task HandleStandUpAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                Logger.Info($"[FieldServer] '{state.Player.CharacterName}' stood up");

                byte[] ntf = YogurtingPackets.MakeGameStandUpNtf(state.Player.CharaId);
                await state.Session.SendAsync(ntf);
                await _broadcastDelegate(state, ntf);
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] StandUp error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x794D (31053): MsgGameRevival119Req - Player requested 119 Emergency In-Place Revival
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameRevival119Req)]
        public async Task HandleRevival119Async(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                var player = state.Player;
                Logger.Info($"[FieldServer] '{player.CharacterName}' requested 119 Emergency In-Place Revival.");

                // 1. Restore 100% HP
                player.CurrentHp = player.MaxHp;

                // 2. Send Revival119Ans (0x794E)
                await state.Session.SendAsync(YogurtingPackets.MakeGameRevival119Ans(1));

                // 3. Broadcast updated HP & alive state (0x520F)
                byte[] stateNtf = YogurtingPackets.MakeGameSetStateNtf(player);
                await state.Session.SendAsync(stateNtf);
                await _broadcastDelegate(state, stateNtf);
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] Revival119 error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x7965 (31077): MsgGameWarpTriggerReq - Step into warp portal ring trigger
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameWarpTriggerReq)]
        public async Task HandleWarpTriggerAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                int gateId = packetData.Length >= 11 ? packetData[10] : 1;
                int currentFieldId = state.Player.FieldId;
                Logger.Info($"[FieldServer] '{state.Player.CharacterName}' stepped onto Warp Gate {gateId} in Field {currentFieldId} at position ({state.Player.Position.X:F1}, {state.Player.Position.Y:F1})");

                int targetField = 0;
                Position targetPos = new Position(50f, 50f, 0f);

                // 1. Authoritative lookup from current field's WarpGates
                FieldWarpGate? matchedGate = null;
                if (_gameDb != null && _gameDb.Fields.TryGetValue(currentFieldId, out var curField))
                {
                    matchedGate = curField.WarpGates.Find(g => g.Id == gateId);
                    if (matchedGate == null && curField.WarpGates.Count > 0)
                    {
                        // Proximity match within current field
                        float px = state.Player.Position.X * 100f;
                        float py = state.Player.Position.Y * 100f;
                        matchedGate = curField.WarpGates
                            .OrderBy(g => (g.X - px) * (g.X - px) + (g.Y - py) * (g.Y - py))
                            .FirstOrDefault();
                    }
                }

                if (matchedGate != null)
                {
                    targetField = matchedGate.DestFieldId;
                    targetPos = new Position(matchedGate.DestX, matchedGate.DestY, 0f);
                    Logger.Info($"[FieldServer] Warp target resolved from DB: Field {targetField} at ({targetPos.X:F1}, {targetPos.Y:F1}) via Gate #{matchedGate.Id}");
                }
                else
                {
                    Logger.Warn($"[FieldServer] No warp gate found for Gate ID {gateId} on Field {currentFieldId} in database.");
                    return;
                }

                state.PendingWarpFieldId = targetField;
                state.PendingWarpPosition = targetPos;

                bool isHuntField = false;
                int huntFieldId = 0;
                if (_gameDb != null && _gameDb.Fields.TryGetValue(targetField, out var targetFieldDef))
                {
                    isHuntField = targetFieldDef.IsHuntField;
                    huntFieldId = targetFieldDef.HuntFieldId;
                }

                // Exact Quartet response sequence for 0x7965 (Screen fades to black, client prepares target field):
                await state.Session.SendAsync(YogurtingPackets.MakeGameFadeOutNtf());
                await state.Session.SendAsync(YogurtingPackets.MakeGameSetHpNtf((ushort)state.Player.CurrentHp));
                await state.Session.SendAsync(YogurtingPackets.MakeGameWarpStartNtf(targetField, targetPos.X, targetPos.Y, isHuntField, huntFieldId));
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] Warp trigger error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x794F (31055): MsgGameRevivalSchoolReq - School Respawn / Revival Request
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameRevivalSchoolReq)]
        public async Task HandleRevivalSchoolAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                var player = state.Player;
                if (player == null) return;

                Logger.Info($"[FieldServer] '{player.CharacterName}' requested School Revival (0x794F)");

                // 1. Restore Full HP and MP from StatusTable
                var status = _gameDb?.GetStatusForLevel(player.Level) ?? new StatusDef { Pow = player.Level * 4, Speed = player.Level * 3, Skill = player.Level * 3, Luck = player.Level * 2 };
                player.RecalculateStats(status.Pow, status.Speed, status.Skill, status.Luck);
                player.CurrentHp = player.MaxHp;
                player.CurrentMp = player.MaxMp;

                // Clear aggro from current field monsters so they don't attack during warp
                if (_gameDb != null && _gameDb.Fields.TryGetValue(player.FieldId, out var curFDef))
                {
                    foreach (var m in curFDef.Monsters)
                    {
                        if (m.TargetPlayerId == player.CharacterId)
                        {
                            m.TargetPlayerId = 0;
                        }
                    }
                }

                // 2. Reply with Revival School Ans (0x7950)
                await state.Session.SendAsync(YogurtingPackets.MakeGameRevivalSchoolAns(1));

                // 3. Initiate Warp back to School Campus spawn point via 0x794C (MsgGameRevivalCharAns)
                var spawn = StarterConfigLoader.GetSpawnPoint(player.School);
                int schoolFieldId = spawn.FieldId;
                var schoolPos = new Position(spawn.X, spawn.Y, 0f);
                state.PendingWarpFieldId = schoolFieldId;
                state.PendingWarpPosition = schoolPos;

                await state.Session.SendAsync(YogurtingPackets.MakeGameFadeOutNtf());
                await state.Session.SendAsync(YogurtingPackets.MakeGameSetStateNtf(player));
                await state.Session.SendAsync(YogurtingPackets.MakeGameSetHpNtf((ushort)player.CurrentHp));
                await state.Session.SendAsync(YogurtingPackets.MakeGameRevivalCharAns(1, player.CharaId, schoolFieldId, (ushort)spawn.X, (ushort)spawn.Y, player.CurrentHp));
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] RevivalSchool error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x7967 (31079): MsgGameWarpGateReq - Campus Warp Gate Teleportation Execution
        /// Sent by client once it has loaded the destination field assets and is ready to enter.
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameWarpGateReq)]
        public async Task HandleWarpGateAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                int targetField = state.PendingWarpFieldId;
                Position targetPos = state.PendingWarpPosition;

                // Fallback: If pending destination was not set, parse requested destination from client packet or player save
                if (targetField == 0)
                {
                    if (packetData.Length >= 14)
                    {
                        targetField = BitConverter.ToInt32(packetData, 10);
                    }
                    if (targetField == 0)
                    {
                        targetField = state.Player.SaveFieldId > 0
                            ? state.Player.SaveFieldId
                            : (state.Player.School == SchoolType.SoilAcademy ? 90 : 1);
                    }

                    // Derive arrival position dynamically from destination field gates or starter spawn point
                    if (_gameDb != null && _gameDb.Fields.TryGetValue(targetField, out var destField))
                    {
                        var returnGate = destField.WarpGates.Find(g => g.DestFieldId == state.Player.FieldId)
                                      ?? destField.WarpGates.FirstOrDefault();
                        if (returnGate != null)
                        {
                            float rx = returnGate.X > 500 ? returnGate.X / 100f : returnGate.X;
                            float ry = returnGate.Y > 500 ? returnGate.Y / 100f : returnGate.Y;
                            targetPos = new Position(rx, ry, 0f);
                        }
                        else
                        {
                            var spawn = StarterConfigLoader.GetSpawnPoint(state.Player.School);
                            targetPos = new Position(spawn.X, spawn.Y, 0f);
                        }
                    }
                    else
                    {
                        var spawn = StarterConfigLoader.GetSpawnPoint(state.Player.School);
                        targetPos = new Position(spawn.X, spawn.Y, 0f);
                    }
                }

                state.LastWarpAt = DateTime.UtcNow;
                state.PendingWarpFieldId = 0;

                int oldFieldId = state.Player.FieldId;
                state.Player.FieldId = targetField;
                state.Player.Position = targetPos;

                // Only persist home save location if NOT a temporary hunt/mob field
                bool isHunt = _gameDb != null && _gameDb.Fields.TryGetValue(targetField, out var tf) && tf.IsHuntField;
                if (!isHunt)
                {
                    state.Player.SaveFieldId = targetField;
                    state.Player.SavePosition = targetPos;
                }

                if (_repository != null)
                {
                    _ = _repository.SaveAsync(state.Player);
                }

                if (_worldManager != null)
                {
                    _worldManager.MovePlayer(state, targetField);
                }

                Logger.Info($"[FieldServer] '{state.Player.CharacterName}' warped from Field {oldFieldId} to Field {targetField} at ({targetPos.X}, {targetPos.Y})!");

                // Exact 1-to-1 transition sequence from Quartet live capture:
                // If entering a Non-Hunt (Campus) map, send MsgGameSchoolInfoNtf (0x5264) to restore Campus mode & UI
                if (!isHunt)
                {
                    await state.Session.SendAsync(YogurtingPackets.MakeGameSchoolInfoNtf());
                }

                // 1. Spawn entities on new map (NPCs, terminals, gates)
                await _spawnEntitiesDelegate(state.Session, targetField);

                // 2. Field Info Done (0x7956) - signals client that map geometry and gates are ready
                await state.Session.SendAsync(YogurtingPackets.MakeGameFieldInfoDoneNtf());

                // 3. Enter Stat Ready (0x520B / TMsgGameStartRegainNtf: Speed / 10.0f)
                int totalSpeed = state.Player.Speed;
                if (_gameDb != null && _gameDb.StatusTable.TryGetValue(state.Player.Level, out var statDef))
                {
                    totalSpeed = statDef.Speed;
                }
                float regainRate = (totalSpeed > 0 ? totalSpeed : 34) / 10.0f;
                await state.Session.SendAsync(YogurtingPackets.MakeGameFieldEnterStatReadyNtf(regainRate));

                // 4. Field View Range (0x79D4 / 400)
                await state.Session.SendAsync(YogurtingPackets.MakeGameFieldViewRangeNtf(400));

                // 5. Background Music for New Field (0x795C / Action 0x27)
                int bgm = _gameDb != null ? _gameDb.GetFieldBgm(targetField) : 6;
                await state.Session.SendAsync(YogurtingPackets.MakeGameTriggerBgmNtf(bgm));

                // 6. Field Monsters (0x796E MonInfo + 0x7969 MonMove) for Hunt Maps
                if (isHunt && _worldManager != null)
                {
                    var targetInstance = _worldManager.GetOrCreateField(targetField);
                    await targetInstance.SpawnMonstersAsync(state.Session, _gameDb);
                }

                // 7. Warp Result (0x7968) - Signals client to fade in and render character at new location
                await state.Session.SendAsync(YogurtingPackets.MakeGameWarpResultNtf(targetField, targetPos.X, targetPos.Y));

                // 8. TMsgGameCharaNameInfoNtf (0x7963) - Field name and zone info
                string targetFieldName = string.Empty;
                if (_gameDb != null && _gameDb.Fields.TryGetValue(targetField, out var tfDef))
                {
                    targetFieldName = tfDef.Name;
                }
                var targetZoneTitles = !string.IsNullOrEmpty(targetFieldName) ? new List<string> { targetFieldName } : null;
                await state.Session.SendAsync(YogurtingPackets.MakeGameCharaNameInfoNtf(-1, (int)state.Player.School, 0, string.Empty, 4001, targetZoneTitles, string.Empty));
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] Warp error: {ex.Message}");
            }
        }
    }
}
