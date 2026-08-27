using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Yogurting.Core.Models;

namespace Yogurting.Core.Network
{
    /// <summary>
    /// Packet Serialization Utilities and Packet Builders directly mapped from Delphi quartet.exe.
    /// Uses Yogurting Little-Endian length prefix: [PayloadLength (TotalBytes - 6): Int32][Opcode: UInt16][Payload...].
    /// </summary>
    public static class YogurtingPackets
    {
        // ====================================================================
        // SYSTEM & TIME PACKETS
        // ====================================================================

        /// <summary>
        /// 0x4E25 (20005): TMsgWorldTimeNtf - Sets season, time-of-day lighting, and world clock ticks
        /// Delphi 0x005A910C:
        ///   WriteByte(Season)
        ///   WriteByte(Clock)
        ///   FillBuffer(0xCC, 2)
        ///   WriteInt32(Season * 15000)
        /// </summary>
        public static byte[] MakeWorldTimeNtf(byte season = 3, byte clock = 0)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgWorldTimeNtf);
            writer.WriteByte(season);                       // Season: 3 (Standard Daytime Environment)
            writer.WriteByte(clock);                        // Clock: 0 = Daylight
            writer.WriteByte(0xCC);                         // Padding
            writer.WriteByte(0xCC);                         // Padding
            writer.WriteInt32(0x0000AFC8);                  // World tick count (45000 - exact match for Quartet)
            return writer.Build();
        }

        /// <summary>
        /// 0x4E26 (20006): TMsgTimeNtf - Server Unix timestamp / TickCount
        /// Delphi 0x005A9184
        /// </summary>
        public static byte[] MakeTimeNtf()
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgTimeNtf);
            writer.WriteInt32(Environment.TickCount);
            return writer.Build();
        }

        /// <summary>
        /// 0x5210 (21008): TMsgGotoSvrNtf - Instructs client to switch to another server IP & Port
        /// </summary>
        public static byte[] MakeGotoSvrNtf(string ipAddress, int port)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGotoSvrNtf);
            writer.WriteBytes(IPAddress.Parse(ipAddress).GetAddressBytes());
            writer.WriteUInt16((ushort)port);
            return writer.Build();
        }

        // ====================================================================
        // LOGIN SERVER PACKETS (PORT 10000)
        // ====================================================================

        /// <summary>
        /// 0x7595 (30101): TMsgAuthTypeNtf - Authentication mode notice (AuthType = 0 for none)
        /// </summary>
        public static byte[] MakeAuthTypeNtf(int authType = 0)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgAuthTypeNtf);
            writer.WriteInt32(authType);
            return writer.Build();
        }

        /// <summary>
        /// 0x7597 (30103): TMsgLoginAuthenticationAns - Full Character Display Profile (458 bytes total)
        /// </summary>
        public static byte[] MakeLoginAuthAns(Player player, string worldName = "Estiva") => MakeLoginAuthenticationAns(player, worldName);

        public static byte[] MakeLoginAuthenticationAns(Player player, string worldName = "Estiva")
        {
            byte[] worldNameBytes = new byte[22];
            byte[] rawWorld = Encoding.Unicode.GetBytes(worldName + "\0");
            Buffer.BlockCopy(rawWorld, 0, worldNameBytes, 0, Math.Min(rawWorld.Length, worldNameBytes.Length));

            byte[] schoolNameBytes = new byte[66]; // Zeros

            byte[] charNameBytes = new byte[28];
            byte[] rawChar = Encoding.Unicode.GetBytes(player.CharacterName + "\0");
            Buffer.BlockCopy(rawChar, 0, charNameBytes, 0, Math.Min(rawChar.Length, 26));
            charNameBytes[26] = 0xCC;
            charNameBytes[27] = 0xCC;

            byte[] clubNameBytes = new byte[28];
            clubNameBytes[26] = 0xCC;
            clubNameBytes[27] = 0xCC;

            using var writer = PacketWriter.Create(PacketOpcode.MsgLoginAuthAns);
            writer.WriteUInt32(1);                          // RC = 1 (Account has character)
            writer.WriteBytes(worldNameBytes);              // World Name (22 bytes UTF16)
            writer.WriteBytes(schoolNameBytes);             // School Name (66 bytes)

            // === TChara.WriteCharDispInfo (356 bytes) ===
            WriteCharDispInfo(writer, player);

            // Queue Wait Count
            writer.WriteUInt32(0);

            return writer.Build();
        }

        /// <summary>
        /// 0x7597 (30103): TMsgLoginAuthenticationAns - Authentication Failure / No Character (Exact 458 bytes matching Delphi 0x005AA952)
        /// Return Codes (RC):
        ///   10002 = Account does not exist
        ///   10003 = Wrong password
        ///   10005 = Already logged in
        ///   10012 = No character (Redirect to Character Creation)
        ///   10015 = Invalid account ID
        ///   10016 = Account banned / blocked
        /// </summary>
        public static byte[] MakeLoginAuthFailAns(int returnCode)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgLoginAuthAns);
            writer.WriteUInt32((uint)returnCode);           // RC (e.g. 10003 for wrong password)
            writer.WriteBytes(new byte[22]);                // World Name (22 bytes zeroes)
            writer.WriteBytes(new byte[66]);                // School Name (66 bytes zeroes)
            for (int i = 0; i < 4; i++) writer.WriteByte(0xCC); // 4 bytes 0xCC
            for (int i = 0; i < 4; i++) writer.WriteByte(0xCC); // 4 bytes 0xCC
            writer.WriteBytes(new byte[26]);                // 26 bytes zeroes
            writer.WriteByte(0xCC); writer.WriteByte(0xCC); // 2 bytes 0xCC
            for (int i = 0; i < 140; i++) writer.WriteByte(0xCC); // 140 bytes 0xCC
            writer.WriteBytes(new byte[184]);               // 184 bytes zeroes
            return writer.Build();                          // Total: 458 bytes (Header 6B + Payload 452B)
        }

        public static void WriteCharDispInfo(PacketWriter writer, Player player)
        {
            byte[] charNameBytes = new byte[28];
            byte[] rawChar = Encoding.Unicode.GetBytes(player.CharacterName + "\0");
            Buffer.BlockCopy(rawChar, 0, charNameBytes, 0, Math.Min(rawChar.Length, 26));
            charNameBytes[26] = 0xCC;
            charNameBytes[27] = 0xCC;

            byte[] clubNameBytes = new byte[28];
            clubNameBytes[26] = 0xCC;
            clubNameBytes[27] = 0xCC;

            writer.WriteUInt32(1);                          // Character ID
            writer.WriteUInt32((uint)player.Grade);         // Grade (1..4)
            writer.WriteBytes(charNameBytes);               // Character Name (28 bytes)
            writer.WriteUInt32((uint)player.School);        // School (1 = Estiva, 2 = So-il)
            writer.WriteByte(0xCC);                         // Padding
            writer.WriteByte(0xCC);
            writer.WriteByte(0xCC);
            writer.WriteByte(0xCC);
            writer.WriteUInt32(uint.TryParse(player.TelNumber, out var tel) ? tel : 3456); // TelNumber
            writer.WriteInt64(player.Money);                // Money (Taff)
            writer.WriteUInt32((uint)player.Gender);        // Gender (0 = Male, 1 = Female)
            writer.WriteUInt32((uint)player.FaceId);        // Face ID
            writer.WriteUInt32((uint)player.HairId);        // Hair ID
            writer.WriteUInt32((uint)player.SkinTone);      // Skin Tone
            writer.WriteBytes(clubNameBytes);               // Club / Guild Name (28 bytes)
            writer.WriteUInt32((uint)player.Level);         // Level (1..99)

            // 9 Equipment Slots: Head (1), Acc1 (2), Bag (3), Weapon (4), Glove (5), Acc2 (6), Top (7), Bottom (8), Shoes (9)
            int[] slotTypeIds = new int[10];
            bool[] slotIsStar = new bool[10];

            for (int slot = 1; slot <= 9; slot++)
            {
                int uid = (slot < player.EquippedSlotUids.Length) ? player.EquippedSlotUids[slot] : 0;
                bool isStar = (slot < player.EquippedSlotIsStar.Length) && player.EquippedSlotIsStar[slot];

                int typeId = GetPlayerItemTypeId(player, uid, (ushort)slot, isStar);
                slotTypeIds[slot] = typeId;
                slotIsStar[slot] = typeId > 1000000;

                writer.WriteUInt32((uint)typeId);
            }

            // 9 IsStarItem LongBools (Delphi TYgPacket.WriteLongBool 0x005A8C84)
            for (int slot = 1; slot <= 9; slot++)
            {
                writer.WriteUInt32(slotIsStar[slot] ? 1u : 0u);
            }

            // 9 Parts x 5 Gem / Reinforce Slots (45 uint32 = 180 bytes)
            for (int i = 0; i < 45; i++) writer.WriteUInt32(0);
        }

        public static int GetPlayerItemTypeId(Player player, int uniqueId, ushort slot = 0, bool isStar = false)
        {
            if (uniqueId == 0) return 0;

            // 1. Check if item exists in Player's StarBeItems (Cash / Star Shop items)
            if (player.StarBeItems != null)
            {
                var starItem = player.StarBeItems.Find(i => i.Id == uniqueId || i.SerialId == uniqueId);
                if (starItem != null && (starItem.TypeId > 0 || starItem.ItemId > 0))
                {
                    return starItem.TypeId > 0 ? starItem.TypeId : starItem.ItemId;
                }
            }

            // 2. Check if item exists in Player's persistent Inventory
            if (player.Inventory != null)
            {
                var invItem = player.Inventory.Find(i => i.Id == uniqueId || i.SerialId == uniqueId);
                if (invItem != null && (invItem.TypeId > 0 || invItem.ItemId > 0))
                {
                    return invItem.TypeId > 0 ? invItem.TypeId : invItem.ItemId;
                }
            }

            // 3. If uniqueId itself is already a direct Item Type ID (> 1000)
            if (uniqueId > 1000)
            {
                return uniqueId;
            }

            // 4. Default School Uniform / Starter Gear Resolution from StarterConfig
            var profile = StarterConfigLoader.GetProfile(player.School, player.Gender);
            if (uniqueId >= 1 && uniqueId <= profile.Equipped.Count)
            {
                return profile.Equipped[uniqueId - 1].ItemId;
            }
            int invIndex = uniqueId - profile.Equipped.Count - 1;
            if (invIndex >= 0 && invIndex < profile.Inventory.Count)
            {
                return profile.Inventory[invIndex].ItemId;
            }

            // Fallback by equip slot index (4=Weapon, 7=Top, 8=Bottom, 9=Shoes)
            if (slot > 0)
            {
                var match = profile.Equipped.Find(e => e.SlotIndex == slot);
                if (match != null) return match.ItemId;
            }

            return 0;
        }

        /// <summary>
        /// 0x759B (30107): TMsgLoginJoinGameAns - School & Comm Server IP & Ports
        /// Exact match of Delphi TMsgLoginJoinGameAns.Create at 0x005AA9E0:
        ///   WriteID(0x759B)
        ///   WriteRC(1)
        ///   WriteInt32(0x3E8)
        ///   WriteInt32(0x5B)
        ///   WriteBuffer(SYSTEMTIME, 16)
        ///   WriteInt32(SessionKey)
        ///   WriteInt32(SchoolIP)
        ///   WriteInt32(SchoolPort)
        ///   WriteInt32(CommIP)
        ///   WriteInt32(CommPort)
        ///   WriteInt32(0x3E8)
        /// </summary>
        public static byte[] MakeLoginJoinGameAns(string serverIp, int schoolPort, int commPort, int sessionKey, int charaId = 1)
        {
            var now = DateTime.Now;
            using var writer = PacketWriter.Create(PacketOpcode.MsgLoginJoinGameAns);
            writer.WriteInt32(1);                               // RC = 1 (SUCCESS) via WriteRC
            writer.WriteInt32(0x3E8);                           // Constant 1000
            writer.WriteInt32(0x5B);                            // World ID 91

            // System Time (SYSTEMTIME struct: 8 x ushort = 16 bytes)
            writer.WriteUInt16((ushort)now.Year);
            writer.WriteUInt16((ushort)now.Month);
            writer.WriteUInt16((ushort)now.DayOfWeek);
            writer.WriteUInt16((ushort)now.Day);
            writer.WriteUInt16((ushort)now.Hour);
            writer.WriteUInt16((ushort)now.Minute);
            writer.WriteUInt16((ushort)now.Second);
            writer.WriteUInt16((ushort)now.Millisecond);

            // 1. SessionKey (auth token for School/Comm servers)
            writer.WriteInt32(sessionKey);

            // 2. School Server IP (4 bytes in network byte order)
            byte[] ipBytes = IPAddress.Parse(serverIp).GetAddressBytes();
            writer.WriteBytes(ipBytes);

            // 3. School Server Port (10002)
            writer.WriteInt32(schoolPort);

            // 4. Comm Server IP (4 bytes in network byte order)
            writer.WriteBytes(ipBytes);

            // 5. Comm Server Port (10004)
            writer.WriteInt32(commPort);

            // 6. Tail constant 1000
            writer.WriteInt32(0x3E8);

            return writer.Build();
        }

        /// <summary>
        /// 0x75A0 (30112): TMsgLoginWorldListAns
        /// </summary>
        public static byte[] MakeWorldListAns(int worldCount = 1)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgLoginWorldListAns);
            writer.WriteUInt32(1);                          // RC = 1 (Success)
            writer.WriteUInt32((uint)worldCount);
            return writer.Build();
        }

        /// <summary>
        /// 0x75A1 (30113): TMsgLoginWorldListNtf - Exact 100-World Table from Delphi Quartet 0x005AAB90
        /// </summary>
        public static byte[] MakeWorldListNtf(string worldName = "Estiva", int worldId = 91)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgLoginWorldListNtf);
            writer.WriteUInt32(1);                          // bLast = 1
            writer.WriteUInt32(1);                          // WorldCount = 1

            // World 1
            byte[] nameBytes = new byte[24];
            byte[] rawName = Encoding.Unicode.GetBytes(worldName + "\0");
            Buffer.BlockCopy(rawName, 0, nameBytes, 0, Math.Min(rawName.Length, nameBytes.Length));
            writer.WriteBytes(nameBytes);
            writer.WriteUInt32((uint)worldId);              // World ID (91)
            writer.WriteUInt32(0);                          // Server Load (0 = Smooth)

            // 99 Empty Padding Worlds
            byte[] emptyName = new byte[24];
            for (int i = 0; i < 99; i++)
            {
                writer.WriteBytes(emptyName);
                writer.WriteUInt32(0);
                writer.WriteUInt32(0);
            }

            return writer.Build();
        }

        /// <summary>
        /// 0x75AE (30126): TMsgLoginResumeNtf
        /// </summary>
        public static byte[] MakeLoginResumeNtf(int sessionId = 1000)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgLoginResumeNtf);
            writer.WriteInt32(sessionId);
            return writer.Build();
        }

        /// <summary>
        /// 0x75A3 (30115): TMsgLoginSchoolListNtf - School Academies (Estiva, So-il)
        /// </summary>
        public static byte[] MakeSchoolListNtf(int worldId = 91)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgLoginSchoolListNtf);
            writer.WriteUInt32(1);                          // bLast = 1
            writer.WriteUInt32((uint)worldId);              // World ID
            writer.WriteUInt32(2);                          // School Count = 2

            // School 1: Estiva Academy (エスティバー)
            byte[] sname1 = new byte[68];
            byte[] raw1 = Encoding.Unicode.GetBytes("\u30A8\u30B9\u30C6\u30A3\u30D0\u30FC\0");
            Buffer.BlockCopy(raw1, 0, sname1, 0, Math.Min(raw1.Length, sname1.Length));
            writer.WriteUInt32(1);
            writer.WriteBytes(sname1);

            // School 2: So-il Academy (宵月)
            byte[] sname2 = new byte[68];
            byte[] raw2 = Encoding.Unicode.GetBytes("\u5BB5\u6708\0");
            Buffer.BlockCopy(raw2, 0, sname2, 0, Math.Min(raw2.Length, sname2.Length));
            writer.WriteUInt32(2);
            writer.WriteBytes(sname2);

            // Schools 3..10 (Padding)
            byte[] emptySchool = new byte[68];
            for (int i = 3; i <= 10; i++)
            {
                writer.WriteUInt32((uint)i);
                writer.WriteBytes(emptySchool);
            }

            return writer.Build();
        }

        /// <summary>
        /// 0x75A9 (30121): TMsgLoginCreateCharAns - Character Creation result (Delphi 0x005AADDC)
        /// </summary>
        public static byte[] MakeLoginMakeCharAns(int resultCode = 1, Player? player = null)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgLoginMakeCharAns);
            writer.WriteInt32(resultCode);                  // RC = 1 via WriteRC
            if (resultCode == 1 && player != null)
            {
                WriteCharDispInfo(writer, player);
            }
            return writer.Build();
        }

        /// <summary>
        /// 0x75A5 (30117): TMsgLoginCheckCharNameAns - Duplicate Name check result
        /// Delphi 0x005AAD20:
        ///   WriteID(0x75A5)
        ///   WriteRC(isAvailable ? 1 : 0x271A)
        ///   WriteWStr(Name, 26 bytes)
        ///   FillBuffer(0xCC, 2)
        /// </summary>
        public static byte[] MakeLoginCheckNameAns(string name, bool isAvailable = true)
        {
            using var writer = PacketWriter.Create((PacketOpcode)0x75A5);
            writer.WriteInt32(isAvailable ? 1 : 0x271A);    // RC = 1 (Available) or 10010 (Taken)
            writer.WriteUnicodeString(name, 13);            // Fixed 26-byte string
            writer.WriteBytes(new byte[] { 0xCC, 0xCC });   // 2-byte padding
            return writer.Build();
        }

        /// <summary>
        /// 0x75A7 (30119): TMsgLoginCheckPhoneAns - Phone number validation (Delphi 0x005AAD88)
        /// </summary>
        public static byte[] MakeLoginCheckPhoneAns(int phone, bool isAvailable = true)
        {
            using var writer = PacketWriter.Create((PacketOpcode)0x75A7);
            writer.WriteInt32(isAvailable ? 1 : 0x271A);    // RC = 1 via WriteRC
            writer.WriteInt32(phone);                       // Validated Phone Number
            return writer.Build();
        }

        /// <summary>
        /// 0x75AB (30123): TMsgLoginDeleteCharAns - Delphi 0x005AAE30
        /// </summary>
        public static byte[] MakeLoginDeleteCharAns(bool success = true)
        {
            using var writer = PacketWriter.Create((PacketOpcode)0x75AB);
            writer.WriteInt32(success ? 1 : 0);             // RC = 1 via WriteRC
            return writer.Build();
        }

        // ====================================================================
        // SCHOOL & FIELD SERVER PACKETS (PORT 10002)
        // ====================================================================

        /// <summary>
        /// 0x799F (31135): TMsgGameAtkMovChangeNtf
        /// Delphi 0x005AEC54: WriteInt32(CharaID), WriteSingle(AtkSpeed), WriteSingle(MovSpeed)
        /// </summary>
        public static byte[] MakeGameAtkMovChangeNtf(int charaId = 1, float atkSpeed = 1.0f, float moveSpeed = 1.0f)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameAtkMovChangeNtf);
            writer.WriteInt32(charaId);
            writer.WriteSingle(atkSpeed);
            writer.WriteSingle(moveSpeed);
            return writer.Build();
        }

        /// <summary>
        /// 0x5212 (21010): TMsgEnterScsNtf
        /// Delphi 0x005A9514: Opcode only
        /// </summary>
        public static byte[] MakeEnterScsNtf()
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgEnterScsNtf);
            return writer.Build();
        }

        /// <summary>
        /// 0x7952 (31058): TMsgGameCharInfoNtf - In-game Character Info (WriteCharPlayInfo)
        /// Delphi 0x005ACDBC & 0x005A8728
        /// </summary>
        public static byte[] MakeGameCharInfoNtf(Player player)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameCharInfoNtf);
            writer.WriteInt32(1);                           // RC = 1 (Success)

            // === TYgPacket.WriteCharPlayInfo (0x005A8728) ===
            writer.WriteInt32(player.CharaId);              // Chara.ID
            writer.WriteUnicodeString(player.CharacterName, 13); // Name (26 bytes / ecx=0x1A)
            writer.WriteInt32(int.TryParse(player.TelNumber, out var tel) ? tel : 3456); // TelNumber
            writer.WriteByte((byte)player.Gender);          // Sex (byte)
            writer.WriteUInt16((ushort)player.School);      // School (word)
            writer.WriteInt64((long)player.CharaId);        // ID as Int64
            writer.WriteInt32(0);                           // Padding
            writer.WriteInt32(player.FaceId);               // Face
            writer.WriteInt32(player.HairId);               // Hair
            writer.WriteInt32(player.SkinTone);             // Skin
            writer.WriteInt32(player.Level);                // Level
            writer.WriteByte((byte)player.Grade);           // Grade (byte)
            writer.WriteInt32(0);                           // Padding
            writer.WriteInt32(0);                           // Padding
            writer.WriteInt64(player.Money);                // Taff (Int64)
            writer.WriteInt64(0);                           // Exp (Int64)
            writer.WriteInt32(player.TaffPoints);           // ShopPoint
            writer.WriteInt32(player.Hp);                   // Current HP (0x0060DD94)
            writer.WriteInt32(player.Sp);                   // Current SP (0x0060FE00)
            writer.WriteInt32(0);                           // 0x0060DDA4
            writer.WriteInt32(0);                           // 0x0060DDB0
            writer.WriteInt32(0);                           // 0x0060DDBC

            writer.WriteInt32(player.FieldId);              // Field.ID
            writer.WriteUInt16((ushort)player.Position.X);  // MapPoint X (word: 124 for So-il, 38 for Estiva)
            writer.WriteUInt16((ushort)player.Position.Y);  // MapPoint Y (word: 165 for So-il, 14 for Estiva)

            // 10 QuickSlot Hotbar Types (10 x Int32 = 40 bytes: 0 = Empty)
            for (int i = 0; i < 10; i++) writer.WriteInt32(0);

            // 10 QuickSlot Hotbar Buffers (10 x 8 bytes = 80 bytes: dim1, dim2, id)
            for (int i = 0; i < 80; i++) writer.WriteByte(0);

            // Stats
            writer.WriteInt32(100);                         // Pow
            writer.WriteInt32(100);                         // Speed
            writer.WriteInt32(100);                         // Skill
            writer.WriteInt32(100);                         // Luck
            writer.WriteInt32(player.MaxHp);                // MaxHP
            writer.WriteInt32(100);                         // GaugeMax
            writer.WriteInt32(100);                         // GaugeCurrent
            writer.WriteByte(0);                            // ChargePoint
            writer.WriteInt32(100);                         // Atk
            writer.WriteInt32(100);                         // Def
            writer.WriteInt32(100);                         // Hit
            writer.WriteInt32(100);                         // Evasion
            writer.WriteInt32(100);                         // Critical
            writer.WriteInt32(100);                         // AtkSpeed
            writer.WriteInt32(100);                         // MovSpeed
            writer.WriteInt32(100);                         // CoolTime
            writer.WriteInt32((int)player.Exp);             // Exp
            writer.WriteInt32((int)player.MaxExp);          // MaxExp

            // 5 x 0 Int32
            for (int i = 0; i < 5; i++) writer.WriteInt32(0);
            writer.WriteInt32(unchecked((int)0xFFFFFFFE));
            writer.WriteInt32(0x1FFFFFF);
            // 6 x 0 Int32
            for (int i = 0; i < 6; i++) writer.WriteInt32(0);
            writer.WriteInt32(unchecked((int)0xFFFFFFFE));
            writer.WriteInt32(0x1FFFFFF);
            // 6 x 0 Int32
            for (int i = 0; i < 6; i++) writer.WriteInt32(0);

            writer.WriteInt32(0);                           // TitleID
            writer.WriteInt32(0);                           // Unused
            writer.WriteSingle(180.0f);                     // Float 0x43340000
            writer.WriteSingle(1.0f);                       // Float 0x3F800000
            writer.WriteInt32(0x68000000);                  // Int32 0x68000000
            writer.WriteUnicodeString(string.Empty, 13);    // Nick (26 bytes)

            // 9 Equip slots (WriteBeItemID = WriteInt64(Item.FID) = 8 bytes each)
            // Slots: 1=Head, 2=Acc1, 3=Bag, 4=Weapon, 5=Glove, 6=Acc2, 7=Top, 8=Bottom, 9=Shoes
            for (int slot = 1; slot <= 9; slot++)
            {
                int uid = (slot < player.EquippedSlotUids.Length) ? player.EquippedSlotUids[slot] : 0;
                bool isStar = (slot < player.EquippedSlotIsStar.Length) && player.EquippedSlotIsStar[slot];

                if (isStar && uid > 0)
                {
                    var starItem = player.StarBeItems?.Find(i => i.Id == uid || i.SerialId == uid || (i.TypeId > 0 && i.TypeId == uid));
                    long rawUid = starItem != null && starItem.SerialId != 0 ? starItem.SerialId : ((long)uid << 32 | (uint)uid);
                    writer.WriteInt64(rawUid);
                }
                else
                {
                    writer.WriteInt64((long)uid);
                }
            }

            // 9 Equip IsStarItem LongBools (9 x 4 bytes = 36 bytes: 0 = Regular Item, 1 = Star Cash Item)
            for (int slot = 1; slot <= 9; slot++)
            {
                int uid = (slot < player.EquippedSlotUids.Length) ? player.EquippedSlotUids[slot] : 0;
                bool isStar = (slot < player.EquippedSlotIsStar.Length) && player.EquippedSlotIsStar[slot] && uid > 0;
                writer.WriteInt32(isStar ? 1 : 0);
            }

            // 4 Weapon Dex Blocks (Blade = 1, Glove = 2, Blunt = 3, Spirit = 4)
            // Delphi 0x005A8A85: DexLevel[1..4], DexExp[1..4], DexMaxExp[1..4]
            for (int w = 1; w <= 4; w++)
            {
                int lvl = (player.DexLevels != null && player.DexLevels.Length > w && player.DexLevels[w] > 0) ? player.DexLevels[w] : Math.Max(1, player.DexLevel);
                int exp = (player.DexExps != null && player.DexExps.Length > w) ? player.DexExps[w] : player.DexExp;
                int maxExp = 20;
                writer.WriteInt32(lvl);
                writer.WriteInt32(exp);
                writer.WriteInt32(maxExp);
            }
            writer.WriteInt32(0);                       // SkillPoint (Int32)
            writer.WriteUInt16(0);                      // SkillList count = 0 (Word)

            // 4 x 9 Socket/Gem Attributes (36 x Int32 = 144 bytes zeroes - Delphi 0x005A8B17..0x005A8B3C)
            for (int i = 0; i < 36; i++) writer.WriteInt32(0);

            // === BeItemList (Equippable Items from player.Inventory) ===
            var beItems = player.Inventory?.Where(i => i.SlotType != ItemSlotType.Consumable).ToList();
            if (beItems == null || beItems.Count == 0)
            {
                var profile = StarterConfigLoader.GetProfile(player.School, player.Gender);
                var starterEquips = profile.Equipped;
                var starterSpare = profile.Inventory.Where(i => i.ItemId < 200000).ToList();
                writer.WriteUInt16((ushort)(starterEquips.Count + starterSpare.Count));
                int uid = 1;
                foreach (var eq in starterEquips)
                {
                    writer.WriteInt64((long)uid++);
                    writer.WriteInt32(eq.ItemId);
                    for (int j = 0; j < 5; j++) writer.WriteInt32(0);
                }
                foreach (var inv in starterSpare)
                {
                    writer.WriteInt64((long)uid++);
                    writer.WriteInt32(inv.ItemId);
                    for (int j = 0; j < 5; j++) writer.WriteInt32(0);
                }
            }
            else
            {
                writer.WriteUInt16((ushort)beItems.Count);
                foreach (var item in beItems)
                {
                    int typeId = item.TypeId > 0 ? item.TypeId : item.ItemId;
                    writer.WriteInt64((long)item.Id);                 // 8 bytes: UID
                    writer.WriteInt32(typeId);                        // 4 bytes: typeId
                    for (int j = 0; j < 5; j++) writer.WriteInt32(0); // 20 bytes: Reinforce slots
                }
            }

            // === CoItemList (Consumables) ===
            var coItems = player.Inventory?.Where(i => i.SlotType == ItemSlotType.Consumable).ToList();
            if (coItems == null || coItems.Count == 0)
            {
                var profile = StarterConfigLoader.GetProfile(player.School, player.Gender);
                var starterConsumables = profile.Inventory.Where(i => i.ItemId >= 200000 && i.ItemId < 300000).ToList();
                if (starterConsumables.Count > 0)
                {
                    writer.WriteUInt16((ushort)starterConsumables.Count);
                    foreach (var c in starterConsumables)
                    {
                        writer.WriteInt32(c.ItemId);
                        writer.WriteInt32(c.Quantity > 0 ? c.Quantity : 20);
                    }
                }
                else
                {
                    writer.WriteUInt16(1);
                    writer.WriteInt32(200001);
                    writer.WriteInt32(20);
                }
            }
            else
            {
                writer.WriteUInt16((ushort)coItems.Count);
                foreach (var item in coItems)
                {
                    int typeId = item.TypeId > 0 ? item.TypeId : item.ItemId;
                    writer.WriteInt32(typeId);
                    writer.WriteInt32(item.Quantity > 0 ? item.Quantity : 1);
                }
            }

            // Trailing lists - 100% byte-for-byte exact match with Quartet ground truth (1243 bytes total)
            writer.WriteUInt16(0);                      // QuestItemList count = 0 (2 bytes)
            writer.WriteUInt16(0);                      // EnItemList count = 0 (2 bytes)
            writer.WriteUInt16(0);                      // UnkList count = 0 (2 bytes)

            // === StarBeItemList (Cash Items) ===
            // === StarBeItemList (Cash Items) ===
            int starCount = player.StarBeItems?.Count ?? 0;
            writer.WriteUInt16((ushort)starCount);

            if (player.StarBeItems != null)
            {
                for (int i = 0; i < player.StarBeItems.Count; i++)
                {
                    var starItem = player.StarBeItems[i];
                    long rawUid = starItem.SerialId != 0 ? starItem.SerialId : ((long)(i + 1) << 32 | (uint)(i + 1));
                    
                    bool isEquipped = false;
                    for (int s = 1; s <= 9; s++)
                    {
                        if (s < player.EquippedSlotUids.Length && s < player.EquippedSlotIsStar.Length
                            && player.EquippedSlotIsStar[s]
                            && (player.EquippedSlotUids[s] == starItem.Id || (starItem.SerialId != 0 && player.EquippedSlotUids[s] == (int)(starItem.SerialId & 0xFFFFFFFF))))
                        {
                            isEquipped = true;
                            break;
                        }
                    }

                    writer.WriteInt64(rawUid);
                    int starModelId = starItem.TypeId > 0 ? starItem.TypeId : starItem.ItemId;
                    if (starModelId > 1000000 && starModelId % 10 == 4) // E.g. 1400044 -> 140004, 1200174 -> 120017
                    {
                        starModelId /= 10;
                    }
                    writer.WriteInt32(starModelId);
                    writer.WriteInt32(1);
                    writer.WriteInt32(isEquipped ? 1 : 0);
                    writer.WriteInt32(0x0026C4A3);
                    for (int j = 0; j < 5; j++) writer.WriteInt32(0);
                }
            }

            // StarItemEffectList (Active Buffs + Baseline Perks)
            var effectList = new List<(int effectType, int remainSec)>
            {
                (0x1068, 0x1E8D0F),
                (0x1072, 0x1E8D0F),
                (0x1839, 0x1E8D0F),
                (0x183A, 0x1E8D0F),
                (0x183B, 0x1E8D0F),
                (0x183C, 0x1E8D0F),
                (0x183D, 0x1E8D0F)
            };

            if (player.ActiveBuffs != null)
            {
                foreach (var buff in player.ActiveBuffs)
                {
                    if (buff.RemainingSeconds > 0)
                    {
                        effectList.Add((buff.EffectType, buff.RemainingSeconds));
                    }
                }
            }

            writer.WriteUInt16((ushort)effectList.Count);
            foreach (var eff in effectList)
            {
                writer.WriteInt32(eff.effectType);
                writer.WriteInt32(eff.remainSec);
            }

            // 9 x 0 Int32s (36 bytes)
            for (int i = 0; i < 9; i++) writer.WriteInt32(0);

            // WriteCharByulItemEffectInfo (88 bytes: 12 + 10 + 64 + 2)
            writer.WriteInt32(1);
            writer.WriteInt32(1);
            writer.WriteInt32(0);
            writer.WriteUnicodeString("67567", 5);      // 10 bytes ("67567")
            for (int i = 0; i < 16; i++) writer.WriteInt32(0); // 64 bytes zeroes
            writer.WriteByte(0xCC); writer.WriteByte(0xCC); // 2 bytes padding

            writer.WriteInt32(5);                       // limitInvalidCouponInput
            writer.WriteInt32(0);                       // remainCouponInputSecond
            writer.WriteInt32(0);                       // DirX
            writer.WriteInt32(1);                       // DirY

            return writer.Build();
        }

        /// <summary>
        /// 0x7945 (31045): TMsgGameEquipBeItemAns - Equip Item Response
        /// Delphi 0x005ACAA4 exact 46-byte ground truth from Quartet:
        ///   WriteID(0x7945)
        ///   WriteRC(1)
        ///   WriteInt32(CharaID)
        ///   WriteBeItemID(Item) [Int64 = UniqueID]
        ///   WriteInt32(ItemType)
        ///   WriteReinforceSlot [5 x Int32 = 20 bytes zeroes]
        /// </summary>
        public static byte[] MakeGameEquipAns(int charaId, long uniqueId, int typeId, PacketOpcode opcode = PacketOpcode.MsgGameEquipAns)
        {
            using var writer = PacketWriter.Create(opcode);
            int rc = (opcode == PacketOpcode.MsgGameEquipByulBeItemAns) ? 0 : 1; // 0x5266 uses EC=0 for success, 0x7945 uses RC=1
            writer.WriteInt32(rc);                          // RC / EC
            writer.WriteInt32(charaId);                     // Chara ID
            writer.WriteInt64(uniqueId);                    // Item Unique ID (Int64: 8 bytes matching Quartet 0x7945 / 0x5266)
            writer.WriteInt32(typeId);                      // Item Type ID (from BeItemType.txt / ByulItemType.txt)
            for (int i = 0; i < 5; i++) writer.WriteInt32(0); // 5 reinforce slots
            return writer.Build();
        }

        /// <summary>
        /// 0x5269 (21097): TMsgGameUseByulBeItemStartNtf - Star item active duration notification
        /// Delphi 0x005AA2C9 (_Unit47.pas:48548):
        ///   WriteID(0x5269)
        ///   WriteInt64(FID)
        ///   WriteInt32(DurationMinutes)
        /// </summary>
        public static byte[] MakeGameUseByulBeItemStartNtf(long fid, int durationMinutes = 0)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameUseByulBeItemStartNtf);
            writer.WriteInt64(fid);
            writer.WriteInt32(durationMinutes);
            return writer.Build();
        }

        /// <summary>
        /// 0x7947 (31047): TMsgGameStripBeItemAns - Unequip Item Response
        /// Delphi 0x005ACB28 exact 26-byte ground truth from Quartet:
        ///   WriteID(0x7947)
        ///   WriteRC(1)
        ///   WriteInt32(CharaID)
        ///   WriteBeItemID(Item) [Int64 = UniqueID]
        ///   WriteInt32(ItemType)
        /// </summary>
        public static byte[] MakeGameUnequipAns(int charaId, long uniqueId, int typeId, PacketOpcode opcode = PacketOpcode.MsgGameUnequipAns)
        {
            using var writer = PacketWriter.Create(opcode);
            int rc = (opcode == PacketOpcode.MsgGameStripByulBeItemAns) ? 0 : 1; // 0x5268 uses EC=0 for success, 0x7947 uses RC=1
            writer.WriteInt32(rc);                          // RC / EC
            writer.WriteInt32(charaId);                     // Chara ID
            writer.WriteInt64(uniqueId);                    // Item Unique ID (Int64: 8 bytes matching Quartet 0x7947 / 0x5268)
            writer.WriteInt32(typeId);                      // Item Type ID (from BeItemType.txt / ByulItemType.txt)
            return writer.Build();
        }

        /// <summary>
        /// 0x520D (21005): TMsgGameStatDeltaNtf - Stat delta update notification
        /// Exact 8-byte ground truth from Quartet:
        ///   WriteID(0x520D)
        ///   WriteWord(StatDeltaIndex)
        /// </summary>
        public static byte[] MakeGameStatDeltaNtf(ushort delta = 0x1C)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameStatDeltaNtf);
            writer.WriteUInt16(delta);
            return writer.Build();
        }

        /// <summary>
        /// 0x791C (31004): TMsgGameChargePointUpdateNtf - Charge point update notification
        /// Exact 18-byte ground truth from Quartet:
        ///   WriteID(0x791C)
        ///   WriteByte(ChargePoint)
        ///   WritePadding(3 bytes 0xCC)
        ///   WriteInt32(NextUpdate)
        ///   WriteInt32(Current)
        /// </summary>
        public static byte[] MakeGameChargePointUpdateNtf(byte chargePoint = 0, int nextUpdate = 0x011170, int current = 0)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameChargePointUpdateNtf);
            writer.WriteByte(chargePoint);
            writer.WriteByte(0xCC);
            writer.WriteByte(0xCC);
            writer.WriteByte(0xCC);
            writer.WriteInt32(nextUpdate);
            writer.WriteInt32(current);
            return writer.Build();
        }

        /// <summary>
        /// 0x79D5 (31189): TMsgGameMoveExNtf - Extended Movement Delta broadcast
        /// Delphi 0x005AF7F0:
        ///   WriteID(0x79D5)
        ///   WriteInt32(CharaID)
        ///   WriteMapPoint(px, py)
        ///   WriteChar(dx)
        ///   WriteChar(dy)
        ///   WriteWord(padding)
        /// </summary>
        public static byte[] MakeGameMoveExNtf(int charaId, ushort px, ushort py, sbyte dx, sbyte dy)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameMoveExNtf);
            writer.WriteInt32(charaId);
            writer.WriteUInt16(px);
            writer.WriteUInt16(py);
            writer.WriteByte((byte)dx);
            writer.WriteByte((byte)dy);
            writer.WriteUInt16(0x770A); // padding
            return writer.Build();
        }

        /// <summary>
        /// 0x520C (21004): TMsgGameFadeOutNtf - Screen fade out for transition
        /// Exact 6-byte ground truth from Quartet
        /// </summary>
        public static byte[] MakeGameFadeOutNtf()
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameFadeOutNtf);
            return writer.Build();
        }

        /// <summary>
        /// 0x7966 (31078): TMsgGameWarpStartNtf - Warp transition start
        /// Exact 26-byte ground truth from Quartet & 31078.dms
        /// </summary>
        public static byte[] MakeGameWarpStartNtf(int targetFieldId, float targetX, float targetY, bool isHuntField = false, int huntFieldId = 0)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameWarpStartNtf);
            writer.WriteInt32(1);                           // ReturnCode = 1 (Success)
            writer.WriteInt32(targetFieldId);               // Target Field ID
            writer.WriteUInt16((ushort)targetX);            // Target X coordinate
            writer.WriteUInt16((ushort)targetY);            // Target Y coordinate
            writer.WriteInt32(isHuntField ? 1 : 0);         // bHuntField (1 if Combat Field, 0 if School)
            if (isHuntField)
            {
                writer.WriteInt32(huntFieldId);             // idHuntField
            }
            else
            {
                writer.WriteByte(0xCC);                     // 4-byte padding
                writer.WriteByte(0xCC);
                writer.WriteByte(0xCC);
                writer.WriteByte(0xCC);
            }
            return writer.Build();
        }

        /// <summary>
        /// 0x523B (21051): MSG_GAME_BYUL_CHARGE_ANS - Returns current Star Point cash coin balance
        /// Delphi 0x005A9A84: WriteEC(0), WriteInt32(ShopPoints)
        /// </summary>
        public static byte[] MakeGameByulChargeAns(int returnCode = 0, int shopPoints = 10000)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameByulChargeAns);
            writer.WriteInt32(returnCode);
            writer.WriteInt32(shopPoints);
            return writer.Build();
        }

        /// <summary>
        /// 0x798D (31117): MsgGameSitDownAns - Sitting on chair/bench or floor response
        /// </summary>
        public static byte[] MakeGameSitDownAns(int returnCode, int charaId, ushort x, ushort y, int bChair)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameSitDownAns);
            writer.WriteInt32(returnCode);
            writer.WriteInt32(charaId);
            writer.WriteUInt16(x);
            writer.WriteUInt16(y);
            writer.WriteInt32(bChair);
            return writer.Build();
        }

        /// <summary>
        /// 0x798E (31118): MsgGameStandUpNtf - Character standing up from bench or floor
        /// </summary>
        public static byte[] MakeGameStandUpNtf(int charaId)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameStandUpNtf);
            writer.WriteInt32(charaId);
            return writer.Build();
        }

        /// <summary>
        /// 0x79B2 (31154): TMsgGamePromoteInfoNtf - Complete promotion & exam ranking table
        /// Delphi 0x005AEF1C:
        ///   WriteInt32(CharaID)
        ///   WriteWord(9) -> 9 x WriteWord(0)
        ///   WriteWord(2) -> 9 x WriteWord(0)
        ///   12 Grade thresholds (95, 90, 85, 80, 75, 70, 66, 62, 58, 54, 50, 45)
        ///   WriteInt32(0), WriteInt32(80)
        ///   WriteWord(6) (6 exams: 0x21, 0x2C, 0x22, 0x1F, 0x47, 0x38)
        ///   WriteInt32(0), WriteInt32(0)
        /// </summary>
        public static byte[] MakeGamePromoteInfoNtf(int charaId = 1, int grade = 1, int rank = 1)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGamePromoteInfoNtf);
            writer.WriteInt32(charaId);
            writer.WriteUInt16(9);
            for (int i = 0; i < 9; i++) writer.WriteUInt16(0);
            writer.WriteUInt16(2);
            for (int i = 0; i < 9; i++) writer.WriteUInt16(0);

            // Grade thresholds (12 Int32s)
            writer.WriteInt32(95); // 0x5F
            writer.WriteInt32(90); // 0x5A
            writer.WriteInt32(85); // 0x55
            writer.WriteInt32(80); // 0x50
            writer.WriteInt32(75); // 0x4B
            writer.WriteInt32(70); // 0x46
            writer.WriteInt32(66); // 0x42
            writer.WriteInt32(62); // 0x3E
            writer.WriteInt32(58); // 0x3A
            writer.WriteInt32(54); // 0x36
            writer.WriteInt32(50); // 0x32
            writer.WriteInt32(45); // 0x2D

            writer.WriteInt32(0);
            writer.WriteInt32(80); // 0x50

            // 6 Promotion Exams
            writer.WriteUInt16(6);
            int[] examIds = { 0x21, 0x2C, 0x22, 0x1F, 0x47, 0x38 };
            foreach (var exam in examIds)
            {
                writer.WriteInt32(exam);
                writer.WriteInt32(10000);
                writer.WriteInt32(-1);
            }

            writer.WriteInt32(0);
            writer.WriteInt32(0);

            return writer.Build();
        }

        /// <summary>
        /// 0x79A3 (31139): TMsgGameEquipTitleAns
        /// Delphi 0x005AED10
        /// </summary>
        public static byte[] MakeGameEquipTitleAns(int charaId = 1, int titleId = 0)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameEquipTitleAns);
            writer.WriteInt32(1);                           // RC = 1
            writer.WriteInt32(charaId);                     // Chara ID
            writer.WriteInt32(titleId);                     // Title ID
            writer.WriteInt32(0);                           // LongBool (False)
            return writer.Build();
        }

        /// <summary>
        /// 0x795A (31066): TMsgGameFieldLoadingStartNtf
        /// Delphi 0x005AD2FC: WriteInt32(1), WriteInt32(FieldID), WriteMapPoint(Point), WriteLongBool(False), FillBuffer(0xCC, 4)
        /// </summary>
        public static byte[] MakeGameFieldLoadingStartNtf(int fieldId = 1, float x = 38f, float y = 14f)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameFieldLoadingStartNtf);
            writer.WriteInt32(1);                           // State: 1 (School)
            writer.WriteInt32(fieldId);                     // Field ID
            writer.WriteUInt16((ushort)x);                  // MapPoint X (word: raw integer coordinate)
            writer.WriteUInt16((ushort)y);                  // MapPoint Y (word: raw integer coordinate)
            writer.WriteInt32(0);                           // LongBool (False: not instance dungeon)
            writer.WriteByte(0xCC);                         // 4-byte padding
            writer.WriteByte(0xCC);
            writer.WriteByte(0xCC);
            writer.WriteByte(0xCC);
            return writer.Build();
        }

        /// <summary>
        /// 0x7968 (31080): TMsgGameWarpResultNtf - Warp Teleport Result
        /// Exact 14-byte ground truth from Quartet
        /// </summary>
        public static byte[] MakeGameWarpResultNtf(int targetFieldId, float targetX, float targetY)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameWarpResultNtf);
            writer.WriteInt32(targetFieldId);
            writer.WriteUInt16((ushort)targetX);
            writer.WriteUInt16((ushort)targetY);
            return writer.Build();
        }

        /// <summary>
        /// 0x520B (21003): MsgGameFieldEnterStatReadyNtf (TMsgGameStartRegainNtf) - Activates Client Live HP/MP Natural Regain Rate
        /// Exact 10-byte ground truth from Quartet: WriteID(0x520B), WriteSingle(1.6f)
        /// </summary>
        public static byte[] MakeGameFieldEnterStatReadyNtf(float rate = 1.6f)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameFieldEnterStatReadyNtf);
            writer.WriteSingle(rate);
            return writer.Build();
        }

        /// <summary>
        /// 0x79D4 (31188): MsgGameFieldViewRangeNtf - Sets Client 3D Field View Distance
        /// Exact 10-byte ground truth from Quartet: WriteID(0x79D4), WriteInt32(400)
        /// </summary>
        public static byte[] MakeGameFieldViewRangeNtf(int viewRange = 400)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameZoneNameNtf);
            writer.WriteInt32(viewRange);
            return writer.Build();
        }

        /// <summary>
        /// 0x7963 (31075): TMsgGameCharaNameInfoNtf (Phase 1) - Initial Overhead Name & Guild Tag
        /// Exact 86-byte ground truth from Quartet
        /// </summary>
        public static byte[] MakeGameCharaNameInfoNtfPhase1(Player player, int entityId = 1)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameCharaNameInfoNtf);
            writer.WriteInt32(-1);                          // Unused (-1)
            writer.WriteInt32(entityId);                    // EntityID in 3D Field (1 for Local Player)
            for (int i = 0; i < 9; i++) writer.WriteInt32(0); // 36 bytes zeroes
            writer.WriteUInt16(0x0022);                     // String length (34 bytes)
            string name = player.CharacterName ?? "Hanazono";
            string schoolTag = player.School == SchoolType.SoilAcademy ? "So-il" : "Estiva";
            string combined = $"{name} {schoolTag}";
            writer.WriteUnicodeString(combined, 17);        // 34 bytes UTF-16LE
            return writer.Build();
        }

        /// <summary>
        /// 0x7963 (31075): TMsgGameCharaNameInfoNtf (Phase 2) - Field Zone Title & Overhead Tag
        /// Exact 78-byte ground truth from Quartet
        /// </summary>
        public static byte[] MakeGameCharaNameInfoNtfPhase2(Player player, int entityId = 1, string zoneName = "裏庭 聖堂周辺")
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameCharaNameInfoNtf);
            writer.WriteInt32(-1);                          // Offset 0..3: Unused (-1)
            writer.WriteInt32(entityId);                    // Offset 4..7: EntityID in 3D Field (1 for Local Player)
            for (int i = 0; i < 7; i++) writer.WriteInt32(0); // Offset 8..35: 28 bytes zeroes
            writer.WriteUInt16(0);                          // Offset 36..37: 2 bytes padding
            writer.WriteInt32(0x00000FA1);                  // Offset 38..41: Name Tag Attribute
            writer.WriteUInt16(1);                          // Offset 42..43: Style flag
            writer.WriteUInt16(0x0016);                     // Offset 44..45: Zone string byte length (22 bytes)
            writer.WriteUnicodeString(zoneName, 11);        // Offset 46..67: 22 bytes UTF-16LE
            writer.WriteInt32(2);                           // Offset 68..71: Suffix flag (4 bytes)
            return writer.Build();
        }

        /// <summary>
        /// Backward-compatible alias for Phase 2 overhead name tag
        /// </summary>
        public static byte[] MakeGameCharaNameInfoNtf(Player player, int entityId = 1, string zoneName = "裏庭 聖堂周辺")
        {
            return MakeGameCharaNameInfoNtfPhase2(player, entityId, zoneName);
        }

        /// <summary>
        /// 0x520F (21007): TMsgGameSetStateNtf - Complete 54-byte Character Stats Table
        /// Exact 54-byte ground truth from Quartet:
        ///   Byte(0x01), Byte(0xCC)
        ///   Word(Level=1), Word(Pow=28), Word(Spd=4), Word(Skl=3), Word(Luk=3), Word(State=2)
        ///   Byte(0xCC), Byte(0xCC)
        ///   Int32(MaxHP=260), Int32(CurHP=260)
        ///   Int32(MaxSP=195), Int32(CurSP=195)
        ///   Int32(Atk=195), Int32(Def=195), Int32(Hit=195), Int32(Crit=130)
        /// </summary>
        public static byte[] MakeGameSetStateNtf(Player player)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameSetStateNtf);
            writer.WriteByte(1);                            // Flag (IsHunt/Active = 1)
            writer.WriteByte(0xCC);                         // Padding
            writer.WriteUInt16((ushort)player.Level);       // Level = 1
            writer.WriteUInt16(28);                         // Power = 28
            writer.WriteUInt16(4);                          // Speed = 4
            writer.WriteUInt16(3);                          // Skill = 3
            writer.WriteUInt16(3);                          // Luck = 3
            writer.WriteUInt16(2);                          // State = 2
            writer.WriteByte(0xCC);                         // Padding
            writer.WriteByte(0xCC);                         // Padding
            writer.WriteInt32(player.MaxHp > 0 ? player.MaxHp : 260); // MaxHP
            writer.WriteInt32(player.Hp > 0 ? player.Hp : 260);       // CurrentHP
            writer.WriteInt32(player.MaxSp > 0 ? player.MaxSp : 195); // MaxSP
            writer.WriteInt32(player.Sp > 0 ? player.Sp : 195);       // CurrentSP
            writer.WriteInt32(195);                         // Attack
            writer.WriteInt32(195);                         // Defense
            writer.WriteInt32(195);                         // Hit
            writer.WriteInt32(130);                         // Critical
            return writer.Build();
        }

        /// <summary>
        /// 0x5264 (21092): TMsgGameSchoolInfoNtf
        public static byte[] MakeGameSchoolInfoNtf(int schoolId = 1)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameSchoolInfoNtf);
            writer.WriteInt32(schoolId);
            return writer.Build();
        }

        /// <summary>
        /// 0x521B (21019): TMsgObjectCreateNtf - Spawns NPC, Warp Gate, or Field Object in 3D Field
        /// Delphi 0x005A95BC:
        ///   WriteInt32(Obj.ID)
        ///   WriteInt32(Obj.ObjectType)
        ///   WriteInt32(Obj.SubID)
        ///   WriteInt32(Obj.ClientID)
        ///   WriteInt32(Obj.ShellID)
        ///   WriteSingle(Obj.X)
        ///   WriteSingle(Obj.Y)
        ///   WriteByte(Obj.Dir)
        ///   WriteByte(Obj.Visible)
        ///   WriteByte(Obj.Usable)
        ///   WriteByte(0xCC)
        /// </summary>
        public static byte[] MakeObjectCreateNtf(
            int objectId, int objectType, int subId = 0, int clientId = 0, int shellId = 0,
            float x = 0f, float y = 0f, byte dir = 0, byte visible = 1, byte usable = 1)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgObjectCreateNtf);
            writer.WriteInt32(objectId);
            writer.WriteInt32(objectType);
            writer.WriteInt32(subId);
            writer.WriteInt32(clientId);
            writer.WriteInt32(shellId);
            writer.WriteSingle(x);
            writer.WriteSingle(y);
            writer.WriteByte(dir);
            writer.WriteByte(visible);
            writer.WriteByte(usable);
            writer.WriteByte(0xCC); // 1-byte padding
            return writer.Build();
        }

        /// <summary>
        /// 0x521C (21020): TMsgObjectDestroyNtf - Despawns entity from 3D Field
        /// </summary>
        public static byte[] MakeObjectDestroyNtf(int objectId)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgObjectDestroyNtf);
            writer.WriteInt32(objectId);
            return writer.Build();
        }

        /// <summary>
        /// 0x5227 (21031): TMsgObjectStateNtf - Updates interactive object state (e.g. Warp Gate Active = 1)
        /// </summary>
        public static byte[] MakeGameObjectStateNtf(int objectId, byte state = 1)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgObjectStateNtf);
            writer.WriteInt32(objectId);
            writer.WriteByte(state);
            writer.WriteByte(0xCC);
            writer.WriteByte(0xCC);
            writer.WriteByte(0xCC);
            return writer.Build();
        }

        /// <summary>
        /// 0x7942 (31042): TMsgGameNpcCreateNtf - Spawns static visual props, streetlights, banners, and decor
        /// Delphi 0x005AC9DC (_Unit47.pas:52508-52545):
        ///   WriteID(0x7942)
        ///   WriteInt32(NpcID)
        ///   WriteInt32(ShellType)
        ///   WriteWord(X)
        ///   WriteWord(Y)
        ///   WriteInt32(Dir)
        ///   FillBuffer(3, 0xCC)
        /// </summary>
        public static byte[] MakeGameNpcCreateNtf(int npcId, int shellType, ushort x, ushort y, int dir)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameNpcCreateNtf);
            writer.WriteInt32(npcId);
            writer.WriteInt32(shellType);
            writer.WriteUInt16(x);
            writer.WriteUInt16(y);
            writer.WriteInt32(dir);
            writer.WriteByte(0xCC);
            writer.WriteByte(0xCC);
            writer.WriteByte(0xCC);
            return writer.Build();
        }

        /// <summary>
        /// 0x795C (31068): TMsgGameTriggerActionNtf - Spawns WarpGate interactive 3D prop/portal
        /// Delphi 005AD8C8 (TMsgGameTriggerActionNtf.Create(Gate: TWarpGate)):
        ///   WriteID(0x795C)
        ///   WriteInt32(0x0A)          // Action = 10 (TWarpGate)
        ///   WriteInt32(gate.ID)
        ///   WriteInt32(gate.X)
        ///   WriteInt32(gate.Y)
        ///   WriteInt32(gate.Shell)    // 3D Model Shell (e.g. 115 for indoor doorway, 96 for outdoor prop)
        ///   WriteInt32(gate.ClientID) // idCli from default.xml
        ///   WriteInt32(gate.Dir)
        ///   WriteInt32(gate.DestFieldID)
        ///   FillBuffer(44, 0xCC)
        /// </summary>
        public static byte[] MakeGameWarpGateSpawnNtf(int gateId, int posX, int posY, int shell, int cliId, int dir, int destFieldId)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameFieldEntitySpawnNtf);
            writer.WriteInt32(0x0A);
            writer.WriteInt32(gateId);
            writer.WriteInt32(posX);
            writer.WriteInt32(posY);
            writer.WriteInt32(shell);
            writer.WriteInt32(cliId);
            writer.WriteInt32(dir);
            writer.WriteInt32(destFieldId);
            for (int i = 0; i < 44; i++) writer.WriteByte(0xCC);
            return writer.Build();
        }

        public static byte[] MakeGameNpcSpawnNtf(int npcId, int posX, int posY, int shell, int cliId, int dir, int destFieldId)
            => MakeGameWarpGateSpawnNtf(npcId, posX, posY, shell, cliId, dir, destFieldId);

        /// <summary>
        /// 0x7956 (31062): TMsgGameFieldInfoDoneNtf - Signal field loading is done and reveal 3D scene
        /// Delphi 0x005AD210: WriteInt32(FieldID)
        /// </summary>
        public static byte[] MakeGameFieldInfoDoneNtf(int fieldId = unchecked((int)0xCCCCCCCC))
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameFieldInfoDoneNtf);
            writer.WriteInt32(fieldId);
            return writer.Build();
        }

        /// <summary>
        /// 0x5234 (21044): TMsgGameByulShopBeginAns - Star Item Shop Ready
        /// Delphi 0x005A9A04: WriteEC(0)
        /// </summary>
        public static byte[] MakeGameByulShopBeginAns()
        {
            using var writer = PacketWriter.Create((PacketOpcode)0x5234);
            writer.WriteInt32(0);                           // Error Code = 0
            return writer.Build();
        }

        /// <summary>
        /// 0x5236 (21046): TMsgGameByulShopEndAns - Star Item Shop Closed
        /// Delphi 0x005A9A44: WriteEC(0)
        /// </summary>
        public static byte[] MakeGameByulShopEndAns()
        {
            using var writer = PacketWriter.Create((PacketOpcode)0x5236);
            writer.WriteInt32(0);                           // Error Code = 0
            return writer.Build();
        }

        /// <summary>
        /// 0x523D (21053): TMsgGameByulProductListAns - Star Item Shop Product List
        /// Delphi 0x005A9AD8: WriteEC(0), WriteWord(ProductCount)
        /// </summary>
        public static byte[] MakeGameByulProductListAns()
        {
            using var writer = PacketWriter.Create((PacketOpcode)0x523D);
            writer.WriteInt32(0);                           // Error Code = 0
            writer.WriteUInt16(0);                          // Product Count = 0
            return writer.Build();
        }

        /// <summary>
        /// 0x7963 (31075): TMsgGameChatNtf - Broadcast Chat Message in Campus/Room
        /// Delphi 0x005ADB9C:
        ///   WriteInt32(SpeakerID)
        ///   WriteInt32(Color)
        ///   WriteInt32(EmotIcon)
        ///   WriteWStr(SpeakerName, 26 bytes)
        ///   WriteInt32(FormatNum)
        ///   WriteWord(FormatArgsCount)
        ///   WriteWStrWithLen(Text)
        /// </summary>
        public static byte[] MakeGameChatNtf(int senderId, string senderName, string message, int chatType = 0)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameChatNtf);
            writer.WriteInt32(senderId);
            writer.WriteInt32(chatType);                    // Chat Color / Type
            writer.WriteInt32(0);                           // EmotIcon ID = 0
            writer.WriteUnicodeString(senderName, 13);      // Fixed 26-byte name
            writer.WriteInt32(0);                           // FormatNum = 0
            writer.WriteUInt16(0);                          // FormatArgs count = 0
            writer.WriteUInt16((ushort)message.Length);     // Text length in chars
            writer.WriteUnicodeString(message, message.Length); // Text

            return writer.Build();
        }

        /// <summary>
        /// 0x7968 (31080): TMsgGameWarpResultNtf - Warp to destination field
        /// </summary>
        public static byte[] MakeGameWarpResultNtf(int targetFieldId, float targetX, float targetY, float targetZ)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameWarpResultNtf);
            writer.WriteInt32(targetFieldId);
            writer.WriteFloat(targetX);
            writer.WriteFloat(targetY);
            writer.WriteFloat(targetZ);
            return writer.Build();
        }

        /// <summary>
        /// 0x7759 (30553): TMsgCommEchoNtf - Comm heartbeat
        /// </summary>
        public static byte[] MakeCommEchoNtf()
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgCommEchoNtf);
            writer.WriteInt32(0);
            return writer.Build();
        }
        /// <summary>
        /// 0x7604 (30212): TMsgTransJoinCmsAns - Comm Server Join Acknowledgment with Friend List
        /// Exact 254-byte ground truth from Quartet / Delphi: TMsgTransJoinCmsAns.Create at 0x005AAEB8
        /// </summary>
        public static byte[] MakeTransJoinCmsAns(Player? player = null)
        {
            var now = DateTime.Now;
            using var writer = PacketWriter.Create(PacketOpcode.MsgTransJoinCmsAns);
            writer.WriteInt32(1);                               // Offset 0..3: RC = 1 (Success)
            for (int i = 0; i < 8; i++) writer.WriteByte(0);   // Offset 4..11: Friend/Block counters (8 bytes)
            writer.WriteInt32(20);                              // Offset 12..15: Max friends = 20
            writer.WriteInt32(20);                              // Offset 16..19: Max block = 20
            writer.WriteInt32(20);                              // Offset 20..23: Max guild = 20
            for (int i = 0; i < 24; i++) writer.WriteByte(0);  // Offset 24..47: 24 bytes zeroes
            writer.WriteInt32(-1);                              // Offset 48..51: -1
            writer.WriteByte(0x63);                             // Offset 52: 0x63
            for (int i = 0; i < 3; i++) writer.WriteByte(0xCC);// Offset 53..55: 0xCC padding
            for (int i = 0; i < 8; i++) writer.WriteByte(0);   // Offset 56..63: 8 bytes zeroes
            writer.WriteInt32(-1);                              // Offset 64..67: -1
            writer.WriteInt32(-1);                              // Offset 68..71: -1
            for (int i = 0; i < 84; i++) writer.WriteByte(0);  // Offset 72..155: 84 bytes zeroes
            
            // CharBriefInfo (WriteCharBriefInfo in Delphi 0x005AB18E)
            writer.WriteInt32(player?.CharaId ?? 256);          // Offset 156..159: CharaID (256)
            string name = player?.CharacterName ?? "Hanazono";
            writer.WriteUnicodeString(name, 8);                 // Offset 160..175: 16 bytes name
            for (int i = 0; i < 50; i++) writer.WriteByte(0);  // Offset 176..225: 50 bytes zeroes
            writer.WriteByte(0xCC); writer.WriteByte(0xCC);     // Offset 226..227: 0xCC padding
            writer.WriteInt32(0x000011B6);                      // Offset 228..231: Brief attribute
            
            // SYSTEMTIME (16 bytes)
            writer.WriteUInt16((ushort)now.Year);               // Offset 232..233
            writer.WriteUInt16((ushort)now.Month);              // Offset 234..235
            writer.WriteUInt16((ushort)now.DayOfWeek);          // Offset 236..237
            writer.WriteUInt16((ushort)now.Day);                // Offset 238..239
            writer.WriteUInt16((ushort)now.Hour);               // Offset 240..241
            writer.WriteUInt16((ushort)now.Minute);             // Offset 242..243
            writer.WriteUInt16((ushort)now.Second);             // Offset 244..245
            writer.WriteUInt16((ushort)now.Millisecond);        // Offset 246..247

            return writer.Build();
        }

        /// <summary>
        /// 0x5229 (21033): TMsgGameNpcDialogExResponseNtf - NPC CutIn Portrait, Dialogue & Interactive Choices
        /// </summary>
        public static byte[] MakeGameNpcDialogNtf(int npcId, int dialogId, string dialogText, string[] choices, int cutInCategory = 1)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameNpcDialogNtf);
            writer.WriteInt32(npcId);
            writer.WriteInt32(dialogId);
            writer.WriteInt32(cutInCategory);
            writer.WriteUInt16((ushort)dialogText.Length);
            writer.WriteUnicodeString(dialogText, dialogText.Length);

            writer.WriteUInt16((ushort)choices.Length);
            foreach (var choice in choices)
            {
                writer.WriteUInt16((ushort)choice.Length);
                writer.WriteUnicodeString(choice, choice.Length);
            }

            writer.WriteInt32(0);                               // Timeout seconds (0 = infinite)
            writer.WriteInt32(0);                               // Choice ID on timeout
            writer.WriteInt32(1);                               // Show close button
            writer.WriteInt32(1);                                // Enable bg frame click
            return writer.Build();
        }

        /// <summary>
        /// 0x5225 (21029): TMsgGameShopListNtf - Vending Machine / Store Auntie Catalog
        /// </summary>
        public static byte[] MakeGameShopListNtf(byte grade, (byte category, int itemId)[] products)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameShopListNtf);
            writer.WriteByte(grade);
            writer.WriteUInt16((ushort)products.Length);

            foreach (var (category, itemId) in products)
            {
                writer.WriteByte(category);
                writer.WriteByte(0);                            // 1-byte padding
                writer.WriteUInt16((ushort)(itemId & 0xFFFF));  // Low word
                writer.WriteUInt16((ushort)((itemId >> 16) & 0xFFFF)); // High word
            }

            return writer.Build();
        }

        /// <summary>
        /// 0x5220 (21024): MsgObjectUseAns - Field Object / Warp Gate Click Answer (Unlocks client cursor/movement)
        /// </summary>
        public static byte[] MakeGameObjectUseAns(int rc, int objectId)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgObjectUseAns);
            writer.WriteInt32(rc);
            writer.WriteInt32(objectId);
            return writer.Build();
        }

        /// <summary>
        /// 0x795C (31068): TMsgGameTriggerActionNtf - Plays Background Music (BGM)
        /// Delphi 0x005AD744 (_Unit47.pas:53760):
        ///   WriteID(0x795C)
        ///   WriteInt32(0x27)          // 39: BGM Trigger Action
        ///   WriteInt32(0x34618956)    // Magic Seed
        ///   WriteInt32(bgmNo)         // BGM Track Number from Field.txt / MatchingBGM.txt (e.g. 6 = "Yogurting_School R", 5 = "Yogurting_School 2")
        ///   WriteLongBool(isPlay)     // 1 = Play
        ///   WriteInt32(loopCount)     // 0 = Infinite Loop
        ///   FillBuffer(56, 0xCC)      // 56 bytes CC padding
        /// </summary>
        public static byte[] MakeGameTriggerBgmNtf(int bgmNo, bool isPlay = true)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameFieldEntitySpawnNtf);
            writer.WriteInt32(0x27);
            writer.WriteInt32(0x34618956);
            writer.WriteInt32(bgmNo);
            writer.WriteInt32(isPlay ? 1 : 0);
            writer.WriteInt32(0); // Loop
            for (int i = 0; i < 56; i++) writer.WriteByte(0xCC);
            return writer.Build();
        }

        /// <summary>
        /// 0x795C (31068): TMsgGameTriggerActionNtf(MobID) - Registers and activates a monster entity in client's interactive combat target list.
        /// Delphi 0x005AD548 (_Unit47.pas:53570):
        ///   WriteID(0x795C)
        ///   WriteInt32(3)           // Type 3 = Mob Action Trigger
        ///   WriteInt32(1)           // Subtype 1 = Register / Activate Mob
        ///   WriteInt32(mobEntityId) // Mob Entity ID (matching MsgGameMonInfoNtf)
        ///   FillBuffer(64, 0xCC)    // 64 bytes CC padding (76 bytes payload total)
        /// </summary>
        public static byte[] MakeGameTriggerMobNtf(int mobEntityId)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameFieldEntitySpawnNtf);
            writer.WriteInt32(3);
            writer.WriteInt32(1);
            writer.WriteInt32(mobEntityId);
            for (int i = 0; i < 64; i++) writer.WriteByte(0xCC);
            return writer.Build();
        }

        /// <summary>
        /// 0x5234 (21044): TMsgGameByulShopBeginAns - Opens Star Cash Shop
        /// Delphi 0x005A9A04: WriteEC(0)
        /// </summary>
        public static byte[] MakeByulShopBeginAns(int ec = 0)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameByulShopBeginAns);
            writer.WriteInt32(ec);
            return writer.Build();
        }

        /// <summary>
        /// 0x5236 (21046): TMsgGameByulShopEndAns - Closes Star Cash Shop
        /// Delphi 0x005A9A44: WriteEC(0)
        /// </summary>
        public static byte[] MakeByulShopEndAns(int ec = 0)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameByulShopEndAns);
            writer.WriteInt32(ec);
            return writer.Build();
        }

        /// <summary>
        /// 0x524C (21068): MsgGameUseByulBeItemAns - Star / Cash Item or Buff Activation Response
        /// Delphi 0x005A9E71 (_Unit47.pas:48086):
        ///   WriteID(0x524C)
        ///   WriteEC(0)
        ///   WriteInt32(charaId)
        ///   WriteInt64(serialId)
        ///   WriteInt32(effectType)
        ///   WriteInt32(0) // resultValue
        /// </summary>
        public static byte[] MakeGameUseByulBeItemAns(int charaId, long serialId, int effectType, int ec = 0)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameUseByulBeItemAns);
            writer.WriteInt32(ec);
            writer.WriteInt32(charaId);
            writer.WriteInt64(serialId);
            writer.WriteInt32(effectType);
            writer.WriteInt32(0);
            return writer.Build();
        }

        /// <summary>
        /// 0x523D (21053): TMsgGameByulProductListAns - Star Cash Shop Product Catalog
        /// Exact Delphi 1:1 match (_Unit47.pas:47818-47830):
        ///   WriteEC(0)
        ///   WriteWord(Count)
        ///   For each product (20 bytes):
        ///     WriteInt32(ProductID)
        ///     WriteInt64(ProductPrice)       // 8 bytes Int64: Price!
        ///     WriteInt32(DisplayOption)       // 4 bytes: Hot/New Flag (1=Hot, 2=New, 0=None)
        ///     WriteInt32(PriceType / Period)  // 4 bytes: Period (0=Permanent, 1=30d, 2=7d)
        /// </summary>
        public static byte[] MakeByulProductListAns(int ec, List<ShopProductDef> products)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameByulProductListAns);
            writer.WriteInt32(ec);
            writer.WriteUInt16((ushort)(products?.Count ?? 0));
            if (products != null)
            {
                foreach (var p in products)
                {
                    writer.WriteInt32(p.ProductId);
                    writer.WriteInt64((long)p.Price);           // Offset +4: Price (Int64, 8 bytes)!
                    writer.WriteInt32(p.DisplayOption);        // Offset +12: DisplayOption (0=Normal, 1=Hot, 2=New)
                    writer.WriteInt32(p.PriceType);            // Offset +16: PriceType (0=TAFF)
                }
            }
            return writer.Build();
        }

        /// <summary>
        /// 0x523F (21055): TMsgGameByulProductBuyAns - Star Cash Shop Purchase Response
        /// Delphi 0x005A9C34:
        ///   WriteEC(ec)
        ///   WriteInt32(productId)
        ///   WriteInt64(serialId)
        ///   WriteInt32(remainingStars)
        ///   WriteInt32(period)
        ///   WriteWord(itemCount = 1)
        ///   WriteInt32(itemId)
        ///   WriteInt64(itemFid)
        ///   WriteInt32(unk1 = 1)
        ///   WriteLongBool(isEquipped = 0)
        ///   WriteInt32(unk2 = 0x26C4A3)
        ///   WriteReinforceSlot(5 * Int32 = 20B)
        ///   WriteInt32(timestamp)
        ///   WriteWStr(12) (OrderId)
        ///   WriteInt32(price)
        ///   WriteWStr(202) (Message)
        /// </summary>
        /// <summary>
        /// 0x523F (21055): TMsgGameByulProductBuyAns - Star Cash Shop Purchase Response
        /// Exact 1:1 Delphi layout (_Unit47.pas:47915):
        ///   WriteEC(ec)
        ///   WriteInt32(productId)
        ///   WriteInt64(serialId)
        ///   WriteInt32(remainingStars)
        ///   WriteInt32(period)
        ///   WriteWord(itemCount)
        ///   For each item (44B):
        ///     WriteInt32(itemId)
        ///     WriteInt64(itemFid)
        ///     WriteInt32(unk1 = 1)
        ///     WriteLongBool(isEquipped = 0)
        ///     WriteInt32(unk2 = 0x00015180)
        ///     WriteReinforceSlot(5 * Int32 = 20B)
        ///   WriteInt32(timestamp)
        ///   WriteWStr(12 bytes = 6 chars) (OrderId)
        ///   WriteInt32(price)
        ///   WriteWStr(202 bytes = 101 chars) (Message)
        /// </summary>
        public static byte[] MakeByulProductBuyAns(int ec, int productId, long serialId, int remainingStars, int period, int price, List<Item>? deliveredItems = null)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameByulProductBuyAns);
            writer.WriteInt32(ec);

            int nowSec = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (ec != 0)
            {
                // Exact Delphi failure structure (_Unit47.pas:47990-48020)
                writer.WriteInt32(0);                                       // ProductID = 0
                writer.WriteInt64(0L);                                      // ProductPrice = 0
                writer.WriteInt32(remainingStars);                          // Current Star Coins
                writer.WriteInt32(0);                                       // PriceType = 0
                writer.WriteUInt16(0);                                      // ItemCount = 0
                writer.WriteInt32(nowSec);                                  // Timestamp
                writer.WriteUnicodeString($"OD{productId % 10000:D4}", 6);  // 12 bytes
                writer.WriteInt32(0);                                       // Price = 0
                writer.WriteUnicodeString("購入失敗", 101);                  // 202 bytes (101 WChars)
                writer.WriteByte(0xCC);                                     // 2-byte padding
                writer.WriteByte(0xCC);
                return writer.Build();
            }

            writer.WriteInt32(productId);
            writer.WriteInt64(price);                                       // ProductPrice (Int64, 8 bytes in Delphi)
            writer.WriteInt32(remainingStars);
            writer.WriteInt32(period);

            var items = deliveredItems != null && deliveredItems.Count > 0 ? deliveredItems : new List<Item>
            {
                new Item { TypeId = productId, SerialId = serialId }
            };

            writer.WriteUInt16((ushort)items.Count);
            foreach (var item in items)
            {
                writer.WriteInt32(item.TypeId);
                writer.WriteInt64(item.SerialId > 0 ? item.SerialId : serialId);
                writer.WriteInt32(1); // Unk1
                writer.WriteInt32(0); // IsEquipped = 0
                writer.WriteInt32(0x00015180); // Unk2 (86400 / 1 day in seconds or constant)
                for (int k = 0; k < 5; k++) writer.WriteInt32(0); // 5 reinforce slots (20B)
            }

            // Receipt & Confirmation details (exact Delphi byte counts)
            writer.WriteInt32(nowSec);
            writer.WriteUnicodeString($"OD{productId % 10000:D4}", 6); // 12 bytes (6 UTF-16 WChars)
            writer.WriteInt32(price);
            writer.WriteUnicodeString("アイテム購入完了", 101); // 202 bytes (101 UTF-16 WChars)
            writer.WriteByte(0xCC); // Delphi 0x005A9DF4 2-byte padding
            writer.WriteByte(0xCC);

            return writer.Build();
        }

        /// <summary>
        /// 0x5224 (21028): MsgGameShopBuyAns - Standard NPC Shop Buy Result
        /// </summary>
        public static byte[] MakeShopBuyAns(int rc, int itemId, int quantity, int price, int remainingMoney)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameShopBuyAns);
            writer.WriteInt32(rc);
            writer.WriteInt32(itemId);
            writer.WriteInt32(quantity);
            writer.WriteInt32(price);
            writer.WriteInt32(remainingMoney);
            return writer.Build();
        }

        /// <summary>
        /// 0x796E (31086): MsgGameMonInfoNtf - Monster Spawn / Stat Information
        /// Exact 32-byte layout from Delphi TMsgGameMonInfoNtf.Create (0x005AE1EC)
        /// </summary>
        public static byte[] MakeGameMonInfoNtf(FieldMonster monster)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameMonInfoNtf);
            writer.WriteInt32(monster.EntityId);
            writer.WriteInt32(monster.MonsterType);
            writer.WriteInt32(monster.CurrentHp);
            writer.WriteInt32(monster.MaxHp);
            writer.WriteUInt16((ushort)monster.X);
            writer.WriteUInt16((ushort)monster.Y);
            writer.WriteInt32(monster.DirX);
            writer.WriteInt32(monster.DirY);
            writer.WriteInt32(monster.TargetPlayerId != 0 ? 1 : 0);
            return writer.Build();
        }

        /// <summary>
        /// 0x7969 (31081): MsgGameMonMoveNtf - Monster Movement & Path Synchronization
        /// Exact layout from Delphi TMsgGameMonMoveNtf.Create (0x005ADF68) & 31081.dms:
        ///   WriteID(0x7969)
        ///   WriteInt32(idMonster)
        ///   WriteUInt16(curX)
        ///   WriteUInt16(curY)
        ///   WriteUInt16(destX)
        ///   WriteUInt16(destY)
        ///   WriteInt32(motion)       // 0 = Idle, 1 = Walk/Run
        ///   WriteByte(speedRate)     // 100 = 1.0x speed
        ///   FillBuffer(3, 0xCC)
        /// </summary>
        public static byte[] MakeGameMonMoveNtf(int idMonster, int curX, int curY, int destX, int destY, int motion = 1, byte speedRate = 100)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameMonMoveNtf);
            writer.WriteInt32(idMonster);
            writer.WriteUInt16((ushort)curX);
            writer.WriteUInt16((ushort)curY);
            writer.WriteUInt16((ushort)destX);
            writer.WriteUInt16((ushort)destY);
            writer.WriteInt32(motion);
            writer.WriteByte(speedRate);
            writer.WriteByte(0xCC);
            writer.WriteByte(0xCC);
            writer.WriteByte(0xCC);
            return writer.Build();
        }

        /// <summary>
        /// 0x796A (31082): MsgGameMonAttackNtf - Monster Attack Animation & Damage Broadcast
        /// Exact layout from Delphi TMsgGameMonAttackNtf.Create (0x005AE004) & 31082.dms:
        ///   WriteID(0x796A)
        ///   WriteInt32(idMonster)
        ///   WriteUInt16(posX)
        ///   WriteUInt16(posY)
        ///   WriteInt32(typeMotion)     // 0 or 1
        ///   WriteInt32(idTargetChar)   // Player CharaId
        ///   WriteInt32(damage)
        ///   WriteInt64(pocketMoney)    // 0
        ///   WriteByte(speedRate)       // 100
        ///   WriteByte(typeHit)         // 0 = Normal, 1 = Crit
        ///   FillBuffer(2, 0xCC)
        /// </summary>
        public static byte[] MakeGameMonAttackNtf(int idMonster, int posX, int posY, int idTargetChar, int damage, int typeMotion = 0, byte typeHit = 0)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameMonAttackNtf);
            writer.WriteInt32(idMonster);
            writer.WriteUInt16((ushort)posX);
            writer.WriteUInt16((ushort)posY);
            writer.WriteInt32(typeMotion);
            writer.WriteInt32(idTargetChar);
            writer.WriteInt32(damage);
            writer.WriteInt64(0);
            writer.WriteByte(100);
            writer.WriteByte(typeHit);
            writer.WriteByte(0xCC);
            writer.WriteByte(0xCC);
            return writer.Build();
        }

        /// <summary>
        /// 0x79D8 (31208): MsgGameMonHpInfoNtf - Monster Real-Time Overhead Health Bar Update
        /// Exact layout from Delphi TMsgGameMonHpInfoNtf.Create (0x005AFAE4) & 31208.dms:
        ///   WriteID(0x79D8)
        ///   WriteInt32(idMonster)
        ///   WriteInt32(hpCurrent)
        ///   WriteInt32(hpMax)
        /// </summary>
        public static byte[] MakeGameMonHpInfoNtf(int idMonster, int hpCurrent, int hpMax)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameMonHpInfoNtf);
            writer.WriteInt32(idMonster);
            writer.WriteInt32(hpCurrent);
            writer.WriteInt32(hpMax);
            return writer.Build();
        }

        /// <summary>
        /// 0x791B (31003): MsgGameDieCharNtf - Player Death Notification & Collapse Animation
        /// Exact layout from Delphi TMsgGameDieCharNtf.Create (0x005AC108):
        ///   WriteID(0x791B)
        ///   WriteInt32(charaId)
        ///   WriteMapPoint(x, y)
        /// </summary>
        public static byte[] MakeGameDieCharNtf(int charaId, int x, int y)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameDieCharNtf);
            writer.WriteInt32(charaId);
            writer.WriteUInt16((ushort)x);
            writer.WriteUInt16((ushort)y);
            return writer.Build();
        }

        /// <summary>
        /// 0x7990 (31120): MsgGameBeginCounterNtf - Initialize Mission / Hunt Monster Objective Counter
        /// Exact layout from Delphi TMsgGameBeginCounterNtf.Create (0x005AE8D8):
        ///   WriteID(0x7990)
        ///   WriteInt32(current)
        ///   WriteInt32(counterType = 1)
        ///   WriteInt32(descType = 1)
        ///   WriteInt32(iconId = 1)
        ///   WriteInt32(max)
        /// </summary>
        public static byte[] MakeGameBeginCounterNtf(int current, int max, int counterType = 1, int descType = 1, int iconId = 1)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameBeginCounterNtf);
            writer.WriteInt32(current);
            writer.WriteInt32(counterType);
            writer.WriteInt32(descType);
            writer.WriteInt32(iconId);
            writer.WriteInt32(max);
            return writer.Build();
        }

        /// <summary>
        /// 0x7993 (31123): MsgGameShowCounterNtf - Show Mission Counter UI Box
        /// Exact layout from Delphi TMsgGameShowCounterNtf.Create (0x005AEA14):
        ///   WriteID(0x7993)
        ///   WriteInt32(current)
        ///   WriteInt32(beginCount2 = 0)
        ///   WriteInt32(beginCount1 = 0)
        ///   WriteInt32(beginCount0 = 0)
        ///   WriteInt32(counterType = 1)
        ///   WriteInt32(descType = 1)
        ///   WriteInt32(iconId = 1)
        ///   WriteInt32(max)
        /// </summary>
        public static byte[] MakeGameShowCounterNtf(int current, int max, int counterType = 1, int descType = 1, int iconId = 1)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameShowCounterNtf);
            writer.WriteInt32(current);
            writer.WriteInt32(0);
            writer.WriteInt32(0);
            writer.WriteInt32(0);
            writer.WriteInt32(counterType);
            writer.WriteInt32(descType);
            writer.WriteInt32(iconId);
            writer.WriteInt32(max);
            return writer.Build();
        }

        /// <summary>
        /// 0x7959 (31065): MsgGameDisplayCounterNtf - Update Active Monster / Mission Counter
        /// Exact layout from Delphi TMsgGameDisplayCounterNtf.Create (0x005AD2A8):
        ///   WriteID(0x7959)
        ///   WriteInt32(current)
        ///   WriteInt32(iconId = 1)
        /// </summary>
        public static byte[] MakeGameDisplayCounterNtf(int current, int iconId = 1)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameDisplayCounterNtf);
            writer.WriteInt32(current);
            writer.WriteInt32(iconId);
            return writer.Build();
        }

        /// <summary>
        /// 0x791A (31002): MsgGameAttackAns - Attack Response & Damage Broadcast (Single-Target)
        /// </summary>
        public static byte[] MakeGameAttackAns(
            int charaId, 
            int targetEntityId, 
            int targetX, 
            int targetY, 
            int damage, 
            bool isCritical = false, 
            int combo = 1,
            int weaponCategory = 1,
            int skillId = 215,
            int addDexExp = 1)
        {
            var targets = new System.Collections.Generic.List<(int entityId, int x, int y, int damage, bool isCrit)>
            {
                (targetEntityId, targetX, targetY, damage, isCritical)
            };
            return MakeGameAttackAns(charaId, targets, combo, weaponCategory, skillId, addDexExp);
        }

        /// <summary>
        /// 0x791A (31002): MsgGameAttackAns - Attack Response & Damage Broadcast (Supports single & multi-target AoE)
        /// Exact layout from Delphi TMsgGameAttackAns (0x005AA2F8 / 0x005ABF00) matching Quartet 77B/98B payload
        /// </summary>
        public static byte[] MakeGameAttackAns(
            int charaId, 
            System.Collections.Generic.List<(int entityId, int x, int y, int damage, bool isCrit)> targets, 
            int combo = 1,
            int weaponCategory = 1,
            int skillId = 215,
            int addDexExp = 1)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameAttackAns);
            writer.WriteInt32(charaId);
            writer.WriteInt32(1); // ReturnCode: 1 = Success

            (int entityId, int x, int y, int damage, bool isCrit) main = targets != null && targets.Count > 0 ? targets[0] : (-1, 0, 0, 0, false);
            writer.WriteInt32(2); // targetMainType: 2 = Monster / Target
            writer.WriteInt32(main.entityId);
            writer.WriteInt32(main.x);
            writer.WriteInt32(main.y);

            int count = targets?.Count ?? 0;
            writer.WriteByte((byte)count); // cntTarget
            writer.WriteUInt16((ushort)count); // TargetsCount

            if (targets != null)
            {
                foreach (var t in targets)
                {
                    writer.WriteInt32(2); // targetType: 2
                    writer.WriteInt32(t.entityId);
                    writer.WriteInt32(t.x);
                    writer.WriteInt32(t.y);
                }

                writer.WriteUInt16((ushort)count); // DamagesCount
                foreach (var t in targets)
                {
                    writer.WriteInt32(t.damage);
                }

                writer.WriteUInt16((ushort)count); // typeHitsCount
                foreach (var t in targets)
                {
                    writer.WriteByte((byte)(t.isCrit ? 1 : 0));
                }
            }
            else
            {
                writer.WriteUInt16(0);
                writer.WriteUInt16(0);
            }

            // Metadata
            writer.WriteInt32(skillId); // skillId (Delphi 215 or weapon skill)
            writer.WriteInt32(0); // moneyDelta
            writer.WriteInt32(main.isCrit ? 1 : 0); // bCritical
            writer.WriteByte(0xCC); // padding (Delphi 0xCC)
            writer.WriteInt32(combo); // numCombo
            writer.WriteInt32(weaponCategory); // cateWeapon: 1=Blade, 2=Glove, 3=Blunt, 4=Spirit
            writer.WriteInt32(addDexExp); // addDexExp (1)

            return writer.Build();
        }

        /// <summary>
        /// 0x7924 (31012): MsgGameSkillActiveAns - Active Weapon Skill Execution Broadcast
        /// Delphi 0x005AC230 (TMsgGameSkillActiveAns)
        /// </summary>
        public static byte[] MakeGameSkillActiveAns(
            int charaId,
            int skillId,
            int seqNum,
            System.Collections.Generic.List<(int entityId, int x, int y, int damage, bool isCrit)> targets,
            int dexExp = 1)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameSkillActiveAns);
            writer.WriteInt32(charaId);
            writer.WriteInt32(skillId);
            writer.WriteInt32(seqNum);

            (int entityId, int x, int y, int damage, bool isCrit) main = targets != null && targets.Count > 0 ? targets[0] : (-1, 0, 0, 0, false);
            writer.WriteInt32(2); // targetMainType: 2 = Monster
            writer.WriteInt32(main.entityId);
            writer.WriteInt32(main.x);
            writer.WriteInt32(main.y);

            int count = targets?.Count ?? 0;
            writer.WriteByte((byte)count);
            writer.WriteUInt16((ushort)count);

            if (targets != null)
            {
                foreach (var t in targets)
                {
                    writer.WriteInt32(2);
                    writer.WriteInt32(t.entityId);
                    writer.WriteInt32(t.x);
                    writer.WriteInt32(t.y);
                }

                writer.WriteUInt16((ushort)count);
                foreach (var t in targets)
                {
                    writer.WriteInt32(t.damage);
                }

                writer.WriteUInt16((ushort)count);
                foreach (var t in targets)
                {
                    writer.WriteByte((byte)(t.isCrit ? 1 : 0));
                }
            }
            else
            {
                writer.WriteUInt16(0);
                writer.WriteUInt16(0);
            }

            writer.WriteInt32(dexExp);
            return writer.Build();
        }

        /// <summary>
        /// 0x5276 (21110): MsgGameHuntMonDeadNtf - Monster Death Animation and Loot Drop
        /// Exact layout from Delphi TMsgGameHuntMonDeadNtf.Create (0x005AA588)
        /// </summary>
        public static byte[] MakeGameHuntMonDeadNtf(FieldMonster monster, int killerCharaId, int expEarned, int totalExp, int dropItemId = 0, int dropCount = 0)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameHuntMonDeadNtf);
            writer.WriteInt32(monster.EntityId);
            writer.WriteUInt16((ushort)monster.X);
            writer.WriteUInt16((ushort)monster.Y);
            writer.WriteInt32(killerCharaId);
            writer.WriteInt32(expEarned);
            writer.WriteInt32(totalExp);

            if (dropItemId > 0 && dropCount > 0)
            {
                writer.WriteUInt16(1);
                writer.WriteUInt16(1);
                writer.WriteInt32(dropItemId | 0x03000000); // 0x03000000 = CoItem prefix
                writer.WriteInt32(dropCount);
                writer.WriteInt32(0);
            }
            else
            {
                writer.WriteUInt16(0);
                writer.WriteUInt16(0);
            }

            return writer.Build();
        }

        /// <summary>
        /// 0x5277 (21111): MsgGameHuntCharExpUpNtf - Player Hunting EXP Gain
        /// Exact layout from Delphi TMsgGameHuntCharExpUpNtf.Create (0x005AA7BC): WriteInt32(ExpEarned)
        /// </summary>
        public static byte[] MakeGameHuntCharExpUpNtf(int expEarned)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameHuntCharExpUpNtf);
            writer.WriteInt32(expEarned);
            return writer.Build();
        }

        /// <summary>
        /// 0x7A00 (31232): MsgGameMonDeadNtf - Despawn Monster from client 3D engine
        /// Exact layout from Delphi TMsgGameMonDeadNtf.Create (0x005B0169): WriteInt32(MonsterEntityId)
        /// </summary>
        public static byte[] MakeGameMonDeadNtf(int monsterEntityId)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameMonDeadNtf);
            writer.WriteInt32(monsterEntityId);
            return writer.Build();
        }

        /// <summary>
        /// 0x796A (31082): MsgGameMonActionNtf - Monster Animation & Target Notification (Attack/Hit/Death)
        /// Exact layout from Delphi TMsgGameMonActionNtf.Create (0x005AE01C)
        /// </summary>
        public static byte[] MakeGameMonActionNtf(int monsterEntityId, ushort x, ushort y, int motionType, int targetCharaId)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameMonActionNtf);
            writer.WriteInt32(monsterEntityId);
            writer.WriteUInt16(x);
            writer.WriteUInt16(y);
            writer.WriteInt32(motionType);
            writer.WriteInt32(targetCharaId);
            writer.WriteInt32(0);
            writer.WriteInt32(0);
            writer.WriteInt32(0);
            writer.WriteUInt16(0x64);
            writer.WriteByte(0xCC);
            writer.WriteByte(0xCC);
            return writer.Build();
        }

        /// <summary>
        /// 0x796D (31085): MsgGameMonStatusNtf - Live Monster Overhead HP Bar Sync
        /// Exact layout from Delphi TMsgGameMonStatusNtf.Create (0x005AE185)
        /// </summary>
        public static byte[] MakeGameMonStatusNtf(int monsterEntityId, int monsterTypeId, int curHp, int maxHp, ushort x, ushort y)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameMonStatusNtf);
            writer.WriteInt32(monsterEntityId);
            writer.WriteInt32(monsterTypeId);
            writer.WriteInt32(curHp);
            writer.WriteInt32(maxHp);
            writer.WriteUInt16(x);
            writer.WriteUInt16(y);
            writer.WriteInt32(0);
            writer.WriteInt32(1);
            return writer.Build();
        }

        /// <summary>
        /// 0x7970 (31088): MsgGameCharLvUpNtf - Player Level-Up Fanfare & Animation
        /// Exact layout from Delphi TMsgGameCharLvUpNtf.Create (0x005AE27C)
        /// </summary>
        public static byte[] MakeGameCharLvUpNtf(int newLevel)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameCharLvUpNtf);
            writer.WriteInt32(newLevel);
            return writer.Build();
        }

        /// <summary>
        /// 0x7929 (31017): MsgGameUseCoItemAns - Consumable Item Use Answer
        /// Exact layout from 31017.dms (COITEM使用返答):
        ///   WriteInt32(CharaID)
        ///   WriteInt32(Result)
        ///   WriteInt32(CoItemType)
        ///   WriteInt32(Count)
        /// </summary>
        public static byte[] MakeGameUseCoItemAns(int charaId, int result, int coItemType, int remainingCount)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameUseCoItemAns);
            writer.WriteInt32(charaId);
            writer.WriteInt32(result);
            writer.WriteInt32(coItemType);
            writer.WriteInt32(remainingCount);
            return writer.Build();
        }

        /// <summary>
        /// 0x7950 (31056): MsgGameRevivalSchoolAns - School Respawn / Revival Response
        /// Exact layout from Delphi _Unit67.pas:25486 (TMsgGameRevivalCharSchoolAns):
        ///   WriteInt32(Result) // 1 = Success
        /// </summary>
        public static byte[] MakeGameRevivalSchoolAns(int result = 1)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameRevivalSchoolAns);
            writer.WriteInt32(result);
            return writer.Build();
        }

        /// <summary>
        /// 0x794E (31054): MsgGameRevival119Ans - Emergency 119 Respawn / Revival Response
        /// Exact layout from Delphi TMsgGameRevivalChar119Ans.Create (0x005ACCF0):
        ///   WriteInt32(Result) // 1 = Success
        /// </summary>
        public static byte[] MakeGameRevival119Ans(int result = 1)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameRevival119Ans);
            writer.WriteInt32(result);
            return writer.Build();
        }

        /// <summary>
        /// 0x7759 (30553): MsgCommEchoNtf - Chat / Messenger Heartbeat Echo
        /// Exact layout from 30553.dms:
        ///   WriteInt32(SeqNum)
        /// </summary>
        public static byte[] MakeCommEchoNtf(int seqNum = 0)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgCommEchoNtf);
            writer.WriteInt32(seqNum);
            return writer.Build();
        }

        /// <summary>
        /// 0x5274 (21108): MsgGameWeaponFrameAns - Weapon Frame / Socket Information
        /// Exact layout from 21108.dms (武器フレーム情報返答):
        ///   WriteInt32(WeaponId)
        ///   WriteUInt16(SerialLow)
        ///   WriteUInt16(SerialHigh)
        ///   WriteInt32(SerialType)
        ///   WriteUInt16(AtkFrameCount)
        ///   WriteUInt16(SklFrameCount)
        /// </summary>
        public static byte[] MakeGameWeaponFrameAns(int weaponId, ushort serialLow = 0, ushort serialHigh = 0, int serialType = 0)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameWeaponFrameAns);
            writer.WriteInt32(weaponId);
            writer.WriteUInt16(serialLow);
            writer.WriteUInt16(serialHigh);
            writer.WriteInt32(serialType);
            writer.WriteUInt16(0); // AtkFrameCount = 0
            writer.WriteUInt16(0); // SklFrameCount = 0
            return writer.Build();
        }

        /// <summary>
        /// 0xA414 (42004): MsgGameCapsuleBuyAns - Capsule Machine Purchase Answer
        /// Exact layout from 42004.dms (カプセル販売機購入返答):
        ///   WriteInt32(Result) // 0 = Success
        ///   WriteInt32(Unknown = 0)
        ///   WriteInt32(RolledItemId)
        ///   WriteUInt16(Count = 1)
        ///   WriteUInt16(0)
        ///   WriteInt32(0)
        ///   WriteInt64(Price)
        ///   WriteInt64(ResultMoney)
        ///   WriteInt64(TotalAmount)
        /// </summary>
        public static byte[] MakeGameCapsuleBuyAns(int result, int rolledItemId, int count, long price, long remainingMoney)
        {
            using var writer = PacketWriter.Create(PacketOpcode.MsgGameCapsuleBuyAns);
            writer.WriteInt32(result);
            writer.WriteInt32(0);
            writer.WriteInt32(rolledItemId);
            writer.WriteUInt16((ushort)count);
            writer.WriteUInt16(0);
            writer.WriteInt32(0);
            writer.WriteInt64(price);
            writer.WriteInt64(remainingMoney);
            writer.WriteInt64(remainingMoney);
            return writer.Build();
        }
    }
}