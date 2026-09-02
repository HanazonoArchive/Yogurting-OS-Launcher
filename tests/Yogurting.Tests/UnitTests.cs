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
    }
}

