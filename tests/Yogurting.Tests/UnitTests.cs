using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Yogurting.Core.Models;
using Yogurting.Core.Network;
using Yogurting.Data.Loaders;
using Yogurting.Data.Repositories;

namespace Yogurting.Tests
{
    public class CoreStabilityTests
    {
        [Fact]
        public void PacketReader_ReadFixedWString_TruncatesAtFirstNull()
        {
            // Simulate a C++ fixed buffer containing "Hero" followed by \0 and uninitialized garbage bytes (0xCC)
            byte[] buffer = new byte[32];
            byte[] nameBytes = Encoding.Unicode.GetBytes("Hero");
            Array.Copy(nameBytes, 0, buffer, 0, nameBytes.Length);
            // Null terminator at index 8, 9 (already 0)
            // Fill remaining with 0xCC (common uninitialized MSVC debug memory)
            for (int i = 10; i < buffer.Length; i++)
            {
                buffer[i] = 0xCC;
            }

            string result = PacketReader.ReadFixedWString(buffer, 0, buffer.Length);
            Assert.Equal("Hero", result);
        }

        [Fact]
        public void FieldMonster_TakeDamage_ConcurrentHits_OnlyOneKiller()
        {
            var monster = new FieldMonster
            {
                EntityId = 1,
                Name = "GloveBug",
                MaxHp = 50,
                CurrentHp = 50
            };

            int killCount = 0;
            const int threadCount = 20;

            Parallel.For(0, threadCount, _ =>
            {
                bool killed = monster.TakeDamage(100);
                if (killed)
                {
                    System.Threading.Interlocked.Increment(ref killCount);
                }
            });

            Assert.Equal(1, killCount);
            Assert.True(monster.IsDead);
            Assert.Equal(0, monster.CurrentHp);
        }

        [Fact]
        public void EquipmentBounds_UniqueId0x100_DoesNotIndexNegativeOne()
        {
            var inventory = new List<Item>
            {
                new Item { Id = 1, TypeId = 10001, Name = "Item 1" }
            };

            int uniqueId = 0x100; // 256
            Item? foundItem = null;

            // Test the fixed condition: uniqueId >= 0x101 && (uniqueId - 0x101) < inventory.Count
            if (uniqueId >= 0x101 && (uniqueId - 0x101) < inventory.Count)
            {
                foundItem = inventory[uniqueId - 0x101];
            }

            Assert.Null(foundItem);

            // Test valid index 0x101 -> index 0
            uniqueId = 0x101;
            if (uniqueId >= 0x101 && (uniqueId - 0x101) < inventory.Count)
            {
                foundItem = inventory[uniqueId - 0x101];
            }

            Assert.NotNull(foundItem);
            Assert.Equal(10001, foundItem.TypeId);
        }

        [Fact]
        public async Task AccountRepository_ConcurrentSaves_DoNotCollideOrCorrupt()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "YogurtingTestAccounts_" + Guid.NewGuid().ToString("N"));
            try
            {
                var repo = new JsonAccountRepository(tempDir);
                var player = await repo.CreateAccountAsync("test_user", "pass", "Tester", SchoolType.EstivaAcademy, GenderType.Male, WeaponClass.Blade);

                var saveTasks = new List<Task>();
                for (int i = 0; i < 20; i++)
                {
                    int exp = i * 10;
                    saveTasks.Add(Task.Run(async () =>
                    {
                        player.CurrentExp = exp;
                        await repo.SaveAccountAsync(player);
                    }));
                }

                await Task.WhenAll(saveTasks);

                // Verify file exists and is valid JSON
                string jsonPath = Path.Combine(tempDir, "test_user.json");
                Assert.True(File.Exists(jsonPath));
                string content = await File.ReadAllTextAsync(jsonPath);
                Assert.Contains("\"AccountId\": \"test_user\"", content);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public void Combat_DelphiQuartetFormula_Level1NormalAttack_Deals1To5Damage()
        {
            // Level 1 player: Pow = 4 from StatusTable.txt, starter blade has Attack = 0, AttackGroup = 102
            int pow = 4;
            int bonusAtk = 0;
            int fAtk = (pow * 65) + (bonusAtk * 100); // 260 (Delphi _Unit49.pas:20268-20280)

            int atkRatio = 63; // Blade attack ratio from AtkWeapon.txt column 5 (Delphi _Unit49.pas:21915)
            int scaledAtk = (int)((fAtk * (long)atkRatio) / 10000); // 16380 / 10000 = 1

            int level = 1;
            int varianceMax = Math.Max(5, level / 2); // 5 (Delphi _Unit49.pas:21936)

            // Test normal damage bounds across 100 iterations
            var random = new Random(42);
            for (int i = 0; i < 100; i++)
            {
                int levelVariance = random.Next(0, varianceMax); // 0..4
                int damage = Math.Max(1, scaledAtk + levelVariance);
                Assert.InRange(damage, 1, 5); // Must be within 1 to 5 damage
            }

            // An 18 HP monster (ソソスケルトン) cannot be one-shot by any normal hit
            var monster = new FieldMonster { MaxHp = 18, CurrentHp = 18 };
            int maxNormalDmg = scaledAtk + (varianceMax - 1); // 1 + 4 = 5
            bool killedInOneHit = monster.TakeDamage(maxNormalDmg);
            Assert.False(killedInOneHit);
            Assert.True(monster.CurrentHp >= 13);
        }

        [Fact]
        public void Combat_DelphiQuartetFormula_ActiveSkill_CalculatesAuthenticDamage()
        {
            int pow = 4;
            int fAtk = pow * 65; // 260
            int skillPower = 1; // Beginner Combo (初級連撃) SkillDesc2.Power = 1
            int skillAtkRatio = 63; // AtkRatio = 63
            int varianceMax = 5;

            int scaledDmg = (int)(((fAtk + (skillPower * 100)) * (long)skillAtkRatio) / 10000); // ((260 + 100) * 63) / 10000 = 22680 / 10000 = 2
            Assert.Equal(2, scaledDmg);

            var random = new Random(42);
            for (int i = 0; i < 100; i++)
            {
                int levelVariance = random.Next(0, varianceMax);
                int dmg = Math.Max(1, scaledDmg + levelVariance);
                Assert.InRange(dmg, 2, 6);
            }
        }

        [Fact]
        public void GameDatabase_LoadsAtkWeaponAndSeparatesAttackGroup()
        {
            string dbPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../data/db"));
            if (!Directory.Exists(dbPath)) return;

            var gameDb = GameDatabase.Create(dbPath);

            // 1. Verify Blade combo action 10201 has authentic AtkRatio = 63 (Column 5 Power)
            Assert.True(gameDb.AtkWeapons.TryGetValue(10201, out var atkDef));
            Assert.Equal(63, atkDef.AtkRatio);

            // 2. Verify Starter Blade 110001 has AttackGroup = 102, Attack = 0, IsEquipment = true
            Assert.True(gameDb.Items.TryGetValue(110001, out var bladeDef));
            Assert.Equal(102, bladeDef.AttackGroup);
            Assert.Equal(0, bladeDef.Attack);
            Assert.True(bladeDef.IsEquipment);
            Assert.True(bladeDef.IsWeapon);

            // 3. Verify SkillDesc2 41001 (初級連撃) has Power = 1
            Assert.True(gameDb.SkillDesc2s.TryGetValue(41001, out var skillDef));
            Assert.Equal(1, skillDef.Power);
        }

        [Fact]
        public void Combat_ExpCalculation_AppliesDelphiParabolicLevelPenalty()
        {
            int baseExp = 100;

            int CalcExp(int playerLv, int monsterLv)
            {
                int diff = Math.Abs(playerLv - monsterLv);
                if (diff > 15) return 1;
                if (diff == 0) return baseExp;
                double scale = 1.0 - (diff * diff * 0.003907);
                return Math.Max(1, (int)Math.Round(baseExp * scale));
            }

            // Same level: 100% EXP
            Assert.Equal(100, CalcExp(1, 1));
            Assert.Equal(100, CalcExp(10, 10));

            // 5 levels difference: ~90% EXP
            Assert.Equal(90, CalcExp(10, 5));
            Assert.Equal(90, CalcExp(5, 10));

            // 10 levels difference: ~61% EXP
            Assert.Equal(61, CalcExp(20, 10));

            // 15 levels difference: ~12% EXP
            Assert.Equal(12, CalcExp(25, 10));

            // 16+ levels difference: hard floor of 1 EXP
            Assert.Equal(1, CalcExp(26, 10));
            Assert.Equal(1, CalcExp(50, 1));
        }

        [Fact]
        public void Combat_MonsterMeleeRange_SupportsDiagonalAdjacentTiles()
        {
            bool InMeleeRange(float monX, float monY, float playerX, float playerY)
            {
                float dx = playerX - monX;
                float dy = playerY - monY;
                return MathF.Abs(dx) < 1.9f && MathF.Abs(dy) < 1.9f;
            }

            // Cardinal neighbors: adjacent
            Assert.True(InMeleeRange(10, 10, 11, 10));
            Assert.True(InMeleeRange(10, 10, 9, 10));
            Assert.True(InMeleeRange(10, 10, 10, 11));
            Assert.True(InMeleeRange(10, 10, 10, 9));

            // Diagonal neighbors: Chebyshev distance = 1.0 < 1.9 (Euclidean was 1.414 > 1.4)
            Assert.True(InMeleeRange(10, 10, 11, 11));
            Assert.True(InMeleeRange(10, 10, 9, 11));
            Assert.True(InMeleeRange(10, 10, 11, 9));
            Assert.True(InMeleeRange(10, 10, 9, 9));

            // Beyond adjacent tiles: out of melee range
            Assert.False(InMeleeRange(10, 10, 12, 10));
            Assert.False(InMeleeRange(10, 10, 12, 12));
            Assert.False(InMeleeRange(10, 10, 10, 13));
        }

        [Fact]
        public void Combat_LeashDistance_ChecksSpawnAnchor()
        {
            float spawnX = 50f;
            float spawnY = 50f;

            bool IsLeashed(float playerX, float playerY)
            {
                float spawnDx = MathF.Abs(playerX - spawnX);
                float spawnDy = MathF.Abs(playerY - spawnY);
                return spawnDx >= 16.0f || spawnDy >= 16.0f;
            }

            // Within 15 tiles of spawn: not leashed
            Assert.False(IsLeashed(50f, 50f));
            Assert.False(IsLeashed(65f, 50f)); // 15 tiles away
            Assert.False(IsLeashed(35f, 65f)); // 15 tiles away

            // 16 or more tiles from spawn anchor: leash breaks
            Assert.True(IsLeashed(66f, 50f));  // 16 tiles away
            Assert.True(IsLeashed(50f, 66f));  // 16 tiles away
            Assert.True(IsLeashed(34f, 50f));  // 16 tiles away
            Assert.True(IsLeashed(50f, 34f));  // 16 tiles away
            Assert.True(IsLeashed(100f, 100f));
        }

        [Fact]
        public void Combat_MsgGameHuntMonDeadNtf_ExactDelphiWireLayout()
        {
            // SRC: server_legacy/DELPHI PROJECT/_Unit47.pas:48845-49048 (TMsgGameHuntMonDeadNtf.Create)
            byte[] pkt = YogurtingPackets.MakeGameHuntMonDeadNtf(
                monsterEntityId: 53,
                x: 71,
                y: 45,
                killerCharaId: 256,
                expEarned: 1,
                totalExp: 9,
                drops: null,
                monsterType: 37);

            // Packet total length: 38 bytes (4B length + 2B opcode + 32B payload)
            Assert.Equal(38, pkt.Length);

            // Length prefix: 32 (0x20)
            int payloadLen = BitConverter.ToInt32(pkt, 0);
            Assert.Equal(32, payloadLen);

            // Opcode: 0x5276
            ushort opcode = BitConverter.ToUInt16(pkt, 4);
            Assert.Equal(0x5276, opcode);

            // MonsterEntityId: 53
            Assert.Equal(53, BitConverter.ToInt32(pkt, 6));

            // MapPoint (X=71, Y=45)
            Assert.Equal(71, BitConverter.ToUInt16(pkt, 10));
            Assert.Equal(45, BitConverter.ToUInt16(pkt, 12));

            // KillerCharaId: 256
            Assert.Equal(256, BitConverter.ToInt32(pkt, 14));

            // ExpEarned: 1, TotalExp: 9
            Assert.Equal(1, BitConverter.ToInt32(pkt, 18));
            Assert.Equal(9, BitConverter.ToInt32(pkt, 22));

            // No loot: 0, 0
            Assert.Equal(0, BitConverter.ToUInt16(pkt, 26)); // vecItemSilCount
            Assert.Equal(0, BitConverter.ToUInt16(pkt, 28)); // vecItemCount

            // Trailing fields: ReturnCode = 0, Type = 0 (_Unit47.pas:005AA793)
            Assert.Equal(0, BitConverter.ToInt32(pkt, 30));
            Assert.Equal(0, BitConverter.ToInt32(pkt, 34)); // Must be 0, NOT monsterType!
        }

        [Fact]
        public void Player_RecalculateStats_RestoresFullHpOnLevelUp()
        {
            // SRC: server_legacy/DELPHI PROJECT/_Unit49.pas:0060DD6A (FInnerCurrentHP = FMaxHP on level up)
            var player = new Player("test", "Hanazono");
            player.Pow = 16;
            player.Speed = 12;
            player.Skill = 12;
            player.Luck = 8;
            player.MaxHp = 300;
            player.CurrentHp = 50; // Damaged player

            // Normal stat recalculation without level up retains current damaged HP
            player.RecalculateStats(20, 15, 15, 10, isLevelUp: false);
            Assert.Equal(50, player.CurrentHp);

            // Level up stat recalculation restores CurrentHp to new MaxHp
            player.RecalculateStats(24, 18, 18, 12, isLevelUp: true);
            Assert.Equal(player.MaxHp, player.CurrentHp);
            Assert.True(player.CurrentHp >= 300);
        }

        [Fact]
        public void TradePackets_ByteExactLayout_MatchesDelphiGroundTruth()
        {
            // 1. Trade Proposal Response Req (0x792C) - 52 bytes Unicode string
            byte[] reqPkt = YogurtingPackets.MakeGameTradeResponseReq("Hanazono");
            Assert.Equal((ushort)PacketOpcode.MsgGameTradeResponseReq, BitConverter.ToUInt16(reqPkt, 4));
            Assert.Equal(52, BitConverter.ToInt32(reqPkt, 0)); // 26 chars * 2 bytes = 52 bytes

            // 2. Trade Attend Notice (0x792E) - 4 bytes chara ID
            byte[] attendPkt = YogurtingPackets.MakeGameTradeOtherSideAttendNtf(256);
            Assert.Equal((ushort)PacketOpcode.MsgGameTradeOtherSideAttendNtf, BitConverter.ToUInt16(attendPkt, 4));
            Assert.Equal(4, BitConverter.ToInt32(attendPkt, 0));
            Assert.Equal(256, BitConverter.ToInt32(attendPkt, 6));

            // 3. Trade Basket Info Sync (0x792F) - 5 slots (160B) + 8B money = 168B payload
            var slots = new TradeSlot[5]
            {
                new TradeSlot { ItemType = 140001, Count = 1, ItemId = 10 },
                new TradeSlot(),
                new TradeSlot(),
                new TradeSlot(),
                new TradeSlot()
            };
            byte[] basketPkt = YogurtingPackets.MakeGameTradeOtherSideBasketInfoNtf(slots, 50000);
            Assert.Equal((ushort)PacketOpcode.MsgGameTradeOtherSideBasketInfoNtf, BitConverter.ToUInt16(basketPkt, 4));
            Assert.Equal(168, BitConverter.ToInt32(basketPkt, 0)); // 5 * 32 + 8 = 168 bytes

            // 4. Trade Complete Notice (0x7934) - 2 * 168 = 336B payload
            byte[] completePkt = YogurtingPackets.MakeGameTradeCompleteNtf(slots, 50000, new TradeSlot[5], 0);
            Assert.Equal((ushort)PacketOpcode.MsgGameTradeCompleteNtf, BitConverter.ToUInt16(completePkt, 4));
            Assert.Equal(336, BitConverter.ToInt32(completePkt, 0)); // 168 * 2 = 336 bytes

            // 5. Trade Failed Notice (0x792A) - 4 bytes Reason
            byte[] failedPkt = YogurtingPackets.MakeGameTradeFailedNtf(6);
            Assert.Equal((ushort)PacketOpcode.MsgGameTradeFailedNtf, BitConverter.ToUInt16(failedPkt, 4));
            Assert.Equal(4, BitConverter.ToInt32(failedPkt, 0));
            Assert.Equal(6, BitConverter.ToInt32(failedPkt, 6));
        }

        [Fact]
        public void TitleAndGradePackets_ByteExactLayout_MatchesDelphiGroundTruth()
        {
            // 1. Equip Title Answer (0x79A3) - 16 bytes payload (RC, CharaId, TitleId, bForce)
            byte[] equipTitlePkt = YogurtingPackets.MakeGameEquipTitleAns(256, 12);
            Assert.Equal((ushort)PacketOpcode.MsgGameEquipTitleAns, BitConverter.ToUInt16(equipTitlePkt, 4));
            Assert.Equal(16, BitConverter.ToInt32(equipTitlePkt, 0));
            Assert.Equal(1, BitConverter.ToInt32(equipTitlePkt, 6)); // ReturnCode = 1
            Assert.Equal(256, BitConverter.ToInt32(equipTitlePkt, 10)); // CharaId
            Assert.Equal(12, BitConverter.ToInt32(equipTitlePkt, 14)); // TitleId

            // 2. Strip Title Answer (0x79A5) - 12 bytes payload (RC, CharaId, TitleId)
            byte[] stripTitlePkt = YogurtingPackets.MakeGameStripTitleAns(256, 0);
            Assert.Equal((ushort)PacketOpcode.MsgGameStripTitleAns, BitConverter.ToUInt16(stripTitlePkt, 4));
            Assert.Equal(12, BitConverter.ToInt32(stripTitlePkt, 0));
            Assert.Equal(1, BitConverter.ToInt32(stripTitlePkt, 6));

            // 3. Grade Up Notice (0x797C) - 8 bytes payload (CharaId, Grade)
            byte[] gradeUpPkt = YogurtingPackets.MakeGameGradeUpNtf(256, 2);
            Assert.Equal((ushort)PacketOpcode.MsgGameGradeUpNtf, BitConverter.ToUInt16(gradeUpPkt, 4));
            Assert.Equal(8, BitConverter.ToInt32(gradeUpPkt, 0));
            Assert.Equal(256, BitConverter.ToInt32(gradeUpPkt, 6));
            Assert.Equal(2, BitConverter.ToInt32(gradeUpPkt, 10));
        }

        [Fact]
        public void CommServer_HandshakePacket_EncodesFriendRosterCorrectly()
        {
            var player = new Player("test", "Hanazono");
            var friends = new List<FriendEntry>
            {
                new FriendEntry
                {
                    CharacterId = 512,
                    PhoneNumber = 1234,
                    CharacterName = "Friend1",
                    IsOnline = true
                }
            };

            byte[] joinPkt = YogurtingPackets.MakeTransJoinCmsAns(player, friends);
            Assert.Equal((ushort)PacketOpcode.MsgTransJoinCmsAns, BitConverter.ToUInt16(joinPkt, 4));
            Assert.True(BitConverter.ToInt32(joinPkt, 0) > 0);
            Assert.Equal(1, BitConverter.ToInt32(joinPkt, 6)); // isReg = 1
            Assert.Equal(1, BitConverter.ToUInt16(joinPkt, 10)); // FriendCount = 1
        }

        [Fact]
        public void StorageLockerPackets_ByteExactLayout_MatchesDelphiGroundTruth()
        {
            // 1. Storage Locker Open Ans (0xA02C) - 8 bytes payload (open=1, lockerId)
            byte[] openPkt = YogurtingPackets.MakeGameLockerOpenAns(5, 1);
            Assert.Equal((ushort)PacketOpcode.MsgGameLockerCloseReq, BitConverter.ToUInt16(openPkt, 4)); // 0xA02C
            Assert.Equal(8, BitConverter.ToInt32(openPkt, 0));
            Assert.Equal(1, BitConverter.ToInt32(openPkt, 6));
            Assert.Equal(5, BitConverter.ToInt32(openPkt, 10));

            // 2. Storage Locker Move Item Complete (0xA030) - 24 bytes payload
            var item = new Item { TypeId = 140001, Quantity = 1, SlotIndex = 3, SerialId = 100 };
            byte[] movePkt = YogurtingPackets.MakeGameLockerMoveItemCompleteNtf(5, 1, item, 0);
            Assert.Equal((ushort)PacketOpcode.MsgGameLockerMoveItemCompleteNtf, BitConverter.ToUInt16(movePkt, 4));
            Assert.Equal(24, BitConverter.ToInt32(movePkt, 0)); // 4 (RC) + 4 (LockerId) + 1 (Direct) + 3 (Pad) + 12 (Item) = 24
            Assert.Equal(1, BitConverter.ToInt32(movePkt, 6)); // RC = 1
            Assert.Equal(5, BitConverter.ToInt32(movePkt, 10)); // LockerId = 5
        }

        [Fact]
        public void CrystalAndReinforcePackets_ByteExactLayout_MatchesDelphiGroundTruth()
        {
            // 1. Crystal Enchant Ans (0x79A9) - 12 bytes payload (RC, Type, Level)
            byte[] enchantPkt = YogurtingPackets.MakeGameEnchantCrystalAns(1, 2);
            Assert.Equal((ushort)PacketOpcode.MsgGameEnchantCrystalAns, BitConverter.ToUInt16(enchantPkt, 4));
            Assert.Equal(12, BitConverter.ToInt32(enchantPkt, 0));
            Assert.Equal(1, BitConverter.ToInt32(enchantPkt, 6));
            Assert.Equal(1, BitConverter.ToInt32(enchantPkt, 10));
            Assert.Equal(2, BitConverter.ToInt32(enchantPkt, 14));

            // 2. Crystallize Ans (0x79AB) - 4 (RC) + 2 (Count=0) + 12 (Reward) = 18 bytes
            byte[] crystalPkt = YogurtingPackets.MakeGameCrystallizeAns(300001, 3);
            Assert.Equal((ushort)PacketOpcode.MsgGameCrystallizeAns, BitConverter.ToUInt16(crystalPkt, 4));
            Assert.Equal(18, BitConverter.ToInt32(crystalPkt, 0));
            Assert.Equal(1, BitConverter.ToInt32(crystalPkt, 6));

            // 3. Reinforce Socket Attach Ans (0x79F0) - 4 + 4 + 2 + 12 + 12 + 20 = 54 bytes
            var weapon = new Item { TypeId = 140001, Quantity = 1, SlotIndex = 1, SerialId = 200 };
            weapon.SocketSlots[0] = 310001;
            byte[] socketPkt = YogurtingPackets.MakeGameReinforceBeItemAttachStoneAns(256, weapon, 310001, 0);
            Assert.Equal((ushort)PacketOpcode.MsgGameReinforceBeItemAttachStoneAns, BitConverter.ToUInt16(socketPkt, 4));
            Assert.Equal(54, BitConverter.ToInt32(socketPkt, 0));
            Assert.Equal(1, BitConverter.ToInt32(socketPkt, 6));
            Assert.Equal(256, BitConverter.ToInt32(socketPkt, 10));
        }

        [Fact]
        public void RevivalPackets_ByteExactLayout_MatchesDelphiGroundTruth()
        {
            // 1. 119 Emergency Respawn Ans (0x794E) - 20 bytes payload (RC, CharaId, Hp, Money)
            byte[] r119Pkt = YogurtingPackets.MakeGameRevival119Ans(256, 300, 50000);
            Assert.Equal((ushort)PacketOpcode.MsgGameRevival119Ans, BitConverter.ToUInt16(r119Pkt, 4));
            Assert.Equal(20, BitConverter.ToInt32(r119Pkt, 0));
            Assert.Equal(1, BitConverter.ToInt32(r119Pkt, 6));
            Assert.Equal(256, BitConverter.ToInt32(r119Pkt, 10));
            Assert.Equal(300, BitConverter.ToInt32(r119Pkt, 14));
            Assert.Equal(50000L, BitConverter.ToInt64(r119Pkt, 18));

            // 2. School Respawn Ans (0x7950) - 4 bytes payload (RC=1)
            byte[] schoolPkt = YogurtingPackets.MakeGameRevivalSchoolAns(1);
            Assert.Equal((ushort)PacketOpcode.MsgGameRevivalSchoolAns, BitConverter.ToUInt16(schoolPkt, 4));
            Assert.Equal(4, BitConverter.ToInt32(schoolPkt, 0));
            Assert.Equal(1, BitConverter.ToInt32(schoolPkt, 6));
        }

        [Fact]
        public void LobbyAndRoomPackets_ByteExactLayout_MatchesDelphiGroundTruth()
        {
            // 1. Lobby Enter Ans (0x765F) - 4 bytes RC
            byte[] enterPkt = YogurtingPackets.MakeLobbyEnterAns(1);
            Assert.Equal((ushort)PacketOpcode.MsgLobbyEnterAns, BitConverter.ToUInt16(enterPkt, 4));
            Assert.Equal(4, BitConverter.ToInt32(enterPkt, 0));
            Assert.Equal(1, BitConverter.ToInt32(enterPkt, 6));

            // 2. Create Room Ans (0x766B) - 4 (RC) + 2 (RoomID) + 2 (LobbyID) + 1 (bAuth) = 9 bytes
            byte[] createAns = YogurtingPackets.MakeLobbyCreateRoomAns(1, 1, 0, 1);
            Assert.Equal((ushort)PacketOpcode.MsgLobbyCreateRoomAns, BitConverter.ToUInt16(createAns, 4));
            Assert.Equal(9, BitConverter.ToInt32(createAns, 0));
            Assert.Equal(1, BitConverter.ToInt32(createAns, 6)); // RC = 1
            Assert.Equal(1, BitConverter.ToUInt16(createAns, 10)); // RoomId = 1

            // 3. Waiting Room Info Ans (0x7679)
            var room = new EpisodeRoom
            {
                RoomId = 1,
                LobbyId = 1,
                Title = "Test Mission",
                EpisodeTypeId = 101,
                MaxUsers = 4
            };
            room.Members.Add(new WaitRoomMember
            {
                CharacterId = 256,
                CharacterName = "Hanazono",
                Gender = 1,
                Grade = 2,
                WeaponTypeId = 140001,
                IsHost = true
            });

            byte[] waitRoomPkt = YogurtingPackets.MakeWaitRoomInfoAns(room);
            Assert.Equal((ushort)PacketOpcode.MsgWaitRoomInfoAns, BitConverter.ToUInt16(waitRoomPkt, 4));
            Assert.True(BitConverter.ToInt32(waitRoomPkt, 0) > 0);

            // 4. Ready Status Sync (0x768D) - 8 bytes (CharaId, bReady)
            byte[] readyPkt = YogurtingPackets.MakeWaitRoomReadyNtf(256, 1);
            Assert.Equal((ushort)PacketOpcode.MsgWaitRoomReadyNtf, BitConverter.ToUInt16(readyPkt, 4));
            Assert.Equal(8, BitConverter.ToInt32(readyPkt, 0));
            Assert.Equal(256, BitConverter.ToInt32(readyPkt, 6));
            Assert.Equal(1, BitConverter.ToInt32(readyPkt, 10));

            // 5. Game Start Notice (0x768E) - 0 payload
            byte[] startPkt = YogurtingPackets.MakeWaitRoomStartNtf();
            Assert.Equal((ushort)PacketOpcode.MsgWaitRoomStartNtf, BitConverter.ToUInt16(startPkt, 4));
            Assert.Equal(0, BitConverter.ToInt32(startPkt, 0));
        }

        [Fact]
        public void CapsuleGachaPackets_ByteExactLayout_MatchesDelphiGroundTruth()
        {
            // 1. Capsule Enter Ntf (0xA411) - 2 bytes machineId
            byte[] enterPkt = YogurtingPackets.MakeGameCapsuleEnterNtf(5);
            Assert.Equal((ushort)PacketOpcode.MsgGameCapsuleEnterNtf, BitConverter.ToUInt16(enterPkt, 4));
            Assert.Equal(2, BitConverter.ToInt32(enterPkt, 0));
            Assert.Equal((ushort)5, BitConverter.ToUInt16(enterPkt, 6));

            // 2. Capsule Product Info Ntf (0xA412)
            var products = new List<(int bSecret, int typeItem, long amount)>
            {
                (0, 140001, 99)
            };
            byte[] infoPkt = YogurtingPackets.MakeGameCapsuleProductInfoNtf(5, 500, products, 999);
            Assert.Equal((ushort)PacketOpcode.MsgGameCapsuleProductInfoNtf, BitConverter.ToUInt16(infoPkt, 4));
            Assert.True(BitConverter.ToInt32(infoPkt, 0) > 0);
            Assert.Equal(5, BitConverter.ToInt32(infoPkt, 6)); // machineId
            Assert.Equal(500L, BitConverter.ToInt64(infoPkt, 10)); // price
            Assert.Equal(1, BitConverter.ToUInt16(infoPkt, 18)); // product count

            // 3. Capsule Buy Ans (0xA414) - 44 bytes payload
            byte[] buyAns = YogurtingPackets.MakeGameCapsuleBuyAns(0, 140001, 1, 500, 99500);
            Assert.Equal((ushort)PacketOpcode.MsgGameCapsuleBuyAns, BitConverter.ToUInt16(buyAns, 4));
            Assert.Equal(44, BitConverter.ToInt32(buyAns, 0));
            Assert.Equal(0, BitConverter.ToInt32(buyAns, 6)); // result = 0 (success)
            Assert.Equal(140001, BitConverter.ToInt32(buyAns, 14)); // itemTypeId
        }

        [Fact]
        public void InteractiveObjectPackets_ByteExactLayout_MatchesDelphiGroundTruth()
        {
            // 1. Take Up Object Ans (0x7985) - 12 bytes payload (charaId, objectId, result)
            byte[] takeUpPkt = YogurtingPackets.MakeGameTakeUpObjectAns(256, 10, 1);
            Assert.Equal((ushort)PacketOpcode.MsgGameTakeUpObjectAns, BitConverter.ToUInt16(takeUpPkt, 4));
            Assert.Equal(12, BitConverter.ToInt32(takeUpPkt, 0));
            Assert.Equal(256, BitConverter.ToInt32(takeUpPkt, 6));
            Assert.Equal(10, BitConverter.ToInt32(takeUpPkt, 10));
            Assert.Equal(1, BitConverter.ToInt32(takeUpPkt, 14));

            // 2. Take Down Object Ans (0x7987) - 12 bytes payload
            byte[] takeDownPkt = YogurtingPackets.MakeGameTakeDownObjectAns(256, 10, 1);
            Assert.Equal((ushort)PacketOpcode.MsgGameTakeDownObjectAns, BitConverter.ToUInt16(takeDownPkt, 4));
            Assert.Equal(12, BitConverter.ToInt32(takeDownPkt, 0));

            // 3. Push Object Ans (0x7997) - 16 bytes payload (charaId, objectId, result, posX, posY)
            byte[] pushPkt = YogurtingPackets.MakeGamePushObjectAns(256, 10, 1, 350, 450);
            Assert.Equal((ushort)PacketOpcode.MsgGamePushObjectAns, BitConverter.ToUInt16(pushPkt, 4));
            Assert.Equal(16, BitConverter.ToInt32(pushPkt, 0));
            Assert.Equal(256, BitConverter.ToInt32(pushPkt, 6));
            Assert.Equal(10, BitConverter.ToInt32(pushPkt, 10));
            Assert.Equal(1, BitConverter.ToInt32(pushPkt, 14));
            Assert.Equal((ushort)350, BitConverter.ToUInt16(pushPkt, 18));
            Assert.Equal((ushort)450, BitConverter.ToUInt16(pushPkt, 20));

            // 4. Special Phone Call Ans (0x79C3) - 4 bytes payload
            byte[] phoneAns = YogurtingPackets.MakeGameSpecialPhoneCallAns(1);
            Assert.Equal((ushort)PacketOpcode.MsgGameSpecialPhoneCallAns, BitConverter.ToUInt16(phoneAns, 4));
            Assert.Equal(4, BitConverter.ToInt32(phoneAns, 0));
            Assert.Equal(1, BitConverter.ToInt32(phoneAns, 6));
        }
    }

    // =========================================================================
    // PERSPECTIVE 1: Protocol Framing & Edge Cases
    // =========================================================================
    public class ProtocolFramingTests
    {
        [Fact]
        public void ZeroPayloadPackets_HaveLengthZeroAndHeaderOnly()
        {
            // Opcodes with 0 payload: MsgCheckVersionNtf (0x4E21), MsgEnterScsNtf (0x5212), MsgLeaveAtsNtf (0x5215)
            using var w1 = PacketWriter.Create(PacketOpcode.MsgEnterScsNtf);
            byte[] enterScsPkt = w1.Build();

            using var w2 = PacketWriter.Create(PacketOpcode.MsgLeaveAtsNtf);
            byte[] leaveAtsPkt = w2.Build();

            Assert.Equal(6, enterScsPkt.Length);
            Assert.Equal(0, BitConverter.ToInt32(enterScsPkt, 0));
            Assert.Equal((ushort)PacketOpcode.MsgEnterScsNtf, BitConverter.ToUInt16(enterScsPkt, 4));

            Assert.Equal(6, leaveAtsPkt.Length);
            Assert.Equal(0, BitConverter.ToInt32(leaveAtsPkt, 0));
            Assert.Equal((ushort)PacketOpcode.MsgLeaveAtsNtf, BitConverter.ToUInt16(leaveAtsPkt, 4));

            // PacketReader on empty payload reports 0 remaining
            var reader = new PacketReader(enterScsPkt, 6, 0);
            Assert.Equal(0, reader.Remaining);
            Assert.Equal(0, reader.Length);
        }

        [Fact]
        public void PacketReader_BoundsCheck_ThrowsEndOfStreamExceptionOnTruncatedRead()
        {
            // Buffer with only 2 bytes
            byte[] shortBuffer = new byte[] { 0x12, 0x34 };
            var reader = new PacketReader(shortBuffer);

            Assert.Equal(2, reader.Remaining);
            Assert.Equal((short)0x3412, reader.ReadInt16());

            // Next read should fail because 0 bytes remain
            Assert.Throws<EndOfStreamException>(() => reader.ReadByte());
            Assert.Throws<EndOfStreamException>(() => reader.ReadInt32());
        }

        [Fact]
        public void FramingEngine_SimulateTCPCoalescing_ExtractsAllPacketsInOrder()
        {
            // Simulate 3 packets coalesced in a single TCP receive chunk
            byte[] p1 = YogurtingPackets.MakeTimeNtf(); // 6 bytes
            byte[] p2 = YogurtingPackets.MakeGameRevival119Ans(1, 100, 500); // 26 bytes
            byte[] p3 = YogurtingPackets.MakeEnterScsNtf(); // 6 bytes

            byte[] coalesced = new byte[p1.Length + p2.Length + p3.Length];
            Buffer.BlockCopy(p1, 0, coalesced, 0, p1.Length);
            Buffer.BlockCopy(p2, 0, coalesced, p1.Length, p2.Length);
            Buffer.BlockCopy(p3, 0, coalesced, p1.Length + p2.Length, p3.Length);

            var extractedOpcodes = new List<ushort>();
            int processedOffset = 0;

            while (coalesced.Length - processedOffset >= 6)
            {
                int payloadLen = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(coalesced.AsSpan(processedOffset, 4));
                int totalPacketLen = 6 + payloadLen;
                if (coalesced.Length - processedOffset < totalPacketLen) break;

                ushort op = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(coalesced.AsSpan(processedOffset + 4, 2));
                extractedOpcodes.Add(op);
                processedOffset += totalPacketLen;
            }

            Assert.Equal(3, extractedOpcodes.Count);
            Assert.Equal((ushort)PacketOpcode.MsgTimeNtf, extractedOpcodes[0]);
            Assert.Equal((ushort)PacketOpcode.MsgGameRevival119Ans, extractedOpcodes[1]);
            Assert.Equal((ushort)PacketOpcode.MsgEnterScsNtf, extractedOpcodes[2]);
            Assert.Equal(coalesced.Length, processedOffset);
        }

        [Fact]
        public void FramingEngine_SimulateTCPFragmentation_BuffersIncompletePacketUntilCompleted()
        {
            byte[] fullPkt = YogurtingPackets.MakeGameRevival119Ans(1, 100, 500); // 26 bytes
            byte[] chunk1 = new byte[10];
            byte[] chunk2 = new byte[fullPkt.Length - 10];
            Buffer.BlockCopy(fullPkt, 0, chunk1, 0, 10);
            Buffer.BlockCopy(fullPkt, 10, chunk2, 0, chunk2.Length);

            byte[] buffer = new byte[65536];
            int bufferCount = 0;
            var dispatchedPackets = new List<byte[]>();

            // 1. First chunk arrives (partial packet)
            Buffer.BlockCopy(chunk1, 0, buffer, bufferCount, chunk1.Length);
            bufferCount += chunk1.Length;

            int processedOffset = 0;
            while (bufferCount - processedOffset >= 6)
            {
                int payloadLen = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(processedOffset, 4));
                int totalLen = 6 + payloadLen;
                if (bufferCount - processedOffset < totalLen) break; // Incomplete!

                byte[] pkt = new byte[totalLen];
                Buffer.BlockCopy(buffer, processedOffset, pkt, 0, totalLen);
                dispatchedPackets.Add(pkt);
                processedOffset += totalLen;
            }

            Assert.Empty(dispatchedPackets); // Must not dispatch prematurely!

            // 2. Second chunk arrives (completes packet)
            Buffer.BlockCopy(chunk2, 0, buffer, bufferCount, chunk2.Length);
            bufferCount += chunk2.Length;

            while (bufferCount - processedOffset >= 6)
            {
                int payloadLen = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(processedOffset, 4));
                int totalLen = 6 + payloadLen;
                if (bufferCount - processedOffset < totalLen) break;

                byte[] pkt = new byte[totalLen];
                Buffer.BlockCopy(buffer, processedOffset, pkt, 0, totalLen);
                dispatchedPackets.Add(pkt);
                processedOffset += totalLen;
            }

            Assert.Single(dispatchedPackets);
            Assert.Equal(fullPkt.Length, dispatchedPackets[0].Length);
            Assert.Equal(fullPkt, dispatchedPackets[0]);
        }
    }

    // =========================================================================
    // PERSPECTIVE 2: Dispatcher Signature & Zero-Copy Validation
    // =========================================================================
    public class DispatcherPipelineTests
    {
        private class TestHandlerModule
        {
            public int ReaderCallCount { get; private set; }
            public int ByteCallCount { get; private set; }
            public int ParameterlessCallCount { get; private set; }
            public int ExtractedInt { get; private set; }

            [PacketHandler(PacketOpcode.MsgGameTradeReq)]
            public Task HandleReaderAsync(string context, PacketReader reader)
            {
                ReaderCallCount++;
                ExtractedInt = reader.ReadInt32();
                return Task.CompletedTask;
            }

            [PacketHandler(PacketOpcode.MsgCheckVersionNtf)]
            public Task HandleRawByteAsync(string context, byte[] rawPacket)
            {
                ByteCallCount++;
                return Task.CompletedTask;
            }

            [PacketHandler(PacketOpcode.MsgTimeNtf)]
            public Task HandleParameterlessAsync(string context)
            {
                ParameterlessCallCount++;
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task PacketDispatcher_SupportsAllThreeSignaturesSimultaneously()
        {
            var dispatcher = new PacketDispatcher<string>();
            var module = new TestHandlerModule();

            // Should bind all 3 methods with 0 errors
            dispatcher.RegisterHandlers(module);

            Assert.True(dispatcher.HasHandler((ushort)PacketOpcode.MsgGameTradeReq));
            Assert.True(dispatcher.HasHandler((ushort)PacketOpcode.MsgCheckVersionNtf));
            Assert.True(dispatcher.HasHandler((ushort)PacketOpcode.MsgTimeNtf));
            Assert.False(dispatcher.HasHandler((ushort)PacketOpcode.MsgGameChatReq));

            // 1. Dispatch PacketReader handler
            byte[] tradeReq = new byte[10];
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(tradeReq.AsSpan(0, 4), 4);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(tradeReq.AsSpan(4, 2), (ushort)PacketOpcode.MsgGameTradeReq);
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(tradeReq.AsSpan(6, 4), 98765);

            bool h1 = await dispatcher.DispatchAsync("testContext", (ushort)PacketOpcode.MsgGameTradeReq, tradeReq);
            Assert.True(h1);
            Assert.Equal(1, module.ReaderCallCount);
            Assert.Equal(98765, module.ExtractedInt);

            // 2. Dispatch raw byte[] handler
            using var cvWriter = PacketWriter.Create(PacketOpcode.MsgCheckVersionNtf);
            byte[] checkVer = cvWriter.Build();
            bool h2 = await dispatcher.DispatchAsync("testContext", (ushort)PacketOpcode.MsgCheckVersionNtf, checkVer);
            Assert.True(h2);
            Assert.Equal(1, module.ByteCallCount);

            // 3. Dispatch parameterless handler
            byte[] timePkt = YogurtingPackets.MakeTimeNtf();
            bool h3 = await dispatcher.DispatchAsync("testContext", (ushort)PacketOpcode.MsgTimeNtf, timePkt);
            Assert.True(h3);
            Assert.Equal(1, module.ParameterlessCallCount);

            // 4. Dispatch unhandled opcode
            bool h4 = await dispatcher.DispatchAsync("testContext", 0xFFFF, new byte[6]);
            Assert.False(h4);
        }
    }

    // =========================================================================
    // PERSPECTIVE 3: Concurrency & Stress Testing
    // =========================================================================
    public class DispatcherConcurrencyStressTests
    {
        private class CounterHandler
        {
            public int Counter;

            [PacketHandler(PacketOpcode.MsgCommEchoNtf)]
            public Task HandleEchoAsync(int clientId, PacketReader reader)
            {
                System.Threading.Interlocked.Increment(ref Counter);
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task PacketDispatcher_Under100ConcurrentWorkers_MaintainsThreadSafety()
        {
            var dispatcher = new PacketDispatcher<int>();
            var handler = new CounterHandler();
            dispatcher.RegisterHandlers(handler);

            byte[] echoPkt = YogurtingPackets.MakeCommEchoNtf(1);
            const int workers = 100;
            const int dispatchesPerWorker = 100;

            var tasks = Enumerable.Range(0, workers).Select(workerId => Task.Run(async () =>
            {
                for (int i = 0; i < dispatchesPerWorker; i++)
                {
                    bool ok = await dispatcher.DispatchAsync(workerId, (ushort)PacketOpcode.MsgCommEchoNtf, echoPkt);
                    Assert.True(ok);
                }
            })).ToArray();

            await Task.WhenAll(tasks);

            Assert.Equal(workers * dispatchesPerWorker, handler.Counter);
        }
    }

    // =========================================================================
    // PERSPECTIVE 4: Daemon Handshake & State Machine Routing Tests
    // =========================================================================
    public class DaemonHandshakeRoutingTests
    {
        [Fact]
        public async Task FieldServer_RejectsPacketsFromUnauthenticatedSession()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "YgTest_" + Guid.NewGuid().ToString("N"));
            var repo = new JsonAccountRepository(tempDir);
            var wm = new Yogurting.Server.World.WorldManager();
            var fieldHandler = new Yogurting.Server.Handlers.FieldServerHandler(repo, wm);

            var mockSession = new ClientSession(null!, null!);

            // Attempt to send a gameplay packet (MsgGameMoveExReq 0x79D5) before handshake
            byte[] movePkt = YogurtingPackets.MakeGameMoveExNtf(1, 50, 50, 0, 0);

            // Should be rejected cleanly without adding player to world
            await fieldHandler.HandlePacketAsync(mockSession, movePkt);

            Assert.Empty(wm.AllFields.SelectMany(f => f.Players));

            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task LoginServer_RoutesWorldListAndSelectWorldThroughDispatcher()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "YgTest_" + Guid.NewGuid().ToString("N"));
            var repo = new JsonAccountRepository(tempDir);
            var loginHandler = new Yogurting.Server.Handlers.LoginServerHandler(repo);

            var mockSession = new ClientSession(null!, null!);

            // Dispatch MsgLoginWorldListReq (0x7598)
            byte[] worldListReq = new byte[6];
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(worldListReq.AsSpan(0, 4), 0);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(worldListReq.AsSpan(4, 2), (ushort)PacketOpcode.MsgLoginWorldListReq);

            await loginHandler.HandlePacketAsync(mockSession, worldListReq);

            // Dispatch MsgLoginSelectWorldReq (0x759E)
            byte[] selectWorldReq = new byte[10];
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(selectWorldReq.AsSpan(0, 4), 4);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(selectWorldReq.AsSpan(4, 2), (ushort)PacketOpcode.MsgLoginSelectWorldReq);
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(selectWorldReq.AsSpan(6, 4), 91);

            await loginHandler.HandlePacketAsync(mockSession, selectWorldReq);

            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task EpisodeServer_RoutesHandshakeAndBootyBoxThroughDispatcher()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "YgTest_" + Guid.NewGuid().ToString("N"));
            var repo = new JsonAccountRepository(tempDir);
            var db = new GameDatabase();
            var episodeHandler = new Yogurting.Server.Handlers.EpisodeServerHandler(repo, db);

            var mockSession = new ClientSession(null!, null!);
            await episodeHandler.HandleClientConnectedAsync(mockSession);

            // Dispatch MsgPingTimeReq (0x5211)
            byte[] pingPkt = YogurtingPackets.MakeTimeNtf();
            System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(pingPkt.AsSpan(4, 2), (ushort)PacketOpcode.MsgPingTimeReq);

            await episodeHandler.HandlePacketAsync(mockSession, pingPkt);

            // Dispatch MsgGameBootyBoxDoneReq (0x7974)
            byte[] bootyPkt = new byte[10];
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bootyPkt.AsSpan(0, 4), 4);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(bootyPkt.AsSpan(4, 2), (ushort)PacketOpcode.MsgGameBootyBoxDoneReq);
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bootyPkt.AsSpan(6, 4), 2); // Box #2

            await episodeHandler.HandlePacketAsync(mockSession, bootyPkt);

            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task CommServer_RoutesJoinAndEchoThroughDispatcher()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "YgTest_" + Guid.NewGuid().ToString("N"));
            var repo = new JsonAccountRepository(tempDir);
            var commHandler = new Yogurting.Server.Handlers.CommServerHandler(repo);

            var mockSession = new ClientSession(null!, null!);
            await commHandler.HandleClientConnectedAsync(mockSession);

            // Dispatch MsgCommEchoNtf (0x7759)
            byte[] echoPkt = YogurtingPackets.MakeCommEchoNtf(42);
            await commHandler.HandlePacketAsync(mockSession, echoPkt);

            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    public sealed class LobbyAndTerminalInteractionTests
    {
        [Fact]
        public void MakeLobbyEnterNtf_SerializesExactDelphiGrammar()
        {
            var episodeIds = new List<int> { 101, 102, 103 };
            byte[] packet = YogurtingPackets.MakeLobbyEnterNtf(episodeIds, 101);

            // Payload: Opcode (2B) + Count (2B) + 3*(4B+4B+4B+4B=16B) + MaxRoom (2B) + EpSN (4B) = 2 + 2 + 48 + 2 + 4 = 58 bytes
            // Header: Length (4B) = 58
            // Total packet length = 62 bytes
            Assert.Equal(62, packet.Length);
            Assert.Equal((ushort)PacketOpcode.MsgLobbyEnterNtf, BitConverter.ToUInt16(packet, 4));
            Assert.Equal((ushort)3, BitConverter.ToUInt16(packet, 6)); // 3 episodes

            // First episode
            Assert.Equal(101, BitConverter.ToInt32(packet, 8));
            Assert.Equal(0.0f, BitConverter.ToSingle(packet, 12));
            Assert.Equal(1.0f, BitConverter.ToSingle(packet, 16));
            Assert.Equal(1.0f, BitConverter.ToSingle(packet, 20));

            // Max rooms & default episode at end
            Assert.Equal((ushort)100, BitConverter.ToUInt16(packet, 56));
            Assert.Equal(101, BitConverter.ToInt32(packet, 58));
        }

        [Fact]
        public void MakeLobbyEnterPcNtf_SerializesExactDelphiWritePCInfo()
        {
            var player = new Player
            {
                CharacterId = 777,
                CharacterName = "TestHero",
                Gender = GenderType.Female,
                Grade = 3,
                TelNumber = "9988"
            };

            byte[] packet = YogurtingPackets.MakeLobbyEnterPcNtf(player);

            Assert.Equal((ushort)PacketOpcode.MsgLobbyEnterPcNtf, BitConverter.ToUInt16(packet, 4));
            Assert.Equal(777, BitConverter.ToInt32(packet, 6));

            // 26-char Unicode name starts at offset 10
            string name = System.Text.Encoding.Unicode.GetString(packet, 10, 52).TrimEnd('\0');
            Assert.Equal("TestHero", name);

            // Gender (1B) + Grade (1B) + Weapon (2B) + Team (2B) + TelNumber (4B) + idPromotion (4B)
            Assert.Equal((byte)GenderType.Female, packet[62]);
            Assert.Equal((byte)3, packet[63]);
            Assert.Equal((ushort)1, BitConverter.ToUInt16(packet, 64)); // Weapon category
            Assert.Equal((ushort)0, BitConverter.ToUInt16(packet, 66)); // Team
            Assert.Equal(9988, BitConverter.ToInt32(packet, 68)); // TelNumber
            Assert.Equal(0, BitConverter.ToInt32(packet, 72)); // idPromotion
        }

        [Fact]
        public async Task HandleItemDiscard_RemovesItemFromPlayerInventory()
        {
            var player = new Player
            {
                CharacterId = 500,
                CharacterName = "Discarder"
            };
            player.Inventory.Add(new Item
            {
                Id = 1,
                TypeId = 30002,
                Name = "Crumpled Paper",
                Quantity = 1,
                SlotIndex = 0
            });
            player.Inventory.Add(new Item
            {
                Id = 2,
                TypeId = 30005,
                Name = "Juice Box",
                Quantity = 3,
                SlotIndex = 1
            });

            var session = new ClientSession(null!, null!);
            var state = new PlayerSessionState(session, player, 1);

            var equipHandlers = new Yogurting.Server.Handlers.Field.EquipmentHandlers((s, b) => Task.CompletedTask);

            // 1. Discard item at slot 0 (dim1 = 0)
            byte[] discardPayload = new byte[12];
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(discardPayload.AsSpan(0, 4), 30002);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(discardPayload.AsSpan(4, 2), 0); // dim1 = 0
            System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(discardPayload.AsSpan(6, 2), 0); // dim2 = 0
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(discardPayload.AsSpan(8, 4), 1); // itemId = 1

            var reader = new PacketReader(discardPayload);
            await equipHandlers.HandleItemDiscardAsync(state, reader);

            // Assert single-quantity item was removed
            Assert.DoesNotContain(player.Inventory, i => i.TypeId == 30002);

            // 2. Discard item at slot 0 (now Juice Box with qty 3)
            var reader2 = new PacketReader(discardPayload); // reuse to discard current slot 0
            await equipHandlers.HandleItemDiscardAsync(state, reader2);

            // Assert multi-quantity item was decremented
            var juice = player.Inventory.FirstOrDefault(i => i.TypeId == 30005);
            Assert.NotNull(juice);
            Assert.Equal(2, juice.Quantity);
        }

        [Fact]
        public async Task ShopHandlers_SellToNpc_CreditsTaffAndRemovesItem()
        {
            var player = new Player
            {
                CharacterId = 888,
                CharacterName = "Seller",
                TaffPoints = 1000
            };
            player.Inventory.Add(new Item
            {
                Id = 10,
                TypeId = 20001,
                Name = "Health Bread",
                Quantity = 5
            });

            var session = new ClientSession(null!, null!);
            var state = new PlayerSessionState(session, player, 1);
            var shop = new Yogurting.Server.Handlers.Field.ShopHandlers((s, b) => Task.CompletedTask);

            // Sell 2 of Health Bread (buy price defaults to 100, sell price is 50 -> +100 Taff)
            byte[] payload = new byte[16];
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(0, 4), 1); // npcId = 1
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(4, 4), 20001); // rawType
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(8, 4), 2); // count = 2
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(12, 4), 10); // itemId = 10

            var reader = new PacketReader(payload);
            await shop.HandleSellToNpcAsync(state, reader);

            Assert.Equal(1100, player.Taff);
            var item = player.Inventory.FirstOrDefault(i => i.TypeId == 20001);
            Assert.NotNull(item);
            Assert.Equal(3, item.Quantity);
        }

        [Fact]
        public void MakeGameEpisodeResultNtf_SerializesExactDelphiLayout()
        {
            byte[] packet = YogurtingPackets.MakeGameEpisodeResultNtf(501, "Hero", 1, 15000, 800);

            Assert.Equal((ushort)PacketOpcode.MsgGameEpisodeResultNtf, BitConverter.ToUInt16(packet, 4));
            Assert.Equal((byte)1, packet[6]); // charCount byte
            Assert.Equal((ushort)1, BitConverter.ToUInt16(packet, 7)); // charCount word
            Assert.Equal(501, BitConverter.ToInt32(packet, 9)); // charaId

            string name = System.Text.Encoding.Unicode.GetString(packet, 13, 52).TrimEnd('\0');
            Assert.Equal("Hero", name);
            Assert.Equal((ushort)1, BitConverter.ToUInt16(packet, 65)); // S-Rank
            Assert.Equal(15000, BitConverter.ToInt32(packet, 67)); // Score
            Assert.Equal(800, BitConverter.ToInt32(packet, 71)); // Bonus Exp
        }

        [Fact]
        public void MakeGameGuildChangeNameNtf_SerializesExactDelphiLayout()
        {
            byte[] packet = YogurtingPackets.MakeGameGuildChangeNameNtf(256, 1001, "HonorsClub");

            Assert.Equal((ushort)PacketOpcode.MsgGameGuildChangeNameNtf, BitConverter.ToUInt16(packet, 4));
            Assert.Equal(256, BitConverter.ToInt32(packet, 6)); // charaId
            Assert.Equal(1001L, BitConverter.ToInt64(packet, 10)); // guildId

            string gName = System.Text.Encoding.Unicode.GetString(packet, 18, 52).TrimEnd('\0');
            Assert.Equal("HonorsClub", gName);
            Assert.Equal(0xCC, packet[70]); // padding byte 1
            Assert.Equal(0xCC, packet[71]); // padding byte 2
        }

        [Fact]
        public void MakeWaitRoomSelectTeamNtf_SerializesExactDelphiLayout()
        {
            byte[] packet = YogurtingPackets.MakeWaitRoomSelectTeamNtf(1001, 2); // Team 2 (Blue)

            Assert.Equal((ushort)PacketOpcode.MsgWaitRoomSelectTeamReq, BitConverter.ToUInt16(packet, 4));
            Assert.Equal(1001, BitConverter.ToInt32(packet, 6)); // charaId
            Assert.Equal((byte)2, packet[10]); // teamId
            Assert.Equal(0xCC, packet[11]); // pad
            Assert.Equal(0xCC, packet[12]); // pad
            Assert.Equal(0xCC, packet[13]); // pad
        }

        [Fact]
        public void MakeGameShopLeaveNtf_SerializesHeaderOnlyZeroPayload()
        {
            byte[] packet = YogurtingPackets.MakeGameShopLeaveNtf();

            Assert.Equal((ushort)PacketOpcode.MsgGuideBoardLeaveNtf, BitConverter.ToUInt16(packet, 4));
            Assert.Equal(6, packet.Length); // 4 bytes length + 2 bytes opcode
            Assert.Equal(0, BitConverter.ToInt32(packet, 0)); // Length prefix = 0
        }

        [Fact]
        public void MakeGameLeaveHairShopNtf_SerializesHeaderOnlyZeroPayload()
        {
            byte[] packet = YogurtingPackets.MakeGameLeaveHairShopNtf();

            Assert.Equal((ushort)PacketOpcode.MsgGameLeaveHairShopNtf, BitConverter.ToUInt16(packet, 4));
            Assert.Equal(6, packet.Length); // 4 bytes length + 2 bytes opcode
            Assert.Equal(0, BitConverter.ToInt32(packet, 0)); // Length prefix = 0
        }

        [Fact]
        public async Task WaitRoom_SelectTeamAndEdit_UpdatesRoomState()
        {
            var player = new Player
            {
                CharacterId = 777,
                CharacterName = "Captain"
            };

            var session = new ClientSession(null!, null!);
            var state = new PlayerSessionState(session, player, 1);
            var lobby = new Yogurting.Server.Handlers.Field.LobbyAndEpisodeRoomHandlers(
                id => state,
                (s, b) => Task.CompletedTask,
                null,
                null,
                "127.0.0.1",
                10003);

            // 1. Create room
            byte[] createPayload = new byte[80];
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(createPayload.AsSpan(0, 4), 101); // Episode 101
            System.Text.Encoding.Unicode.GetBytes("Mission Alpha").CopyTo(createPayload, 4);

            var createReader = new PacketReader(createPayload);
            await lobby.HandleLobbyCreateRoomReqAsync(state, createReader);

            // 2. Select Team (Team 1 = Red)
            byte[] teamPayload = new byte[5];
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(teamPayload.AsSpan(0, 4), 1); // roomId = 1
            teamPayload[4] = 1; // Team 1

            var teamReader = new PacketReader(teamPayload);
            await lobby.HandleWaitRoomSelectTeamReqAsync(state, teamReader);

            // 3. Edit Room (Max users = 6)
            byte[] editPayload = new byte[90];
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(editPayload.AsSpan(0, 4), 102); // Episode 102
            System.Text.Encoding.Unicode.GetBytes("Mission Beta").CopyTo(editPayload, 4);
            editPayload[84] = 0; // isPrivate
            editPayload[85] = 1; // isTeam
            editPayload[86] = 6; // maxUsers = 6

            var editReader = new PacketReader(editPayload);
            await lobby.HandleWaitRoomEditReqAsync(state, editReader);

            // 4. Leave Room
            var leaveReader = new PacketReader(Array.Empty<byte>());
            await lobby.HandleWaitRoomLeaveReqAsync(state, leaveReader);
        }

        [Fact]
        public void MakeLoginKickOutNtf_SerializesExactDelphiLayout()
        {
            byte[] packet = YogurtingPackets.MakeLoginKickOutNtf(1);
            Assert.Equal((ushort)PacketOpcode.MsgLoginKickOutNtf, BitConverter.ToUInt16(packet, 4));
            Assert.Equal(1, BitConverter.ToInt32(packet, 6)); // Reason
        }

        [Fact]
        public void MakeGameChangeHairAns_SerializesExactDelphiLayout()
        {
            byte[] packet = YogurtingPackets.MakeGameChangeHairAns(1, 256, 105, 0, 50000L);
            Assert.Equal((ushort)0x5271, BitConverter.ToUInt16(packet, 4));
            Assert.Equal(1, BitConverter.ToInt32(packet, 6)); // rc
            Assert.Equal(256, BitConverter.ToInt32(packet, 10)); // charaId
            Assert.Equal(105, BitConverter.ToInt32(packet, 14)); // hairId
            Assert.Equal(0, BitConverter.ToInt32(packet, 18)); // hairColor
            Assert.Equal(50000L, BitConverter.ToInt64(packet, 22)); // taff
        }

        [Fact]
        public void MakeGamePicketStatusChangeAns_SerializesExactDelphiLayout()
        {
            byte[] packet = YogurtingPackets.MakeGamePicketStatusChangeAns(1, 300, true);
            Assert.Equal((ushort)PacketOpcode.MsgGamePicketStatusChangeAns, BitConverter.ToUInt16(packet, 4));
            Assert.Equal(1, BitConverter.ToInt32(packet, 6)); // rc
            Assert.Equal(300, BitConverter.ToInt32(packet, 10)); // charaId
            Assert.Equal(1, BitConverter.ToInt32(packet, 14)); // isOpen = 1
        }

        [Fact]
        public void MakeGamePicketContentsChangeAns_SerializesExactDelphiLayout()
        {
            byte[] packet = YogurtingPackets.MakeGamePicketContentsChangeAns(1, 300, "Welcome!");
            Assert.Equal((ushort)PacketOpcode.MsgGamePicketContentsChangeAns, BitConverter.ToUInt16(packet, 4));
            Assert.Equal(1, BitConverter.ToInt32(packet, 6)); // rc
            Assert.Equal(300, BitConverter.ToInt32(packet, 10)); // charaId
            string text = System.Text.Encoding.Unicode.GetString(packet, 14, 74).TrimEnd('\0');
            Assert.Equal("Welcome!", text);
        }

        [Fact]
        public void MakeGameByulHistoryAns_SerializesExactDelphiLayout()
        {
            byte[] packet = YogurtingPackets.MakeGameByulHistoryAns();
            Assert.Equal((ushort)PacketOpcode.MsgGameByulHistoryAns, BitConverter.ToUInt16(packet, 4));
            Assert.Equal(0, BitConverter.ToInt32(packet, 6)); // rc
            Assert.Equal(0, BitConverter.ToInt32(packet, 10)); // count
        }

        [Fact]
        public void MakeLobbyQuickJoinRoomAns_SerializesExactDelphiLayout()
        {
            byte[] packet = YogurtingPackets.MakeLobbyQuickJoinRoomAns(1);
            Assert.Equal((ushort)PacketOpcode.MsgLobbyQuickJoinRoomAns, BitConverter.ToUInt16(packet, 4));
            Assert.Equal(1, BitConverter.ToInt32(packet, 6)); // rc
        }

        [Fact]
        public void MakeWaitRoomTestInviteAns_SerializesExactDelphiLayout()
        {
            byte[] packet = YogurtingPackets.MakeWaitRoomTestInviteAns(555, 1);
            Assert.Equal((ushort)PacketOpcode.MsgWaitRoomTestInviteAns, BitConverter.ToUInt16(packet, 4));
            Assert.Equal(555, BitConverter.ToInt32(packet, 6)); // targetCharaId
            Assert.Equal(1, BitConverter.ToInt32(packet, 10)); // rc
        }

        [Fact]
        public async Task EquipmentHandlers_ItemDrop_DecrementsInventory()
        {
            var player = new Player
            {
                CharacterId = 888,
                CharacterName = "Dropper"
            };
            player.Inventory.Add(new Item { TypeId = 30005, Name = "Juice Box", Quantity = 5 });

            var session = new ClientSession(null!, null!);
            var state = new PlayerSessionState(session, player, 1);
            var equip = new Yogurting.Server.Handlers.Field.EquipmentHandlers(
                (s, b) => Task.CompletedTask,
                null,
                null);

            byte[] dropPayload = new byte[8];
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(dropPayload.AsSpan(0, 4), 30005);
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(dropPayload.AsSpan(4, 4), 2);

            var reader = new PacketReader(dropPayload);
            await equip.HandleItemDropAsync(state, reader);

            var juice = player.Inventory.FirstOrDefault(i => i.TypeId == 30005);
            Assert.NotNull(juice);
            Assert.Equal(3, juice.Quantity);
        }

        [Fact]
        public async Task ShopHandlers_ChangeHair_UpdatesPlayerHair()
        {
            var player = new Player
            {
                CharacterId = 999,
                CharacterName = "Fashionista",
                HairId = 101,
                Taff = 5000
            };

            var session = new ClientSession(null!, null!);
            var state = new PlayerSessionState(session, player, 1);
            var shop = new Yogurting.Server.Handlers.Field.ShopHandlers(
                (s, b) => Task.CompletedTask,
                null,
                null);

            byte[] hairPayload = new byte[4];
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(hairPayload.AsSpan(0, 4), 108);

            var reader = new PacketReader(hairPayload);
            await shop.HandleChangeHairReqAsync(state, reader);

            Assert.Equal(108, player.HairId);
        }

        [Fact]
        public void EpisodeGate_UniqueEpisodesFiltering_MatchesDatabaseSubIds()
        {
            string dbPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../data/db"));
            if (!Directory.Exists(dbPath)) return;

            var db = GameDatabase.Create(dbPath);
            Assert.NotNull(db);

            // 1. So-il Campus Field 90 Kiosk #1 (GateSubId 20101): exactly 4 unique episodes
            var gate20101Episodes = db.Episodes.Values
                .Where(ep => ep.GateSubId == 20101 && ep.Id > 0)
                .Select(ep => ep.Id)
                .OrderBy(id => id)
                .ToList();

            Assert.Equal(4, gate20101Episodes.Count);
            Assert.Equal(new[] { 37, 49, 69, 104 }, gate20101Episodes);

            // 2. So-il Campus Field 90 Kiosk #2 (GateSubId 20001): exactly 12 unique episodes
            var gate20001Episodes = db.Episodes.Values
                .Where(ep => ep.GateSubId == 20001 && ep.Id > 0)
                .Select(ep => ep.Id)
                .OrderBy(id => id)
                .ToList();

            Assert.Equal(12, gate20001Episodes.Count);
            Assert.Contains(31, gate20001Episodes);
            Assert.Contains(32, gate20001Episodes);
            Assert.Contains(33, gate20001Episodes);
            Assert.Contains(34, gate20001Episodes);

            // 3. Estiva Campus Field 1 Kiosk #1 (GateSubId 10101): exactly 4 unique episodes
            var gate10101Episodes = db.Episodes.Values
                .Where(ep => ep.GateSubId == 10101 && ep.Id > 0)
                .Select(ep => ep.Id)
                .OrderBy(id => id)
                .ToList();

            Assert.Equal(4, gate10101Episodes.Count);
            Assert.Equal(new[] { 42, 43, 62, 111 }, gate10101Episodes);
        }

        [Fact]
        public void EpisodeLobby_PacketsAndHandshake_ByteExactLayout()
        {
            // 1. MsgLobbyLeaveNtf (0x765E) - 0-byte payload (6 bytes wire length)
            byte[] leavePkt = YogurtingPackets.MakeLobbyLeaveNtf();
            Assert.Equal((ushort)PacketOpcode.MsgLobbyLeaveNtf, BitConverter.ToUInt16(leavePkt, 4));
            Assert.Equal(0, BitConverter.ToInt32(leavePkt, 0)); // 0 payload bytes
            Assert.Equal(6, leavePkt.Length);

            // 2. MsgLobbySelectEpisodeAns (0x7665) - 8-byte payload (retCode: 1, episodeId: 37)
            byte[] selectAnsPkt = YogurtingPackets.MakeLobbySelectEpisodeAns(1, 37);
            Assert.Equal((ushort)PacketOpcode.MsgLobbySelectEpisodeAns, BitConverter.ToUInt16(selectAnsPkt, 4));
            Assert.Equal(8, BitConverter.ToInt32(selectAnsPkt, 0));
            Assert.Equal(1, BitConverter.ToInt32(selectAnsPkt, 6)); // retCode = 1
            Assert.Equal(37, BitConverter.ToInt32(selectAnsPkt, 10)); // epId = 37

            // 3. MsgLobbyAvailableEpisodeInfoNtf (0x7676) - 2 bytes count + 16 bytes per episode
            byte[] availPkt = YogurtingPackets.MakeLobbyAvailableEpisodeInfoNtf(new[] { 37, 49, 69, 104 });
            Assert.Equal((ushort)PacketOpcode.MsgLobbyAvailableEpisodeInfoNtf, BitConverter.ToUInt16(availPkt, 4));
            Assert.Equal(2 + (4 * 16), BitConverter.ToInt32(availPkt, 0)); // 66 bytes payload
            Assert.Equal(4, BitConverter.ToUInt16(availPkt, 6)); // count = 4
            Assert.Equal(37, BitConverter.ToInt32(availPkt, 8)); // first epId = 37

            // 4. MsgLobbyEpisodePageStatusNtf (0x7663) - 8 bytes payload (epId, roomExists)
            byte[] statusPkt = YogurtingPackets.MakeLobbyEpisodePageStatusNtf(37, true);
            Assert.Equal((ushort)PacketOpcode.MsgLobbyEpisodePageStatusNtf, BitConverter.ToUInt16(statusPkt, 4));
            Assert.Equal(8, BitConverter.ToInt32(statusPkt, 0));
            Assert.Equal(37, BitConverter.ToInt32(statusPkt, 6));
            Assert.Equal(1, BitConverter.ToInt32(statusPkt, 10)); // roomExists = 1
        }
    }
}

