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

        public static StarterConfig Instance
        {
            get
            {
                if (!_isLoaded)
                {
                    TryLoadDefault();
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
                    new() { SlotIndex = 4, ItemId = 120001, Name = "Starter Estiva Red Glove", Quantity = 1 },
                    new() { SlotIndex = 7, ItemId = 150001, Name = "Estiva Winter Top (M)", Quantity = 1 },
                    new() { SlotIndex = 8, ItemId = 150002, Name = "Estiva Winter Bottom (M)", Quantity = 1 },
                    new() { SlotIndex = 9, ItemId = 150003, Name = "Estiva Winter Shoes (M)", Quantity = 1 }
                },
                Inventory = new List<StarterItemEntry>
                {
                    new() { SlotIndex = 4, ItemId = 150007, Name = "Estiva Summer Top (M)", Quantity = 1 },
                    new() { SlotIndex = 5, ItemId = 150008, Name = "Estiva Summer Bottom (M)", Quantity = 1 },
                    new() { SlotIndex = 6, ItemId = 150009, Name = "Estiva Summer Shoes (M)", Quantity = 1 },
                    new() { SlotIndex = 7, ItemId = 200001, Name = "Beginner Bread", Quantity = 20 }
                }
            };

            // Estiva Female
            _instance.Profiles["Estiva_Female"] = new StarterProfile
            {
                Equipped = new List<StarterItemEntry>
                {
                    new() { SlotIndex = 4, ItemId = 140001, Name = "Starter Estiva Blue Glove", Quantity = 1 },
                    new() { SlotIndex = 7, ItemId = 150004, Name = "Estiva Winter Top (F)", Quantity = 1 },
                    new() { SlotIndex = 8, ItemId = 150005, Name = "Estiva Winter Bottom (F)", Quantity = 1 },
                    new() { SlotIndex = 9, ItemId = 150006, Name = "Estiva Winter Shoes (F)", Quantity = 1 }
                },
                Inventory = new List<StarterItemEntry>
                {
                    new() { SlotIndex = 4, ItemId = 150010, Name = "Estiva Summer Top (F)", Quantity = 1 },
                    new() { SlotIndex = 5, ItemId = 150011, Name = "Estiva Summer Bottom (F)", Quantity = 1 },
                    new() { SlotIndex = 6, ItemId = 150012, Name = "Estiva Summer Shoes (F)", Quantity = 1 },
                    new() { SlotIndex = 7, ItemId = 200001, Name = "Beginner Bread", Quantity = 20 }
                }
            };

            // Soil Male
            _instance.Profiles["Soil_Male"] = new StarterProfile
            {
                Equipped = new List<StarterItemEntry>
                {
                    new() { SlotIndex = 4, ItemId = 110001, Name = "Starter Blade", Quantity = 1 },
                    new() { SlotIndex = 7, ItemId = 150060, Name = "So-il Winter Top (M)", Quantity = 1 },
                    new() { SlotIndex = 8, ItemId = 150061, Name = "So-il Winter Bottom (M)", Quantity = 1 },
                    new() { SlotIndex = 9, ItemId = 150062, Name = "So-il Winter Shoes (M)", Quantity = 1 }
                },
                Inventory = new List<StarterItemEntry>
                {
                    new() { SlotIndex = 4, ItemId = 150066, Name = "So-il Summer Top (M)", Quantity = 1 },
                    new() { SlotIndex = 5, ItemId = 150067, Name = "So-il Summer Bottom (M)", Quantity = 1 },
                    new() { SlotIndex = 6, ItemId = 150068, Name = "So-il Summer Shoes (M)", Quantity = 1 },
                    new() { SlotIndex = 7, ItemId = 200001, Name = "Beginner Bread", Quantity = 20 }
                }
            };

            // Soil Female
            _instance.Profiles["Soil_Female"] = new StarterProfile
            {
                Equipped = new List<StarterItemEntry>
                {
                    new() { SlotIndex = 4, ItemId = 110001, Name = "Starter Blade", Quantity = 1 },
                    new() { SlotIndex = 7, ItemId = 150063, Name = "So-il Winter Top (F)", Quantity = 1 },
                    new() { SlotIndex = 8, ItemId = 150064, Name = "So-il Winter Bottom (F)", Quantity = 1 },
                    new() { SlotIndex = 9, ItemId = 150065, Name = "So-il Winter Shoes (F)", Quantity = 1 }
                },
                Inventory = new List<StarterItemEntry>
                {
                    new() { SlotIndex = 4, ItemId = 150069, Name = "So-il Summer Dress (F)", Quantity = 1 },
                    new() { SlotIndex = 5, ItemId = 150070, Name = "So-il Summer Shoes (F)", Quantity = 1 },
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

            var spawn = GetSpawnPoint(player.School);
            player.FieldId = spawn.FieldId;
            player.Position = new Position(spawn.X, spawn.Y, 0f);
            player.SaveFieldId = spawn.FieldId;
            player.SavePosition = player.Position;
        }
    }
}
