using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Yogurting.Core.Logging;
using Yogurting.Core.Models;
using Yogurting.Core.Network;
using Yogurting.Data.Repositories;

namespace Yogurting.Server.Handlers.Auth
{
    /// <summary>
    /// Handles all Login & Authentication packets (Port 10000).
    /// Exact 1-to-1 match with Delphi quartet.exe Login Server unit (0x006BE9F8..0x006BF2A0).
    /// </summary>
    public sealed class AuthHandlers
    {
        private readonly IAccountRepository _accountRepository;
        private readonly Random _random = new();
        private readonly string _serverBindIp;
        private readonly int _fieldPort;
        private readonly int _commPort;

        public AuthHandlers(IAccountRepository accountRepository, string serverBindIp = "127.0.0.1", int fieldPort = 10002, int commPort = 10004)
        {
            _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
            _serverBindIp = serverBindIp;
            _fieldPort = fieldPort;
            _commPort = commPort;
        }

        /// <summary>
        /// 0x4E21 (20001): MsgCheckVersionNtf - Client initial connection handshake
        /// </summary>
        [PacketHandler(PacketOpcode.MsgCheckVersionNtf)]
        public async Task HandleCheckVersionAsync(ClientSession session, byte[] packetData)
        {
            Logger.Info($"[LoginServer] Client connected from {session.RemoteEndPoint}. Awaiting handshake...");
            await session.SendAsync(YogurtingPackets.MakeAuthTypeNtf(0));
        }

        /// <summary>
        /// 0x7596 (30102): MsgLoginAuthReq - User Account & Password Authentication
        /// Delphi 0x006BEAC4
        /// </summary>
        [PacketHandler(PacketOpcode.MsgLoginAuthReq)]
        public async Task HandleLoginAuthAsync(ClientSession session, byte[] packetData)
        {
            try
            {
                // Format: [Header 6B] [Username Unicode 50B] [Password MD5 Hash ASCII 32B]
                string username = string.Empty;
                string passwordHash = string.Empty;

                if (packetData.Length >= 56)
                {
                    username = Encoding.Unicode.GetString(packetData, 6, 50).TrimEnd('\0');
                }

                if (packetData.Length >= 88)
                {
                    passwordHash = Encoding.ASCII.GetString(packetData, 56, 32).TrimEnd('\0');
                }

                if (string.IsNullOrWhiteSpace(username))
                {
                    username = "test";
                }

                Logger.Info($"[LoginServer] Authenticating User: '{username}'");

                var player = await _accountRepository.GetByUsernameAsync(username);
                if (player == null)
                {
                    // If username begins with "make_", auto-register new account and go to char creation
                    if (username.StartsWith("make_", StringComparison.OrdinalIgnoreCase))
                    {
                        string realUsername = username.Substring(5);
                        Logger.Info($"[LoginServer] New Account registration requested for '{realUsername}'");
                        player = new Player(realUsername, string.Empty, SchoolType.EstivaAcademy, GenderType.Female)
                        {
                            PasswordHash = passwordHash,
                            HasCharacter = false
                        };
                        await _accountRepository.SaveAsync(player);
                        session.AccountId = realUsername;

                        // Send RC 10012 to trigger Character Creation screen!
                        await session.SendAsync(YogurtingPackets.MakeLoginAuthFailAns(10012));
                        return;
                    }

                    Logger.Warn($"[LoginServer] Auth Failed: Account '{username}' not found in database.");
                    // RC 10002: Account does not exist / Unregistered ID
                    await session.SendAsync(YogurtingPackets.MakeLoginAuthFailAns(10002));
                    return;
                }

                // 2. Validate Password MD5 Hash
                if (!string.IsNullOrEmpty(player.PasswordHash) &&
                    !string.IsNullOrEmpty(passwordHash) &&
                    !string.Equals(player.PasswordHash, passwordHash, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Warn($"[LoginServer] Auth Failed: Invalid password for '{username}'");
                    // RC 10003: Wrong password
                    await session.SendAsync(YogurtingPackets.MakeLoginAuthFailAns(10003));
                    return;
                }

                // 3. Check if account is banned/denied
                if (player.AuthType == "denied")
                {
                    Logger.Warn($"[LoginServer] Auth Failed: Account '{username}' is banned/blocked.");
                    // RC 10016: Blocked/Banned
                    await session.SendAsync(YogurtingPackets.MakeLoginAuthFailAns(10016));
                    return;
                }

                // 4. Check if Character is created
                if (!player.HasCharacter || string.IsNullOrEmpty(player.CharacterName))
                {
                    Logger.Info($"[LoginServer] Account '{username}' has no character. Redirecting to Character Creation (RC 10012)...");
                    session.AccountId = username;
                    await session.SendAsync(YogurtingPackets.MakeLoginAuthFailAns(10012));
                    return;
                }

                // 5. Authentication Succeeded
                player.SessionKey = _random.Next(100000, 999999);
                session.AccountId = username;
                session.SessionKey = player.SessionKey;
                session.CharaId = player.CharaId;

                await _accountRepository.SaveAsync(player);

                // Dispatch full LoginAuthAns (458 bytes)
                byte[] response = YogurtingPackets.MakeLoginAuthAns(player);
                await session.SendAsync(response);

                Logger.Info($"[LoginServer] Full Login Auth Response (458 bytes) dispatched for '{player.CharacterName}' (CharaId={player.CharaId}, SessionKey={player.SessionKey})!");
            }
            catch (Exception ex)
            {
                Logger.Error($"[LoginServer] Auth Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x759A (30106): MsgLoginJoinGameReq - Select Server & Redirect to Field / Comm
        /// Delphi 0x006BED6C
        /// </summary>
        [PacketHandler(PacketOpcode.MsgLoginJoinGameReq)]
        public async Task HandleJoinGameAsync(ClientSession session, byte[] packetData)
        {
            try
            {
                Logger.Info($"[LoginServer] Game Join Request received. SessionKey={session.SessionKey}, CharaId={session.CharaId}");

                byte[] response = YogurtingPackets.MakeLoginJoinGameAns(
                    _serverBindIp,
                    _fieldPort,
                    _commPort,
                    session.SessionKey,
                    session.CharaId
                );

                await session.SendAsync(response);
                Logger.Info($"[LoginServer] Game Join Response sent! Redirecting to FieldServer on {_serverBindIp}:{_fieldPort} and CommServer on {_serverBindIp}:{_commPort}!");
            }
            catch (Exception ex)
            {
                Logger.Error($"[LoginServer] Join Game Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x75A4 (30116): MsgLoginCheckNameReq - Duplicate Character Name Check
        /// Delphi 0x006BEFC0
        /// </summary>
        [PacketHandler(PacketOpcode.MsgLoginCheckNameReq)]
        public async Task HandleCheckNameAsync(ClientSession session, byte[] packetData)
        {
            try
            {
                string charName = string.Empty;
                if (packetData.Length >= 32)
                {
                    charName = Encoding.Unicode.GetString(packetData, 6, 26).TrimEnd('\0');
                }

                bool isTaken = await _accountRepository.GetByCharacterNameAsync(charName) != null;
                Logger.Info($"[LoginServer] Check Character Name: '{charName}' -> {(isTaken ? "TAKEN" : "AVAILABLE")}");

                // Exact Delphi TMsgLoginCheckNameAns (0x75A5): RC(4B) + Name(26B) + Padding(2B 0xCC) = 32B payload
                byte[] response = YogurtingPackets.MakeLoginCheckNameAns(charName, !isTaken);
                await session.SendAsync(response);
            }
            catch (Exception ex)
            {
                Logger.Error($"[LoginServer] Check Name Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x75A6 (30118): MsgLoginCheckPhoneReq - Check Duplicate Phone Number
        /// Delphi 0x006BF064
        /// </summary>
        [PacketHandler(PacketOpcode.MsgLoginCheckPhoneReq)]
        public async Task HandleCheckPhoneAsync(ClientSession session, byte[] packetData)
        {
            try
            {
                int phone = packetData.Length >= 10 ? BitConverter.ToInt32(packetData, 6) : 1001;
                Logger.Info($"[LoginServer] Check Phone Number: '{phone}' -> AVAILABLE");

                byte[] response = YogurtingPackets.MakeLoginCheckPhoneAns(phone, true);
                await session.SendAsync(response);
            }
            catch (Exception ex)
            {
                Logger.Error($"[LoginServer] Check Phone Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x75A8 (30120): MsgLoginMakeCharReq - Character Creation
        /// Delphi 0x006BF098 exact 1-to-1 record layout
        /// </summary>
        [PacketHandler(PacketOpcode.MsgLoginMakeCharReq)]
        public async Task HandleMakeCharAsync(ClientSession session, byte[] packetData)
        {
            try
            {
                // [Header 6B] [WorldID 4B] [Name 28B] [Tel 4B] [Gender 4B] [School 4B] [Face 4B] [Hair 4B] [Skin 4B] [Month 1B] [Day 1B] [Blood 1B]
                int worldId = BitConverter.ToInt32(packetData, 6);
                string charName = Encoding.Unicode.GetString(packetData, 10, 28).TrimEnd('\0');
                int telNumber = BitConverter.ToInt32(packetData, 38);
                int gender = BitConverter.ToInt32(packetData, 42);
                int school = BitConverter.ToInt32(packetData, 46);
                int face = BitConverter.ToInt32(packetData, 50);
                int hair = BitConverter.ToInt32(packetData, 54);
                int skin = BitConverter.ToInt32(packetData, 58);
                byte birthMonth = packetData.Length > 62 ? packetData[62] : (byte)5;
                byte birthDay = packetData.Length > 63 ? packetData[63] : (byte)29;
                byte bloodType = packetData.Length > 64 ? packetData[64] : (byte)1;

                Logger.Info($"[LoginServer] Creating Character '{charName}' (School={school}, Gender={gender}, Face={face}, Hair={hair}, Tel={telNumber})");

                var player = await _accountRepository.GetByUsernameAsync(session.AccountId ?? charName) ?? new Player(charName, charName);
                player.CharacterName = charName;
                player.School = (SchoolType)school;
                player.Gender = (GenderType)gender;
                player.FaceId = face;
                player.HairId = hair;
                player.SkinTone = skin;
                player.BirthMonth = birthMonth;
                player.BirthDay = birthDay;
                player.BloodType = bloodType;
                player.TelNumber = telNumber.ToString();
                player.HasCharacter = true;

                // Load initial spawn location, stats, and inventory from starter_items.json
                var spawn = Yogurting.Core.Models.StarterConfigLoader.GetSpawnPoint(player.School);
                player.FieldId = spawn.FieldId;
                player.Position = new Position(spawn.X, spawn.Y, 0f);
                player.SaveFieldId = player.FieldId;
                player.SavePosition = player.Position;
                Yogurting.Core.Models.StarterConfigLoader.ApplyDefaultStats(player);
                Yogurting.Core.Models.StarterConfigLoader.ApplyToPlayer(player);

                await _accountRepository.SaveAsync(player);

                // Send 0x75A9 (30121) with complete TMsgLoginCreateCharAns (RC + WriteCharDispInfo)
                byte[] response = YogurtingPackets.MakeLoginMakeCharAns(1, player);
                await session.SendAsync(response);

                Logger.Info($"[LoginServer] Character '{charName}' successfully created & saved!");
            }
            catch (Exception ex)
            {
                Logger.Error($"[LoginServer] Make Character Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x75AA (30122): MsgLoginDeleteCharReq - Delete/Reset Character Request
        /// Delphi 0x006BF1A0
        /// </summary>
        [PacketHandler(PacketOpcode.MsgLoginDeleteCharReq)]
        public async Task HandleDeleteCharAsync(ClientSession session, byte[] packetData)
        {
            try
            {
                string username = session.AccountId ?? "test";
                Logger.Info($"[LoginServer] Delete Character Request for account '{username}'");

                var player = await _accountRepository.GetByUsernameAsync(username);
                if (player != null)
                {
                    player.HasCharacter = false;
                    player.CharacterName = string.Empty;
                    await _accountRepository.SaveAsync(player);
                }

                // Send 0x75AB (30123) confirmation so client transitions smoothly to Character Creation screen
                await session.SendAsync(YogurtingPackets.MakeLoginDeleteCharAns(true));
                Logger.Info($"[LoginServer] Character for '{username}' deleted successfully. Dispatched MsgLoginDeleteCharAns.");
            }
            catch (Exception ex)
            {
                Logger.Error($"[LoginServer] Delete Character Error: {ex.Message}");
            }
        }
    }
}
