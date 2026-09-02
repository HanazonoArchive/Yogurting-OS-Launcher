using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Yogurting.Core.Logging;
using Yogurting.Core.Models;
using Yogurting.Core.Network;
using Yogurting.Data.Loaders;
using Yogurting.Data.Repositories;

namespace Yogurting.Server.Handlers.Field
{
    /// <summary>
    /// Episode Lobby, Room Matchmaking, and Waiting Room Handlers.
    /// Reverse-engineered from Delphi Quartet server logic
    /// (server_legacy/DELPHI PROJECT/_Unit67.pas:006C3400-006C3950 & struct.dms / 30300 series).
    /// </summary>
    public sealed class LobbyAndEpisodeRoomHandlers
    {
        private readonly Func<int, PlayerSessionState?> _findPlayerById;
        private readonly Func<PlayerSessionState, byte[], Task> _broadcastDelegate;
        private readonly IAccountRepository? _repository;
        private readonly GameDatabase? _gameDb;
        private readonly string _host;
        private readonly int _episodePort;

        private readonly ConcurrentDictionary<ushort, EpisodeRoom> _activeRooms = new();
        private ushort _nextRoomId = 1;
        private readonly object _roomLock = new();

        public LobbyAndEpisodeRoomHandlers(
            Func<int, PlayerSessionState?> findPlayerById,
            Func<PlayerSessionState, byte[], Task> broadcastDelegate,
            IAccountRepository? repository = null,
            GameDatabase? gameDb = null,
            string host = "127.0.0.1",
            int episodePort = 10003)
        {
            _findPlayerById = findPlayerById;
            _broadcastDelegate = broadcastDelegate;
            _repository = repository;
            _gameDb = gameDb;
            _host = host;
            _episodePort = episodePort;
        }

        /// <summary>
        /// 0x765E (30302): MsgLobbyLeaveNtf - Player exits episode lobby modal back to campus field
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C0A74 (TSchoolSession.sub_006C0A74) & _Unit49.pas:21842 (TChara.ReturnToField)
        /// </summary>
        [PacketHandler(PacketOpcode.MsgLobbyLeaveNtf)]
        public async Task HandleLobbyLeaveNtfAsync(PlayerSessionState state, PacketReader reader)
        {
            var player = state.Player;
            Logger.Info($"[Lobby] '{player.CharacterName}' exited Episode Lobby back to Field {player.FieldId} at ({player.Position.X}, {player.Position.Y}).");

            // 1. Confirm modal dismissal (0x765E)
            await state.Session.SendAsync(YogurtingPackets.MakeLobbyLeaveNtf());

            // 2. Trigger field re-entry at current spawn/field position (0x795A)
            await state.Session.SendAsync(YogurtingPackets.MakeGameFieldLoadingStartNtf(player.FieldId, player.Position.X, player.Position.Y));
        }

        /// <summary>
        /// 0x7664 (30308): MsgLobbySelectEpisodeReq - Player selects episode or category tab in lobby
        /// SRC: 30308.dms / 30309.dms & Delphi Quartet
        /// </summary>
        [PacketHandler(PacketOpcode.MsgLobbySelectEpisodeReq)]
        public async Task HandleLobbySelectEpisodeReqAsync(PlayerSessionState state, PacketReader reader)
        {
            int episodeId = reader.Remaining >= 4 ? reader.ReadInt32() : 0;
            Logger.Info($"[Lobby] '{state.Player.CharacterName}' selected Episode #{episodeId} in Lobby.");
            await state.Session.SendAsync(YogurtingPackets.MakeLobbySelectEpisodeAns(1, episodeId));
        }

        /// <summary>
        /// 0x7660 (30304): MsgLobbyPageSelectReq - Player selects room page
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C0A80 (TMsgLobbySelectPageAns) & 30304.dms
        /// </summary>
        [PacketHandler(PacketOpcode.MsgLobbyPageSelectReq)]
        public async Task HandleLobbyPageSelectReqAsync(PlayerSessionState state, PacketReader reader)
        {
            int pageNum = reader.Remaining >= 4 ? reader.ReadInt32() : 1;
            await state.Session.SendAsync(YogurtingPackets.MakeLobbyPageSelectAns(pageNum));
            var rooms = _activeRooms.Values.ToList();
            await state.Session.SendAsync(YogurtingPackets.MakeLobbyRoomListAns(rooms));
        }

        /// <summary>
        /// 0x7668 (30312): MsgLobbyRoomListReq - Refresh room list
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C3450 & 30312.dms
        /// </summary>
        [PacketHandler(PacketOpcode.MsgLobbyRoomListReq)]
        public async Task HandleLobbyRoomListReqAsync(PlayerSessionState state, PacketReader reader)
        {
            var rooms = _activeRooms.Values.ToList();
            await state.Session.SendAsync(YogurtingPackets.MakeLobbyRoomListAns(rooms));
        }

        /// <summary>
        /// 0x766A (30314): MsgLobbyCreateRoomReq - Create Episode Waiting Room
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C3500 & 30314.dms
        /// </summary>
        [PacketHandler(PacketOpcode.MsgLobbyCreateRoomReq)]
        public async Task HandleLobbyCreateRoomReqAsync(PlayerSessionState state, PacketReader reader)
        {
            var player = state.Player;
            EpisodeRoom room;

            lock (_roomLock)
            {
                ushort roomId = _nextRoomId++;
                room = new EpisodeRoom
                {
                    RoomId = roomId,
                    LobbyId = 1,
                    Title = $"Room #{roomId} - {player.CharacterName}",
                    // HARDCODED: Default introductory episode ID (101) for newly created waiting rooms
                    EpisodeTypeId = 101,
                    MaxUsers = 4,
                    Status = 0
                };

                // Add creator as host
                room.Members.Add(new WaitRoomMember
                {
                    CharacterId = player.CharacterId,
                    CharacterName = player.CharacterName,
                    Gender = (byte)player.Gender,
                    Grade = (byte)player.Grade,
                    // HARDCODED: Default fallback weapon type ID (Wooden Practice Blade 140001) for room members
                    WeaponTypeId = 140001,
                    TeamId = 0,
                    PhoneNumber = int.TryParse(player.TelNumber, out var tel) ? tel : 3456,
                    IsHost = true,
                    IsReady = true
                });

                _activeRooms[roomId] = room;
            }

            Logger.Info($"[Lobby] '{player.CharacterName}' created Waiting Room #{room.RoomId} ('{room.Title}').");

            // 1. Confirm room creation (0x766B)
            await state.Session.SendAsync(YogurtingPackets.MakeLobbyCreateRoomAns(room.RoomId, room.LobbyId));

            // 2. Send Waiting Room info (0x7679)
            await state.Session.SendAsync(YogurtingPackets.MakeWaitRoomInfoAns(room));
        }

        /// <summary>
        /// 0x766F (30319): MsgLobbyJoinRoomReq - Join existing Waiting Room
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C3600 & 30319.dms
        /// </summary>
        [PacketHandler(PacketOpcode.MsgLobbyJoinRoomReq)]
        public async Task HandleLobbyJoinRoomReqAsync(PlayerSessionState state, PacketReader reader)
        {
            ushort roomId = reader.Remaining >= 2 ? reader.ReadUInt16() : (ushort)1;
            var player = state.Player;

            EpisodeRoom? room;
            bool joined = false;

            lock (_roomLock)
            {
                if (_activeRooms.TryGetValue(roomId, out room))
                {
                    if (room.Members.Count < room.MaxUsers)
                    {
                        if (!room.Members.Any(m => m.CharacterId == player.CharacterId))
                        {
                            room.Members.Add(new WaitRoomMember
                            {
                                CharacterId = player.CharacterId,
                                CharacterName = player.CharacterName,
                                Gender = (byte)player.Gender,
                                Grade = (byte)player.Grade,
                                WeaponTypeId = 140001,
                                TeamId = (ushort)(room.Members.Count % room.TeamCount),
                                PhoneNumber = int.TryParse(player.TelNumber, out var tel) ? tel : 3456,
                                IsHost = false,
                                IsReady = false
                            });
                        }
                        joined = true;
                    }
                }
            }

            if (!joined || room == null)
            {
                await state.Session.SendAsync(YogurtingPackets.MakeLobbyJoinRoomAns(0)); // Fail
                return;
            }

            Logger.Info($"[Lobby] '{player.CharacterName}' joined Waiting Room #{room.RoomId}.");

            // 1. Confirm join (0x7670)
            await state.Session.SendAsync(YogurtingPackets.MakeLobbyJoinRoomAns(1));

            // 2. Broadcast updated waiting room info to all room members (0x7679)
            byte[] waitRoomPkt = YogurtingPackets.MakeWaitRoomInfoAns(room);
            foreach (var member in room.Members)
            {
                var memberSession = _findPlayerById(member.CharacterId);
                if (memberSession != null)
                {
                    await memberSession.Session.SendAsync(waitRoomPkt);
                }
            }
        }

        /// <summary>
        /// 0x7678 (30328): MsgWaitRoomInfoReq - Request waiting room status
        /// </summary>
        [PacketHandler(PacketOpcode.MsgWaitRoomInfoReq)]
        public async Task HandleWaitRoomInfoReqAsync(PlayerSessionState state, PacketReader reader)
        {
            var player = state.Player;
            var room = _activeRooms.Values.FirstOrDefault(r => r.Members.Any(m => m.CharacterId == player.CharacterId));
            if (room != null)
            {
                await state.Session.SendAsync(YogurtingPackets.MakeWaitRoomInfoAns(room));
            }
        }

        /// <summary>
        /// 0x768C (30348): MsgWaitRoomReadyStartReq - Ready / Start Episode
        /// If guest: toggles Ready status (0x768D).
        /// If host: initiates Episode launch and redirects all players to EpisodeServer :10003!
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C3850 & 30348.dms
        /// </summary>
        [PacketHandler(PacketOpcode.MsgWaitRoomReadyStartReq)]
        public async Task HandleWaitRoomReadyStartReqAsync(PlayerSessionState state, PacketReader reader)
        {
            var player = state.Player;
            EpisodeRoom? room;
            WaitRoomMember? selfMember;

            lock (_roomLock)
            {
                room = _activeRooms.Values.FirstOrDefault(r => r.Members.Any(m => m.CharacterId == player.CharacterId));
                selfMember = room?.Members.FirstOrDefault(m => m.CharacterId == player.CharacterId);
            }

            if (room == null || selfMember == null) return;

            if (selfMember.IsHost)
            {
                // HOST: Launch Episode Mission!
                Logger.Info($"[Lobby] Host '{player.CharacterName}' started Episode in Room #{room.RoomId}! Redirecting {room.Members.Count} players to EpisodeServer.");

                // 1. Broadcast Start Countdown notice (0x768E)
                byte[] startPkt = YogurtingPackets.MakeWaitRoomStartNtf();

                // 2. Redirect all participants to Episode Server (:10003)
                byte[] gotoEpisodePkt = YogurtingPackets.MakeGotoSvrNtf(_host, _episodePort);

                foreach (var m in room.Members)
                {
                    var mSession = _findPlayerById(m.CharacterId);
                    if (mSession != null)
                    {
                        await mSession.Session.SendAsync(startPkt);
                        await mSession.Session.SendAsync(gotoEpisodePkt);
                    }
                }

                // Mark room in progress
                room.Status = 2;
            }
            else
            {
                // GUEST: Toggle ready status
                selfMember.IsReady = !selfMember.IsReady;
                Logger.Info($"[Lobby] '{player.CharacterName}' toggled Ready: {selfMember.IsReady}.");

                byte[] readyPkt = YogurtingPackets.MakeWaitRoomReadyNtf(player.CharacterId, selfMember.IsReady ? 1 : 0);
                foreach (var m in room.Members)
                {
                    var mSession = _findPlayerById(m.CharacterId);
                    if (mSession != null)
                    {
                        await mSession.Session.SendAsync(readyPkt);
                    }
                }
            }
        }

        /// <summary>
        /// 0x7680 (30336): MsgWaitRoomSelectTeamReq - Select Team Slot (Red/Blue/None)
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C1374 (sub_006C1374)
        /// Payload: ReadInt32(roomId), ReadByte(teamId)
        /// Broadcasts 0x7680 (MsgWaitRoomSelectTeamNtf) to room members.
        /// </summary>
        [PacketHandler(PacketOpcode.MsgWaitRoomSelectTeamReq)]
        public async Task HandleWaitRoomSelectTeamReqAsync(PlayerSessionState state, PacketReader reader)
        {
            if (reader.Remaining < 5) return;
            int roomId = reader.ReadInt32();
            byte teamId = reader.ReadByte();
            var player = state.Player;

            EpisodeRoom? room;
            lock (_roomLock)
            {
                room = _activeRooms.Values.FirstOrDefault(r => r.Members.Any(m => m.CharacterId == player.CharacterId));
                var selfMember = room?.Members.FirstOrDefault(m => m.CharacterId == player.CharacterId);
                if (selfMember != null)
                {
                    selfMember.TeamId = teamId;
                }
            }

            if (room != null)
            {
                byte[] teamNtf = YogurtingPackets.MakeWaitRoomSelectTeamNtf(player.CharacterId, teamId);
                foreach (var m in room.Members)
                {
                    var mSession = _findPlayerById(m.CharacterId);
                    if (mSession != null)
                    {
                        await mSession.Session.SendAsync(teamNtf);
                    }
                }
                Logger.Info($"[Lobby] '{player.CharacterName}' selected Team #{teamId} in Room #{room.RoomId}.");
            }
        }

        /// <summary>
        /// 0x767A (30330): MsgWaitRoomEditReq - Edit Wait Room Settings
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C120C (sub_006C120C)
        /// Payload: ReadInt32(idEpisodeType), ReadWStr(42, title), ReadByteBool(isPrivate), ReadByteBool(isTeam), ReadByte(maxUsers)
        /// </summary>
        [PacketHandler(PacketOpcode.MsgWaitRoomEditReq)]
        public async Task HandleWaitRoomEditReqAsync(PlayerSessionState state, PacketReader reader)
        {
            if (reader.Remaining < 4) return;
            int episodeType = reader.ReadInt32();
            string title = reader.Remaining >= 84 ? reader.ReadUnicodeString(42).TrimEnd('\0') : "Room";
            bool isPrivate = reader.Remaining >= 1 && reader.ReadByte() != 0;
            bool isTeam = reader.Remaining >= 1 && reader.ReadByte() != 0;
            byte maxUsers = reader.Remaining >= 1 ? reader.ReadByte() : (byte)4;

            var player = state.Player;
            EpisodeRoom? room;

            lock (_roomLock)
            {
                room = _activeRooms.Values.FirstOrDefault(r => r.Members.Any(m => m.CharacterId == player.CharacterId));
                if (room != null)
                {
                    var host = room.Members.FirstOrDefault(m => m.IsHost);
                    if (host != null && host.CharacterId == player.CharacterId)
                    {
                        room.EpisodeTypeId = (uint)episodeType;
                        room.Title = title;
                        room.HasPassword = isPrivate ? (byte)1 : (byte)0;
                        room.TeamCount = isTeam ? (byte)2 : (byte)1;
                        room.MaxUsers = Math.Clamp(maxUsers, (byte)1, (byte)8);
                    }
                }
            }

            if (room != null)
            {
                await state.Session.SendAsync(YogurtingPackets.MakeWaitRoomEditAns(1));
                // Broadcast updated room info to room members
                byte[] infoAns = YogurtingPackets.MakeWaitRoomInfoAns(room);
                foreach (var m in room.Members)
                {
                    var mSession = _findPlayerById(m.CharacterId);
                    if (mSession != null)
                    {
                        await mSession.Session.SendAsync(infoAns);
                    }
                }
                Logger.Info($"[Lobby] Host '{player.CharacterName}' updated settings for Room #{room.RoomId} ('{title}').");
            }
        }

        /// <summary>
        /// 0x768D (30349): MsgWaitRoomLeaveReq - Leave Wait Room & Return to Lobby
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C145C (sub_006C145C)
        /// </summary>
        [PacketHandler(PacketOpcode.MsgWaitRoomLeaveReq)]
        public async Task HandleWaitRoomLeaveReqAsync(PlayerSessionState state, PacketReader reader)
        {
            var player = state.Player;
            EpisodeRoom? room;
            bool wasHost = false;

            lock (_roomLock)
            {
                room = _activeRooms.Values.FirstOrDefault(r => r.Members.Any(m => m.CharacterId == player.CharacterId));
                if (room != null)
                {
                    var self = room.Members.FirstOrDefault(m => m.CharacterId == player.CharacterId);
                    if (self != null)
                    {
                        wasHost = self.IsHost;
                        room.Members.Remove(self);
                    }

                    if (room.Members.Count == 0)
                    {
                        _activeRooms.TryRemove(room.RoomId, out _);
                    }
                    else if (wasHost)
                    {
                        // Pass host to next player
                        room.Members[0].IsHost = true;
                    }
                }
            }

            // Return player to Lobby browsing state (0x7662)
            await state.Session.SendAsync(YogurtingPackets.MakeLobbyPageInfoNtf(1, (ushort)_activeRooms.Count));
            await state.Session.SendAsync(YogurtingPackets.MakeLobbyRoomListAns(_activeRooms.Values.ToList()));

            if (room != null && room.Members.Count > 0)
            {
                byte[] roomInfo = YogurtingPackets.MakeWaitRoomInfoAns(room);
                foreach (var m in room.Members)
                {
                    var mSession = _findPlayerById(m.CharacterId);
                    if (mSession != null)
                    {
                        await mSession.Session.SendAsync(roomInfo);
                    }
                }
            }

            Logger.Info($"[Lobby] '{player.CharacterName}' left Wait Room and returned to Lobby.");
        }

        /// <summary>
        /// 0x766C (30316): MsgLobbyPageInfoReq - Query Current Page Status
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C0D8C
        /// </summary>
        [PacketHandler(PacketOpcode.MsgLobbyPageInfoReq)]
        public async Task HandlePageInfoReqAsync(PlayerSessionState state, PacketReader reader)
        {
            await state.Session.SendAsync(YogurtingPackets.MakeLobbyPageInfoNtf(1, (ushort)_activeRooms.Count));
        }

        /// <summary>
        /// 0x766D (30317): MsgLobbyReserveJoinRoomReq - Reserve Slot in Room
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C0E2C
        /// </summary>
        [PacketHandler(PacketOpcode.MsgLobbyReserveJoinRoomReq)]
        public async Task HandleReserveJoinAsync(PlayerSessionState state, PacketReader reader)
        {
            ushort roomId = reader.Remaining >= 2 ? reader.ReadUInt16() : (ushort)1;
            int rc = _activeRooms.TryGetValue(roomId, out var room) && room.CurrentUsers < room.MaxUsers ? 1 : 0;
            await state.Session.SendAsync(YogurtingPackets.MakeLobbyReserveJoinRoomAns(rc));
        }

        /// <summary>
        /// 0x7671 (30321): MsgLobbyRefreshPageReq - Refresh Lobby Page
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C10A8
        /// </summary>
        [PacketHandler(PacketOpcode.MsgLobbyRefreshPageReq)]
        public async Task HandleRefreshPageReqAsync(PlayerSessionState state, PacketReader reader)
        {
            await state.Session.SendAsync(YogurtingPackets.MakeLobbyPageInfoNtf(1, (ushort)_activeRooms.Count));
            await state.Session.SendAsync(YogurtingPackets.MakeLobbyRoomListAns(_activeRooms.Values.ToList()));
        }

        /// <summary>
        /// 0x7672 (30322): MsgLobbyQuickJoinReq - Matchmake into Any Open Room
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C1168
        /// </summary>
        [PacketHandler(PacketOpcode.MsgLobbyQuickJoinReq)]
        public async Task HandleQuickJoinAsync(PlayerSessionState state, PacketReader reader)
        {
            var player = state.Player;
            EpisodeRoom? targetRoom = null;

            lock (_roomLock)
            {
                targetRoom = _activeRooms.Values.FirstOrDefault(r => r.Status == 0 && r.CurrentUsers < r.MaxUsers);
                if (targetRoom != null)
                {
                    targetRoom.Members.Add(new WaitRoomMember
                    {
                        CharacterId = player.CharacterId,
                        CharacterName = player.CharacterName,
                        Gender = (byte)(player.Gender == GenderType.Female ? 1 : 0),
                        Grade = (byte)player.Level,
                        IsHost = false,
                        IsReady = false
                    });
                }
            }

            if (targetRoom != null)
            {
                await state.Session.SendAsync(YogurtingPackets.MakeLobbyQuickJoinRoomAns(1));
                await state.Session.SendAsync(YogurtingPackets.MakeWaitRoomInfoAns(targetRoom));
                Logger.Info($"[Lobby] '{player.CharacterName}' Quick-Joined Room #{targetRoom.RoomId}.");
            }
            else
            {
                await state.Session.SendAsync(YogurtingPackets.MakeLobbyQuickJoinRoomAns(0));
            }
        }

        /// <summary>
        /// 0x7682 (30338): MsgWaitRoomInviteReq - Invite Peer to Waiting Room
        /// SRC: server_legacy/DELPHI PROJECT/_Unit67.pas:006C13B4
        /// </summary>
        [PacketHandler(PacketOpcode.MsgWaitRoomInviteReq)]
        public async Task HandleWaitRoomInviteAsync(PlayerSessionState state, PacketReader reader)
        {
            int targetCharaId = reader.Remaining >= 4 ? reader.ReadInt32() : 0;
            await state.Session.SendAsync(YogurtingPackets.MakeWaitRoomTestInviteAns(targetCharaId, 1));
        }
    }
}
