using System;
using System.Collections.Generic;

namespace Yogurting.Core.Models
{
    public enum SchoolType
    {
        EstivaAcademy = 1,
        SoilAcademy = 2
    }

    public enum GenderType
    {
        Male = 1,
        Female = 2
    }

    public enum WeaponClass
    {
        Blade = 1,
        Glove = 2,
        Muffler = 3,
        Shooting = 4
    }

    public enum ItemSlotType
    {
        Inventory = 0,
        Equipment = 1,
        Locker = 2,
        Consumable = 3
    }

    public struct Position
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Heading { get; set; }

        public Position(float x, float y, float z = 0f, float heading = 0f)
        {
            X = x;
            Y = y;
            Z = z;
            Heading = heading;
        }

        public override string ToString() => $"({X:F1}, {Y:F1}, {Z:F1})";
    }

    public sealed class Item
    {
        public int Id { get; set; }
        public int ItemId { get => TypeId; set => TypeId = value; }
        public int TypeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int SlotIndex { get; set; }
        public ItemSlotType SlotType { get; set; } = ItemSlotType.Inventory;
        public long SerialId { get; set; }
        public int Quantity { get; set; } = 1;
        public int EnchantLevel { get; set; }
        public bool IsEquipped { get; set; }
        public int[] SocketSlots { get; set; } = new int[5];
    }

    /// <summary>
    /// Represents a complete Yogurting Player Character.
    /// Exact 1-to-1 match with Quartet's XML Account & Character Schema.
    /// </summary>
    public sealed class Player
    {
        // Account & Profile
        public string AccountId { get; set; } = string.Empty;
        public string CharacterName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string TelNumber { get; set; } = "3456";
        public string AuthType { get; set; } = "none";
        public bool HasCharacter { get; set; } = true;

        // Session Identity (matches Delphi TChara.CharaID and TChara.SessionKey at offset 0x324)
        public int CharaId { get; set; } = 256;
        public int CharacterId { get => CharaId; set => CharaId = value; }
        public int SessionKey { get; set; } = 0;

        // Customization
        public SchoolType School { get; set; } = SchoolType.EstivaAcademy;
        public GenderType Gender { get; set; } = GenderType.Female;
        public int FaceId { get; set; } = 31;
        public int HairId { get; set; } = 103;
        public int SkinTone { get; set; } = 2000;
        public int BirthMonth { get; set; } = 5;
        public int BirthDay { get; set; } = 29;
        public int BloodType { get; set; } = 1;
        public int NametagColor { get; set; } = 0;

        // Progression & Stats
        public int Level { get; set; } = 1;
        public int Grade { get; set; } = 1;
        public int DexLevel { get; set; } = 1;
        public int DexExp { get; set; } = 0;
        public int[] DexLevels { get; set; } = new int[5] { 0, 1, 1, 1, 1 };
        public int[] DexExps { get; set; } = new int[5] { 0, 0, 0, 0, 0 };
        public int ComboCount { get; set; } = 0;
        public long LastAttackTime { get; set; } = 0;
        public byte ChargePoint { get; set; } = 0;
        public int Defense { get; set; } = 10;
        public long CurrentExp { get; set; } = 0;
        public long Exp { get => CurrentExp; set => CurrentExp = value; }
        public long MaxExp { get; set; } = 1000;
        public int CurrentHp { get; set; } = 260;
        public int Hp { get => CurrentHp; set => CurrentHp = value; }
        public int MaxHp { get; set; } = 260;
        public int CurrentMp { get; set; } = 195;
        public int Sp { get => CurrentMp; set => CurrentMp = value; }
        public int MaxMp { get; set; } = 195;
        public int MaxSp { get => MaxMp; set => MaxMp = value; }
        public long Money { get; set; } = 9999999;
        public int StarPoints { get; set; } = 9999999;
        public int TaffPoints { get => StarPoints; set => StarPoints = value; }

        // Coordinates & Zones (Defaults to Estiva Central Campus Courtyard / So-il Ground)
        public int FieldId { get; set; } = 1;
        public Position Position { get; set; } = new Position(76f, 104f, 0f);
        public int SaveFieldId { get; set; } = 1;
        public Position SavePosition { get; set; } = new Position(76f, 104f, 0f);

        // Inventories & Equipment
        public int[] EquippedSlotUids { get; set; } = new int[10] { 0, 0, 0, 0, 1, 0, 0, 2, 3, 4 };
        public bool[] EquippedSlotIsStar { get; set; } = new bool[10];
        public List<Item> Equips { get; set; } = new();
        public List<Item> Inventory { get; set; } = new();
        public List<Item> StarBeItems { get; set; } = new();
        public List<int> LearnedSkills { get; set; } = new();
        public List<int> CompletedEpisodes { get; set; } = new();
        public List<int> UnlockedTitles { get; set; } = new();
        public List<ActiveBuff> ActiveBuffs { get; set; } = new();

        public Player()
        {
        }

        public Player(string accountId, string characterName, SchoolType school = SchoolType.EstivaAcademy, GenderType gender = GenderType.Female)
        {
            AccountId = accountId;
            CharacterName = characterName;
            School = school;
            Gender = gender;
            FieldId = school == SchoolType.EstivaAcademy ? 1 : 90;
            Position = school == SchoolType.EstivaAcademy ? new Position(76f, 104f, 0f) : new Position(89f, 131f, 0f);
            SaveFieldId = school == SchoolType.EstivaAcademy ? 1 : 90;
            SavePosition = school == SchoolType.EstivaAcademy ? new Position(76f, 104f, 0f) : new Position(89f, 131f, 0f);
        }

        public bool HasActiveBuff(int effectType) => ActiveBuffs.Any(b => b.EffectType == effectType && b.RemainingSeconds > 0);

        public float GetExpMultiplier() => HasActiveBuff(7000) ? 1.5f : 1.0f;
        public float GetDamageMultiplier() => HasActiveBuff(7100) ? 1.2f : 1.0f;
        public float GetDefenseMultiplier() => HasActiveBuff(7200) ? 0.8f : 1.0f;
        public float GetHitMultiplier() => HasActiveBuff(7300) ? 1.15f : 1.0f;
        public float GetFleeMultiplier() => HasActiveBuff(7400) ? 1.15f : 1.0f;
        public float GetCritMultiplier() => HasActiveBuff(7500) ? 2.0f : 1.0f;
    }

    public class ActiveBuff
    {
        public int EffectType { get; set; }
        public int DurationSeconds { get; set; }
        public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;

        public int RemainingSeconds
        {
            get
            {
                int elapsed = (int)(DateTime.UtcNow - ActivatedAt).TotalSeconds;
                return Math.Max(0, DurationSeconds - elapsed);
            }
        }
    }
}
