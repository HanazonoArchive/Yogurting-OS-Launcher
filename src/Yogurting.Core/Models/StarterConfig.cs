using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Yogurting.Core.Models
{
    public class StarterConfig
    {
        public StarterDefaults Defaults { get; set; } = new();
        public Dictionary<string, StarterSpawnPoint> SpawnPoints { get; set; } = new();
        public Dictionary<string, StarterProfile> Profiles { get; set; } = new();
    }

    public class StarterDefaults
    {
        public int Level { get; set; } = 1;
        public int Grade { get; set; } = 1;
        public int CurrentHp { get; set; } = 260;
        public int MaxHp { get; set; } = 260;
        public int CurrentMp { get; set; } = 195;
        public int MaxMp { get; set; } = 195;
        public long TaffPoints { get; set; } = 100000;
        public int StarPoints { get; set; } = 100000;
    }

    public class StarterSpawnPoint
    {
        public int FieldId { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
    }

    public class StarterProfile
    {
        public List<StarterItemEntry> Equipped { get; set; } = new();
        public List<StarterItemEntry> Inventory { get; set; } = new();
    }

    public class StarterItemEntry
    {
        public ushort SlotIndex { get; set; }
        public int ItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
    }

    public static class StarterConfigLoader
    {
        private static StarterConfig _instance = new();
        private static bool _isLoaded;
        private static readonly object _lock = new();

        public static StarterConfig Instance
        {
            get
            {
                if (!_isLoaded)
                {
                    lock (_lock)
                    {
                        if (!_isLoaded)
                        {
                            TryLoadDefault();
                        }
                    }
                }
                return _instance;
            }
        }

        public static void Initialize(string configPath)
        {
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    var parsed = JsonSerializer.Deserialize<StarterConfig>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (parsed != null)
                    {
                        _instance = parsed;
                        _isLoaded = true;
                        Console.WriteLine($"[StarterConfig] Loaded starter items configuration from '{configPath}' ({_instance.Profiles.Count} profiles)");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[StarterConfig] Warning: Failed to parse '{configPath}': {ex.Message}. Using embedded defaults.");
                }
            }
            CreateDefaultFallback();
        }

        private static void TryLoadDefault()
        {
            string[] probePaths =
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "starter_items.json"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "config", "starter_items.json"),
                "config/starter_items.json"
            };

            foreach (var path in probePaths)
            {
                if (File.Exists(path))
                {
                    Initialize(path);
                    return;
                }
            }
            CreateDefaultFallback();
        }

        private static void CreateDefaultFallback()
        {
            _instance = new StarterConfig();
            _instance.SpawnPoints["EstivaAcademy"] = new StarterSpawnPoint { FieldId = 1, X = 76f, Y = 104f };
            _instance.SpawnPoints["SoilAcademy"] = new StarterSpawnPoint { FieldId = 90, X = 124f, Y = 165f };

            // Estiva Male
            _instance.Profiles["Estiva_Male"] = new StarterProfile
            {
                Equipped = new List<StarterItemEntry>
                {
                    new() { SlotIndex = 4, ItemId = 110001, Name = "スティンガー (Starter Blade)", Quantity = 1 },
                    new() { SlotIndex = 7, ItemId = 150001, Name = "冬服上着 (Estiva Winter Top M)", Quantity = 1 },
                    new() { SlotIndex = 8, ItemId = 150002, Name = "冬服ズボン (Estiva Winter Pants M)", Quantity = 1 },
                    new() { SlotIndex = 9, ItemId = 150003, Name = "冬用学生靴 (Estiva Winter Shoes M)", Quantity = 1 }
                },
                Inventory = new List<StarterItemEntry>
                {
                    new() { SlotIndex = 1, ItemId = 150007, Name = "夏服上着 (Estiva Summer Top M)", Quantity = 1 },
                    new() { SlotIndex = 2, ItemId = 150008, Name = "夏服ズボン (Estiva Summer Pants M)", Quantity = 1 },
                    new() { SlotIndex = 3, ItemId = 150009, Name = "夏用学生靴 (Estiva Summer Shoes M)", Quantity = 1 },
                    new() { SlotIndex = 4, ItemId = 120001, Name = "有備無患 (Starter Glove)", Quantity = 1 },
                    new() { SlotIndex = 5, ItemId = 130001, Name = "ドローン (Starter Myura/Blunt)", Quantity = 1 },
                    new() { SlotIndex = 6, ItemId = 140001, Name = "アクアマリン (Starter Spirit)", Quantity = 1 },
                    new() { SlotIndex = 7, ItemId = 200001, Name = "Beginner Bread", Quantity = 20 }
                }
            };

            // Estiva Female
            _instance.Profiles["Estiva_Female"] = new StarterProfile
            {
                Equipped = new List<StarterItemEntry>
                {
                    new() { SlotIndex = 4, ItemId = 110001, Name = "スティンガー (Starter Blade)", Quantity = 1 },
                    new() { SlotIndex = 7, ItemId = 150004, Name = "冬服上着 (Estiva Winter Top F)", Quantity = 1 },
                    new() { SlotIndex = 8, ItemId = 150005, Name = "冬服スカート (Estiva Winter Skirt F)", Quantity = 1 },
                    new() { SlotIndex = 9, ItemId = 150006, Name = "冬用ブーツ (Estiva Winter Boots F)", Quantity = 1 }
                },
                Inventory = new List<StarterItemEntry>
                {
                    new() { SlotIndex = 1, ItemId = 150010, Name = "夏服上着 (Estiva Summer Top F)", Quantity = 1 },
                    new() { SlotIndex = 2, ItemId = 150011, Name = "夏服スカート (Estiva Summer Skirt F)", Quantity = 1 },
                    new() { SlotIndex = 3, ItemId = 150012, Name = "夏用学生靴 (Estiva Summer Shoes F)", Quantity = 1 },
                    new() { SlotIndex = 4, ItemId = 120001, Name = "有備無患 (Starter Glove)", Quantity = 1 },
                    new() { SlotIndex = 5, ItemId = 130001, Name = "ドローン (Starter Myura/Blunt)", Quantity = 1 },
                    new() { SlotIndex = 6, ItemId = 140001, Name = "アクアマリン (Starter Spirit)", Quantity = 1 },
                    new() { SlotIndex = 7, ItemId = 200001, Name = "Beginner Bread", Quantity = 20 }
                }
            };

            // Soil Male
            _instance.Profiles["Soil_Male"] = new StarterProfile
            {
                Equipped = new List<StarterItemEntry>
                {
                    new() { SlotIndex = 4, ItemId = 110001, Name = "スティンガー (Starter Blade)", Quantity = 1 },
                    new() { SlotIndex = 7, ItemId = 150060, Name = "冬服上着 (So-il Winter Top M)", Quantity = 1 },
                    new() { SlotIndex = 8, ItemId = 150061, Name = "冬服ズボン (So-il Winter Pants M)", Quantity = 1 },
                    new() { SlotIndex = 9, ItemId = 150062, Name = "冬用学生靴 (So-il Winter Shoes M)", Quantity = 1 }
                },
                Inventory = new List<StarterItemEntry>
                {
                    new() { SlotIndex = 1, ItemId = 150066, Name = "夏服上着 (So-il Summer Top M)", Quantity = 1 },
                    new() { SlotIndex = 2, ItemId = 150067, Name = "夏服ズボン (So-il Summer Pants M)", Quantity = 1 },
                    new() { SlotIndex = 3, ItemId = 150068, Name = "夏用学生靴 (So-il Summer Shoes M)", Quantity = 1 },
                    new() { SlotIndex = 4, ItemId = 120001, Name = "有備無患 (Starter Glove)", Quantity = 1 },
                    new() { SlotIndex = 5, ItemId = 130001, Name = "ドローン (Starter Myura/Blunt)", Quantity = 1 },
                    new() { SlotIndex = 6, ItemId = 140001, Name = "アクアマリン (Starter Spirit)", Quantity = 1 },
                    new() { SlotIndex = 7, ItemId = 200001, Name = "Beginner Bread", Quantity = 20 }
                }
            };

            // Soil Female
            _instance.Profiles["Soil_Female"] = new StarterProfile
            {
                Equipped = new List<StarterItemEntry>
                {
                    new() { SlotIndex = 4, ItemId = 110001, Name = "スティンガー (Starter Blade)", Quantity = 1 },
                    new() { SlotIndex = 7, ItemId = 150063, Name = "冬服上着 (So-il Winter Top F)", Quantity = 1 },
                    new() { SlotIndex = 8, ItemId = 150064, Name = "冬服スカート (So-il Winter Skirt F)", Quantity = 1 },
                    new() { SlotIndex = 9, ItemId = 150065, Name = "冬用学生靴 (So-il Winter Shoes F)", Quantity = 1 }
                },
                Inventory = new List<StarterItemEntry>
                {
                    new() { SlotIndex = 1, ItemId = 150069, Name = "夏服 (So-il Summer Dress F)", Quantity = 1 },
                    new() { SlotIndex = 2, ItemId = 150070, Name = "夏用学生靴 (So-il Summer Shoes F)", Quantity = 1 },
                    new() { SlotIndex = 3, ItemId = 120001, Name = "有備無患 (Starter Glove)", Quantity = 1 },
                    new() { SlotIndex = 4, ItemId = 130001, Name = "ドローン (Starter Myura/Blunt)", Quantity = 1 },
                    new() { SlotIndex = 5, ItemId = 140001, Name = "アクアマリン (Starter Spirit)", Quantity = 1 },
                    new() { SlotIndex = 6, ItemId = 200001, Name = "Beginner Bread", Quantity = 20 }
                }
            };
            _isLoaded = true;
        }

        public static string GetProfileKey(SchoolType school, GenderType gender)
        {
            string schoolPrefix = school == SchoolType.EstivaAcademy ? "Estiva" : "Soil";
            string genderSuffix = gender == GenderType.Male ? "Male" : "Female";
            return $"{schoolPrefix}_{genderSuffix}";
        }

        public static StarterProfile GetProfile(SchoolType school, GenderType gender)
        {
            string key = GetProfileKey(school, gender);
            if (Instance.Profiles.TryGetValue(key, out var profile))
            {
                return profile;
            }
            return Instance.Profiles["Estiva_Female"];
        }

        public static StarterSpawnPoint GetSpawnPoint(SchoolType school)
        {
            string key = school == SchoolType.SoilAcademy ? "SoilAcademy" : "EstivaAcademy";
            if (Instance.SpawnPoints.TryGetValue(key, out var spawn))
            {
                return spawn;
            }
            return new StarterSpawnPoint { FieldId = 1, X = 76f, Y = 104f };
        }

        public static void ApplyToPlayer(Player player)
        {
            player.Inventory ??= new List<Item>();
            player.Inventory.Clear();
            player.Equips ??= new List<Item>();
            player.Equips.Clear();
            player.EquippedSlotUids = new int[10];

            var profile = GetProfile(player.School, player.Gender);
            int runningUid = 1;

            // 1. Populate Equipped Items
            foreach (var eq in profile.Equipped)
            {
                int uid = runningUid++;
                var item = new Item
                {
                    Id = uid,
                    ItemId = eq.ItemId,
                    TypeId = eq.ItemId,
                    Name = eq.Name,
                    SlotIndex = eq.SlotIndex,
                    SlotType = ItemSlotType.Equipment,
                    IsEquipped = true,
                    Quantity = eq.Quantity > 0 ? eq.Quantity : 1,
                    SocketSlots = new int[5]
                };

                player.Inventory.Add(item);
                player.Equips.Add(item);
                if (eq.SlotIndex < player.EquippedSlotUids.Length)
                {
                    player.EquippedSlotUids[eq.SlotIndex] = uid;
                }
            }

            // 2. Populate In-Bag Items
            foreach (var inv in profile.Inventory)
            {
                int uid = runningUid++;
                var item = new Item
                {
                    Id = uid,
                    ItemId = inv.ItemId,
                    TypeId = inv.ItemId,
                    Name = inv.Name,
                    SlotIndex = inv.SlotIndex,
                    SlotType = (inv.ItemId >= 200000 && inv.ItemId < 300000) ? ItemSlotType.Consumable : ItemSlotType.Inventory,
                    IsEquipped = false,
                    Quantity = inv.Quantity > 0 ? inv.Quantity : 1,
                    SocketSlots = new int[5]
                };

                player.Inventory.Add(item);
            }
        }

        public static void ApplyDefaultStats(Player player)
        {
            var def = Instance.Defaults;
            player.Level = def.Level;
            player.Grade = def.Grade;
            player.CurrentHp = def.CurrentHp;
            player.MaxHp = def.MaxHp;
            player.CurrentMp = def.CurrentMp;
            player.MaxMp = def.MaxMp;
            player.TaffPoints = def.TaffPoints;
            player.StarPoints = def.StarPoints;
            player.CurrentExp = 0;
            player.GaugeCurrent = 0;
            player.ChargePoint = 0;
            player.SkillPoint = 0;

            var spawn = GetSpawnPoint(player.School);
            player.FieldId = spawn.FieldId;
            player.Position = new Position(spawn.X, spawn.Y, 0f);
            player.SaveFieldId = spawn.FieldId;
            player.SavePosition = player.Position;
        }
    }
}