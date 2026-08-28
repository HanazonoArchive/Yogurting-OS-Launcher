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
        /// 0x4E21 (20001): MsgCheckVersionNtf - Client Time/Version Sync
        /// </summary>
        [PacketHandler(PacketOpcode.MsgCheckVersionNtf)]
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

                // 4. Activate LocalPlayer Live Inventory & Paperdoll Event Listener (0x520B / Float 0.3f)
                await session.SendAsync(YogurtingPackets.MakeGameFieldEnterStatReadyNtf(0.3f));

                // 5. Set 3D Field View Range (0x79D4 / 400)
                await session.SendAsync(YogurtingPackets.MakeGameFieldViewRangeNtf(400));

                // 7. Background Music (BGM - 0x795C / Action 0x27)
                int bgmNo = _gameDb != null ? _gameDb.GetFieldBgm(player.FieldId) : 6;
                await session.SendAsync(YogurtingPackets.MakeGameTriggerBgmNtf(bgmNo));

                // 8. Sync Full Character State
                await session.SendAsync(YogurtingPackets.MakeGameSetStateNtf(player));

                // 10. Warp Result / Fade-in confirmation
                await session.SendAsync(YogurtingPackets.MakeGameWarpResultNtf(player.FieldId, player.Position.X, player.Position.Y));

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
                Logger.Info($"[FieldServer] '{state.Player.CharacterName}' stepped onto Warp Gate {gateId} in Field {state.Player.FieldId}");

                int targetField = 0;
                Position targetPos = new Position(50f, 50f, 0f);

                // 1. Authoritative lookup from database score WarpGates (default.xml)
                FieldWarpGate? matchedGate = null;
                if (_gameDb != null && _gameDb.Fields.TryGetValue(state.Player.FieldId, out var curField))
                {
                    matchedGate = curField.WarpGates.Find(g => g.Id == gateId);
                }

                // If not found in current recorded field, search connected fields by proximity to find true current field
                if (matchedGate == null && _gameDb != null)
                {
                    foreach (var kvp in _gameDb.Fields)
                    {
                        var g = kvp.Value.WarpGates.Find(gate => gate.Id == gateId);
                        if (g != null)
                        {
                            state.Player.FieldId = kvp.Key;
                            matchedGate = g;
                            Logger.Info($"[FieldServer] Synchronized '{state.Player.CharacterName}' to Field {kvp.Key} via Gate {gateId}");
                            break;
                        }
                    }
                }

                if (matchedGate != null)
                {
                    targetField = matchedGate.DestFieldId;
                    targetPos = new Position(matchedGate.DestX, matchedGate.DestY, 0f);
                }

                // Fallback if not configured in database
                if (targetField == 0)
                {
                    targetField = state.Player.FieldId switch
                    {
                        90 => 92,
                        92 => 90,
                        91 => 399,
                        399 => 91,
                        1 or 386 => 2,
                        2 => 1,
                        _ => state.Player.FieldId
                    };
                    targetPos = new Position(55f, 28f, 0f);
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
                await state.Session.SendAsync(YogurtingPackets.MakeGameStatDeltaNtf(0xCB));
                await state.Session.SendAsync(YogurtingPackets.MakeGameWarpStartNtf(targetField, targetPos.X, targetPos.Y, isHuntField, huntFieldId));

                // If entering a Mob / Hunt Field, request weapon socket frame info (0x5273)
                // This initializes combat stance and activates the Cardboard Box HUD (LuCGameItemDropCateFrame / bootybox.tga)
                if (isHuntField)
                {
                    int weaponSlot = 4;
                    int weaponUid = state.Player.EquippedSlotUids.Length > weaponSlot ? state.Player.EquippedSlotUids[weaponSlot] : 1;
                    if (weaponUid == 0) weaponUid = 1;

                    int weaponTypeId = YogurtingPackets.GetPlayerItemTypeId(state.Player, weaponUid, (ushort)weaponSlot);
                    if (weaponTypeId == 0) weaponTypeId = 140001; // Starter Blade

                    await state.Session.SendAsync(YogurtingPackets.MakeGameWeaponFrameInfoReq(weaponTypeId, weaponUid));
                }
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
                var status = _gameDb?.GetStatusForLevel(player.Level) ?? new StatusDef { Pow = player.Level * 4, Skill = player.Level * 3 };
                player.MaxHp = status.Pow * 10 + 200;
                player.CurrentHp = player.MaxHp;
                player.MaxMp = status.Skill * 10 + 150;
                player.CurrentMp = player.MaxMp;

                // 2. Reply with Revival School Ans (0x7950)
                await state.Session.SendAsync(YogurtingPackets.MakeGameRevivalSchoolAns(1));

                // 3. Initiate Warp back to School Campus (Field 91 for Estiva, Field 90 for So-il)
                int schoolFieldId = player.School == SchoolType.EstivaAcademy ? 91 : 90;
                var schoolPos = new Position(76f, 104f, 0f);
                state.PendingWarpFieldId = schoolFieldId;
                state.PendingWarpPosition = schoolPos;

                await state.Session.SendAsync(YogurtingPackets.MakeGameFadeOutNtf());
                await state.Session.SendAsync(YogurtingPackets.MakeGameSetStateNtf(player));
                await state.Session.SendAsync(YogurtingPackets.MakeGameWarpStartNtf(schoolFieldId, schoolPos.X, schoolPos.Y, false, 0));
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

                // Fallback: If pending destination was not set, parse requested destination from client packet
                if (targetField == 0)
                {
                    if (packetData.Length >= 14)
                    {
                        targetField = BitConverter.ToInt32(packetData, 10);
                    }
                    if (targetField == 0) targetField = 91;

                    // Derive arrival position from return gate in destination field or map defaults
                    if (_gameDb != null && _gameDb.Fields.TryGetValue(targetField, out var destField))
                    {
                        var returnGate = destField.WarpGates.Find(g => g.DestFieldId == state.Player.FieldId);
                        if (returnGate != null)
                        {
                            float rx = returnGate.X > 500 ? returnGate.X / 100f : returnGate.X;
                            float ry = returnGate.Y > 500 ? returnGate.Y / 100f : returnGate.Y;
                            targetPos = new Position(rx > 0 ? rx : 58f, ry > 0 ? ry : 17f, 0f);
                        }
                        else
                        {
                            targetPos = new Position(58f, 17f, 0f);
                        }
                    }
                    else
                    {
                        targetPos = new Position(58f, 17f, 0f);
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

                // 3. Enter Stat Ready (0x520B)
                await state.Session.SendAsync(YogurtingPackets.MakeGameFieldEnterStatReadyNtf(isHunt ? 0.3f : 2.2f));

                // 4. Field Monsters (0x796E MonInfo + 0x7969 MonMove) for Hunt Maps
                if (isHunt && _worldManager != null)
                {
                    var targetInstance = _worldManager.GetOrCreateField(targetField);
                    await targetInstance.SpawnMonstersAsync(state.Session, _gameDb);
                }

                // 5. Zone Title & Mission Announcement (0x7963) - Exact 1-to-1 match with Quartet
                string zoneName = _gameDb != null && _gameDb.Fields.TryGetValue(targetField, out var fDef) && !string.IsNullOrWhiteSpace(fDef.Name)
                    ? fDef.Name
                    : (isHunt ? "分かれ道" : "1F");
                await state.Session.SendAsync(YogurtingPackets.MakeGameCharaNameInfoNtfPhase2(state.Player, 1, zoneName));

                // 6. Field View Range (0x79D4 / 400)
                await state.Session.SendAsync(YogurtingPackets.MakeGameFieldViewRangeNtf(400));

                // 7. Background Music for New Field (0x795C)
                int bgm = _gameDb != null ? _gameDb.GetFieldBgm(targetField) : 6;
                await state.Session.SendAsync(YogurtingPackets.MakeGameTriggerBgmNtf(bgm));

                // 8. Warp Result (0x7968) - Signals client to fade in and render character at new location
                await state.Session.SendAsync(YogurtingPackets.MakeGameWarpResultNtf(targetField, targetPos.X, targetPos.Y));
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] Warp error: {ex.Message}");
            }
        }
    }
}
