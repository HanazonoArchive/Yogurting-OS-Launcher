using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Yogurting.Core.Models;

namespace Yogurting.Data.Loaders
{
    /// <summary>
    /// Modern JSON-driven Game Database Engine.
    /// Loads all 30 game database parameter tables, spatial collision grids (map.db),
    /// NPC dialogue script trees, and episode XML score metadata.
    /// </summary>
    public class GameDatabase
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public ConcurrentDictionary<int, GameItemDef> Items { get; } = new();
        public ConcurrentDictionary<int, GameEpisodeDef> Episodes { get; } = new();
        public ConcurrentDictionary<int, GameSkillDef> Skills { get; } = new();
        public ConcurrentDictionary<int, string> Titles { get; } = new();
        public ConcurrentDictionary<int, string> Npcs { get; } = new();
        public ConcurrentDictionary<int, int> NpcCutIns { get; } = new();
        public ConcurrentDictionary<int, NpcScriptDef> NpcScripts { get; } = new();
        public ConcurrentDictionary<int, ReinforceStoneDef> ReinforceStones { get; } = new();
        public ConcurrentDictionary<int, GameFieldDef> Fields { get; } = new();
        public ConcurrentDictionary<int, AtkWeaponDef> AtkWeapons { get; } = new();
        public ConcurrentDictionary<int, SkillDescDef> SkillDescs { get; } = new();
        public ConcurrentDictionary<int, SkillDesc2Def> SkillDesc2s { get; } = new();
        public ConcurrentDictionary<int, SkillWeaponDef> SkillWeapons { get; } = new();
        public ConcurrentDictionary<int, HuntMonsterDef> HuntMonsters { get; } = new();
        public ConcurrentDictionary<int, string> MatchingBgm { get; } = new();
        public List<string> KujiResults { get; } = new();
        public List<ShopProductDef> StarProducts { get; } = new();
        public List<ShopItemDef> ShopItems { get; } = new();
        public Dictionary<int, int> ExpTable { get; } = new();
        public Dictionary<int, StatusDef> StatusTable { get; } = new();
        public Dictionary<int, int> DexTable { get; } = new();

        public int GetMaxExpForLevel(int level) => ExpTable.TryGetValue(level, out int exp) ? exp : (level * 25 + 10);
        public StatusDef GetStatusForLevel(int level) => StatusTable.TryGetValue(level, out var stat) ? stat : new StatusDef { Level = level, Pow = level * 4, Speed = level * 3, Skill = level * 3, Luck = level * 2 };
        public int GetRequiredDexForLevel(int dexLevel) => DexTable.TryGetValue(dexLevel, out int dex) ? dex : (dexLevel * 10 + 10);

        public string GetEpisodeTitleForProgress(int school, int progress)
        {
            int targetId = school == 1 ? (1000 + progress) : (2000 + progress);
            if (Episodes.TryGetValue(targetId, out var epi) && !string.IsNullOrEmpty(epi.Title))
            {
                return epi.Title;
            }
            if (Episodes.TryGetValue(progress, out var directEpi) && !string.IsNullOrEmpty(directEpi.Title))
            {
                return directEpi.Title;
            }
            return "クリア";
        }

        public int GetFieldBgm(int fieldId)
        {
            if (Fields.TryGetValue(fieldId, out var field) && field.Bgm > 0)
            {
                return field.Bgm;
            }
            return 6;
        }

        public static GameDatabase Create(string rootDataDir, string preferredFormat = "Json")
        {
            string dbDir = Path.Combine(rootDataDir, "db");
            if (!Directory.Exists(dbDir))
            {
                dbDir = rootDataDir;
            }

            Console.WriteLine("[GameDatabase] Initializing JSON Database Engine...");
            var db = new GameDatabase();
            db.LoadAll(dbDir);
            return db;
        }

        public virtual void LoadAll(string dbDir)
        {
            try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); } catch { }
            Console.WriteLine("[GameDatabase] Loading parameter tables from JSON dataset...");

            LoadExpTableJson(Path.Combine(dbDir, "ExpTable.json"));
            LoadStatusTableJson(Path.Combine(dbDir, "StatusTable.json"));
            LoadDexTableJson(Path.Combine(dbDir, "DexTable.json"));
            LoadBeItemsJson(Path.Combine(dbDir, "BeItemType.json"));
            LoadCoItemsJson(Path.Combine(dbDir, "CoItemType.json"));
            LoadByulBeItemsJson(Path.Combine(dbDir, "ByulItemBeType.json"));
            LoadByulItemsJson(Path.Combine(dbDir, "ByulItemType.json"));
            LoadEnItemsJson(Path.Combine(dbDir, "EnItemType.json"));
            LoadReinforceStonesJson(Path.Combine(dbDir, "ReinforceStone.json"));
            LoadEpisodesJson(Path.Combine(dbDir, "Episode.json"));
            LoadTitlesJson(Path.Combine(dbDir, "Title.json"));
            LoadNpcsJson(Path.Combine(dbDir, "NpcEx.json"));
            LoadShopItemListJson(Path.Combine(dbDir, "ShopItemList.json"));
            LoadHuntMonstersJson(Path.Combine(dbDir, "HuntMon.json"));
            LoadFieldsJson(Path.Combine(dbDir, "Field.json"));

            string mapDbPath = Path.Combine(dbDir, "map.db");
            if (!File.Exists(mapDbPath))
            {
                mapDbPath = Path.Combine(dbDir, "..", "db", "map.db");
            }
            MapGridManager.Initialize(mapDbPath, Fields);

            LoadAtkWeaponsJson(Path.Combine(dbDir, "AtkWeapon.json"));
            LoadSkillWeaponsJson(Path.Combine(dbDir, "SkillWeapon.json"));
            LoadSkillDescsJson(Path.Combine(dbDir, "SkillDesc.json"));
            LoadSkillDesc2sJson(Path.Combine(dbDir, "SkillDesc2.json"));
            LoadMatchingBgmJson(Path.Combine(dbDir, "MatchingBGM.json"));
            LoadKujiTableJson(Path.Combine(dbDir, "kuji.json"));

            string scoreDir = Path.Combine(dbDir, "..", "score");
            if (!Directory.Exists(scoreDir))
            {
                scoreDir = Path.Combine(AppContext.BaseDirectory, "data", "score");
            }
            if (Directory.Exists(scoreDir))
            {
                LoadFieldScoreData(scoreDir);
            }

            string productListXml = Path.Combine(dbDir, "..", "score", "ProductList.xml");
            if (!File.Exists(productListXml))
            {
                productListXml = Path.Combine(AppContext.BaseDirectory, "data", "score", "ProductList.xml");
            }
            LoadProductListXml(productListXml);

            string specialPhoneXml = Path.Combine(scoreDir, "SpecialPhone.xml");
            if (File.Exists(specialPhoneXml))
            {
                try
                {
                    string text = File.ReadAllText(specialPhoneXml, Encoding.GetEncoding("Shift_JIS"));
                    ParseNpcDialogsFromXml(text);
                }
                catch { }
            }

            string libDir = Path.Combine(scoreDir, "lib");
            if (Directory.Exists(libDir))
            {
                try
                {
                    foreach (var libFile in Directory.GetFiles(libDir, "*.xml"))
                    {
                        string text = File.ReadAllText(libFile, Encoding.GetEncoding("Shift_JIS"));
                        ParseNpcDialogsFromXml(text);
                    }
                }
                catch { }
            }

            // Link ByulItemType (Cash/Star) to ByulItemBeType (Base equipment properties)
            foreach (var item in Items.Values)
            {
                if (item.BaseItemType > 0 && Items.TryGetValue(item.BaseItemType, out var baseItem))
                {
                    if (item.EquipPos == 0) item.EquipPos = baseItem.EquipPos;
                    if (item.WeaponType == 0) item.WeaponType = baseItem.WeaponType;
                    if (item.AttackGroup == 0) item.AttackGroup = baseItem.AttackGroup;
                    if (item.Attack == 0) item.Attack = baseItem.Attack;
                    if (item.SkillId == 0) item.SkillId = baseItem.SkillId;
                }
            }

            Console.WriteLine($"[GameDatabase] Loaded {Items.Count} Items, {ReinforceStones.Count} Reinforce Stones, {Episodes.Count} Episodes, {Titles.Count} Titles, {Npcs.Count} NPCs, {ShopItems.Count} Shop Items, {StarProducts.Count} Star Products, {Fields.Count} Fields!");
        }

        #region JSON Table Loaders

        private void LoadExpTableJson(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                string json = File.ReadAllText(filePath);
                var list = JsonSerializer.Deserialize<List<JsonExpEntry>>(json, JsonOptions);
                if (list != null)
                {
                    foreach (var entry in list)
                    {
                        if (entry.Level > 0)
                        {
                            ExpTable[entry.Level] = entry.RequiredExp;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDatabase] Error loading ExpTable.json: {ex.Message}");
            }
        }

        private void LoadStatusTableJson(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                string json = File.ReadAllText(filePath);
                var list = JsonSerializer.Deserialize<List<JsonStatusEntry>>(json, JsonOptions);
                if (list != null)
                {
                    foreach (var entry in list)
                    {
                        if (entry.Level > 0)
                        {
                            StatusTable[entry.Level] = new StatusDef
                            {
                                Level = entry.Level,
                                Pow = entry.Power,
                                Speed = entry.Speed,
                                Skill = entry.Skill,
                                Luck = entry.Luck
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDatabase] Error loading StatusTable.json: {ex.Message}");
            }
        }

        private void LoadDexTableJson(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                string json = File.ReadAllText(filePath);
                var list = JsonSerializer.Deserialize<List<JsonDexEntry>>(json, JsonOptions);
                if (list != null)
                {
                    foreach (var entry in list)
                    {
                        if (entry.DexLevel > 0)
                        {
                            DexTable[entry.DexLevel] = entry.RequiredExp;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDatabase] Error loading DexTable.json: {ex.Message}");
            }
        }

        private void LoadBeItemsJson(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                string json = File.ReadAllText(filePath);
                var list = JsonSerializer.Deserialize<List<JsonBeItemEntry>>(json, JsonOptions);
                if (list != null)
                {
                    foreach (var row in list)
                    {
                        if (row.Id <= 0) continue;
                        var item = new GameItemDef
                        {
                            Id = row.Id,
                            Code = row.Code ?? string.Empty,
                            Name = row.Name ?? string.Empty,
                            Description = row.Description ?? string.Empty,
                            Attr = row.ItemType,
                            Flag = row.Flag,
                            EquipPos = row.EquipPos,
                            WeaponType = row.WeaponType,
                            Sex = row.Sex,
                            School = row.School,
                            AttackGroup = row.AttackGroup,
                            Attack = 0,
                            SkillId = row.SkillId,
                            GradeReq = row.LevelReq,
                            Price = row.BeItemSlot
                        };
                        Items[row.Id] = item;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDatabase] Error loading BeItemType.json: {ex.Message}");
            }
        }

        private void LoadCoItemsJson(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                string json = File.ReadAllText(filePath);
                var list = JsonSerializer.Deserialize<List<JsonCoItemEntry>>(json, JsonOptions);
                if (list != null)
                {
                    foreach (var row in list)
                    {
                        if (row.Id <= 0) continue;
                        if (!Items.TryGetValue(row.Id, out var item))
                        {
                            item = new GameItemDef { Id = row.Id };
                            Items[row.Id] = item;
                        }

                        item.Code = row.Code ?? item.Code;
                        item.Name = row.Name ?? item.Name;
                        item.Description = row.Description ?? item.Description;
                        if (row.Attr > 0) item.Attr = row.Attr;
                        item.UseType = row.UseType;
                        item.QuickUsable = row.Quick || row.QuickUsable;
                        item.Price = row.Price;

                        if (!string.IsNullOrEmpty(item.Description))
                        {
                            var match = Regex.Match(item.Description, @"HP.*?(\d+)");
                            if (match.Success && int.TryParse(match.Groups[1].Value, out int rec))
                            {
                                item.RecoveryAmount = rec;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDatabase] Error loading CoItemType.json: {ex.Message}");
            }
        }

        private void LoadByulBeItemsJson(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                string json = File.ReadAllText(filePath);
                var list = JsonSerializer.Deserialize<List<JsonByulBeItemEntry>>(json, JsonOptions);
                if (list != null)
                {
                    foreach (var row in list)
                    {
                        if (row.Id <= 0) continue;
                        if (!Items.TryGetValue(row.Id, out var item))
                        {
                            item = new GameItemDef { Id = row.Id };
                            Items[row.Id] = item;
                        }

                        item.Code = row.Code ?? item.Code;
                        item.Name = row.Name ?? item.Name;
                        item.Description = row.Description ?? item.Description;
                        if (row.Attr > 0) item.Attr = row.Attr;
                        item.EquipPos = row.EquipPos;
                        item.WeaponType = row.WeaponType;
                        item.Sex = row.Sex;
                        item.School = row.School;
                        item.AttackGroup = row.AttackGroup > 0 ? row.AttackGroup : item.AttackGroup;
                        item.SkillId = row.SkillId;
                        item.GradeReq = row.LevelReq > 0 ? row.LevelReq : (row.GradeReq > 0 ? row.GradeReq : item.GradeReq);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDatabase] Error loading ByulItemBeType.json: {ex.Message}");
            }
        }

        private void LoadByulItemsJson(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                string json = File.ReadAllText(filePath);
                var list = JsonSerializer.Deserialize<List<JsonByulItemEntry>>(json, JsonOptions);
                if (list != null)
                {
                    foreach (var row in list)
                    {
                        if (row.Id <= 0) continue;
                        if (!Items.TryGetValue(row.Id, out var item))
                        {
                            item = new GameItemDef { Id = row.Id };
                            Items[row.Id] = item;
                        }

                        item.Code = row.Code ?? item.Code;
                        item.Name = row.Name ?? item.Name;
                        item.Description = row.Description ?? item.Description;
                        item.DurationDays = row.DurationDays;
                        item.RecoveryAmount = row.BeType;
                        item.BaseItemType = row.BeType;
                        item.Sex = row.Sex;
                        item.School = row.School;
                        item.EffectId = row.Effect;
                        if (item.EffectId > 0)
                        {
                            item.SkillId = item.EffectId;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDatabase] Error loading ByulItemType.json: {ex.Message}");
            }
        }

        private void LoadEnItemsJson(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                string json = File.ReadAllText(filePath);
                var list = JsonSerializer.Deserialize<List<JsonEnItemEntry>>(json, JsonOptions);
                if (list != null)
                {
                    foreach (var row in list)
                    {
                        if (row.Id <= 0) continue;
                        if (!Items.TryGetValue(row.Id, out var item))
                        {
                            item = new GameItemDef { Id = row.Id };
                            Items[row.Id] = item;
                        }

                        item.Code = row.Code ?? item.Code;
                        item.Name = row.Name ?? item.Name;
                        item.Description = row.Description ?? item.Description;
                        item.Attr = row.Element;
                        item.UseType = row.UseType;
                        item.GradeReq = row.Grade;
                        item.Price = row.Price;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDatabase] Error loading EnItemType.json: {ex.Message}");
            }
        }

        private void LoadReinforceStonesJson(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                string json = File.ReadAllText(filePath);
                var list = JsonSerializer.Deserialize<List<JsonReinforceStoneEntry>>(json, JsonOptions);
                if (list != null)
                {
                    foreach (var row in list)
                    {
                        if (row.CoItemTypeId <= 0) continue;
                        ReinforceStones[row.CoItemTypeId] = new ReinforceStoneDef
                        {
                            Id = row.CoItemTypeId,
                            Grade = row.Grade,
                            Level = row.Level,
                            Hp = row.Hp,
                            Atk = row.Atk,
                            Def = row.Def,
                            ExpandGauge = row.ExpandGauge,
                            WeaponType = row.WeaponType,
                            EquipPos = row.EquipPos
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDatabase] Error loading ReinforceStone.json: {ex.Message}");
            }
        }

        private void LoadEpisodesJson(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                string json = File.ReadAllText(filePath);
                var list = JsonSerializer.Deserialize<List<JsonEpisodeEntry>>(json, JsonOptions);
                if (list != null)
                {
                    foreach (var row in list)
                    {
                        Episodes[row.Id] = new GameEpisodeDef
                        {
                            Id = row.Id,
                            Title = row.Title ?? string.Empty,
                            CleanTitle = row.CleanTitle ?? string.Empty,
                            School = row.School,
                            GradeReq = row.GradeReq,
                            GateSubId = row.GateSubId
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDatabase] Error loading Episode.json: {ex.Message}");
            }
        }

        private void LoadTitlesJson(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                string json = File.ReadAllText(filePath);
                var list = JsonSerializer.Deserialize<List<JsonTitleEntry>>(json, JsonOptions);
                if (list != null)
                {
                    foreach (var row in list)
                    {
                        if (row.Id > 0 && !string.IsNullOrEmpty(row.Name))
                        {
                            Titles[row.Id] = row.Name;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDatabase] Error loading Title.json: {ex.Message}");
            }
        }

        private void LoadNpcsJson(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                string json = File.ReadAllText(filePath);
                var list = JsonSerializer.Deserialize<List<JsonNpcEntry>>(json, JsonOptions);
                if (list != null)
                {
                    foreach (var row in list)
                    {
                        if (row.Id > 0)
                        {
                            Npcs[row.Id] = row.Name ?? string.Empty;
                            NpcCutIns[row.Id] = row.PortraitCutinId;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDatabase] Error loading NpcEx.json: {ex.Message}");
            }
        }

        private void LoadShopItemListJson(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                string json = File.ReadAllText(filePath);
                var list = JsonSerializer.Deserialize<List<JsonShopItemEntry>>(json, JsonOptions);
                if (list != null)
                {
                    foreach (var row in list)
                    {
                        if (row.ItemId <= 0) continue;
                        ShopItems.Add(new ShopItemDef
                        {
                            ItemId = row.ItemId,
                            Price = row.Price,
                            Grade = row.Grade,
                            Category = row.Category
                        });

                        StarProducts.Add(new ShopProductDef
                        {
                            ProductId = row.ItemId,
                            Price = row.Price,
                            Period = row.Grade,
                            Flag = row.Category
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDatabase] Error loading ShopItemList.json: {ex.Message}");
            }
        }

        private void LoadHuntMonstersJson(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                string json = File.ReadAllText(filePath);
                var list = JsonSerializer.Deserialize<List<JsonHuntMonEntry>>(json, JsonOptions);
                if (list != null)
                {
                    foreach (var row in list)
                    {
                        if (row.Id <= 0) continue;
                        HuntMonsters[row.Id] = new HuntMonsterDef
                        {
                            IdHuntField = row.HuntFieldId,
                            TypeMonster = row.Id,
                            HpMax = row.MaxHp,
                            Level = row.Level,
                            TypeBasis = row.BasisMobId,
                            Name = row.Name ?? string.Empty,
                            Motion = row.Motion,
                            DropItemType = row.DropItemId,
                            DropCount = row.DropCount,
                            DropRate = row.DropRate,
                            Exp = row.ExpReward
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDatabase] Error loading HuntMon.json: {ex.Message}");
            }
        }

        private void LoadFieldsJson(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                string json = File.ReadAllText(filePath);
                var list = JsonSerializer.Deserialize<List<JsonFieldEntry>>(json, JsonOptions);
                if (list != null)
                {
                    foreach (var row in list)
                    {
                        if (row.Id <= 0) continue;
                        Fields[row.Id] = new GameFieldDef
                        {
                            Id = row.Id,
                            Code = row.Code ?? string.Empty,
                            Name = row.Name ?? string.Empty,
                            Bgm = row.BgmId,
                            IsHuntField = row.IsHuntField,
                            HuntFieldId = row.HuntFieldId,
                            IsEpisode = !row.IsHuntField && (row.Id >= 1000 && row.Id <= 9999)
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDatabase] Error loading Field.json: {ex.Message}");
            }
        }

        private void LoadAtkWeaponsJson(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                string json = File.ReadAllText(filePath);
                var list = JsonSerializer.Deserialize<List<JsonAtkWeaponEntry>>(json, JsonOptions);
                if (list != null)
                {
                    foreach (var row in list)
                    {
                        if (row.Id <= 0) continue;
                        AtkWeapons[row.Id] = new AtkWeaponDef
                        {
                            ItemType = row.Id,
                            Category = row.Range,
                            AtkRatio = row.Power, // Power from AtkWeapon.json (Column 5) - Delphi TAtkWeaponData.AtkRatio
                            HitMotion = row.Param1,
                            Range = row.Param2,
                            Angle = row.Area
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDatabase] Error loading AtkWeapon.json: {ex.Message}");
            }
        }

        private void LoadSkillWeaponsJson(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                string json = File.ReadAllText(filePath);
                var list = JsonSerializer.Deserialize<List<JsonSkillWeaponEntry>>(json, JsonOptions);
                if (list != null)
                {
                    foreach (var row in list)
                    {
                        if (row.Id <= 0) continue;
                        SkillWeapons[row.Id] = new SkillWeaponDef
                        {
                            SkillId = row.Id,
                            Code = row.Code ?? string.Empty,
                            Name = row.Name ?? string.Empty,
                            Range = (int)row.RangeMax,
                            Delay = row.DelayMs
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDatabase] Error loading SkillWeapon.json: {ex.Message}");
            }
        }

        private void LoadSkillDescsJson(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                string json = File.ReadAllText(filePath);
                var list = JsonSerializer.Deserialize<List<JsonSkillDescEntry>>(json, JsonOptions);
                if (list != null)
                {
                    foreach (var row in list)
                    {
                        if (row.Id <= 0) continue;
                        SkillDescs[row.Id] = new SkillDescDef
                        {
                            SkillId = row.Id,
                            Code = row.Code ?? string.Empty,
                            Name = row.Name ?? string.Empty,
                            Description = row.Description ?? string.Empty,
                            RequiredSkill = row.ParentId,
                            WeaponType = row.WeaponType
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDatabase] Error loading SkillDesc.json: {ex.Message}");
            }
        }

        private void LoadSkillDesc2sJson(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                string json = File.ReadAllText(filePath);
                var list = JsonSerializer.Deserialize<List<JsonSkillDesc2Entry>>(json, JsonOptions);
                if (list != null)
                {
                    foreach (var row in list)
                    {
                        if (row.Id <= 0) continue;
                        SkillDesc2s[row.Id] = new SkillDesc2Def
                        {
                            SkillId = row.Id,
                            BaseId = row.BaseSkillId,
                            RequiredGrade = row.ReqGrade,
                            Level = row.Level,
                            NextId = row.NextSkillId,
                            RequiredDex = row.ReqDex,
                            SkillType = row.SkillType,
                            Power = row.Power > 0 ? row.Power : row.EnhancedHit,
                            Time = row.Duration,
                            Atk = row.EnhancedAtk,
                            Def = row.EnhancedDef,
                            Hit = row.EnhancedHit,
                            Eva = row.EnhancedEva,
                            Cri = row.EnhancedCri,
                            Hp = row.EnhancedHp,
                            AtkSpd = row.EnhancedAtkSpd,
                            MovSpd = row.EnhancedMovSpd,
                            CoolTime = row.EnhancedCooldown
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDatabase] Error loading SkillDesc2.json: {ex.Message}");
            }
        }

        private void LoadMatchingBgmJson(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                string json = File.ReadAllText(filePath);
                var list = JsonSerializer.Deserialize<List<JsonMatchingBgmEntry>>(json, JsonOptions);
                if (list != null)
                {
                    foreach (var row in list)
                    {
                        if (row.Id > 0 && !string.IsNullOrEmpty(row.TrackName))
                        {
                            MatchingBgm[row.Id] = row.TrackName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDatabase] Error loading MatchingBGM.json: {ex.Message}");
            }
        }

        private void LoadKujiTableJson(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                string json = File.ReadAllText(filePath);
                var list = JsonSerializer.Deserialize<List<JsonKujiEntry>>(json, JsonOptions);
                if (list != null)
                {
                    foreach (var row in list)
                    {
                        if (!string.IsNullOrWhiteSpace(row.Result))
                        {
                            KujiResults.Add(row.Result.Trim());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDatabase] Error loading kuji.json: {ex.Message}");
            }
        }

        #endregion

        #region Score & Dialogue XML Loaders

        public void LoadProductListXml(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                Encoding enc = Encoding.GetEncoding("Shift_JIS");
                string xmlText = File.ReadAllText(filePath, enc);
                // Strip XML declaration to prevent parser encoding mismatch
                xmlText = Regex.Replace(xmlText, @"<\?xml[^>]*\?>", string.Empty);
                var doc = System.Xml.Linq.XDocument.Parse(xmlText);
                foreach (var prodElem in doc.Descendants("product"))
                {
                    int pid = int.TryParse(prodElem.Element("id")?.Value, out int id) ? id : 0;
                    int price = int.TryParse(prodElem.Element("price")?.Value, out int pr) ? pr : 0;
                    int dpOption = int.TryParse(prodElem.Element("dp_option")?.Value, out int dp) ? dp : 0;
                    int priceType = int.TryParse(prodElem.Element("pricetype")?.Value, out int pt) ? pt : 0;

                    var itemIds = new System.Collections.Generic.List<int>();
                    var itemsElem = prodElem.Element("items");
                    if (itemsElem != null)
                    {
                        foreach (var itemElem in itemsElem.Elements("item"))
                        {
                            if (int.TryParse(itemElem.Attribute("id")?.Value, out int itemId))
                            {
                                itemIds.Add(itemId);
                            }
                        }
                    }

                    var prod = StarProducts.Find(p => p.ProductId == pid);
                    if (prod == null)
                    {
                        prod = new ShopProductDef { ProductId = pid, Price = price, DisplayOption = dpOption, PriceType = priceType };
                        StarProducts.Add(prod);
                    }
                    if (price > 0) prod.Price = price;
                    prod.DisplayOption = dpOption;
                    prod.PriceType = priceType;
                    prod.ItemIds = itemIds;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDatabase] Error loading ProductList.xml: {ex.Message}");
            }
        }

        public void LoadFieldScoreData(string scoreDir)
        {
            if (!Directory.Exists(scoreDir)) return;

            int totalNpcs = 0;
            int totalGates = 0;
            int totalTerminals = 0;
            int totalMonsters = 0;

            foreach (var field in Fields.Values)
            {
                if (string.IsNullOrWhiteSpace(field.Code)) continue;
                string fdir = Path.Combine(scoreDir, field.Code);
                if (!Directory.Exists(fdir)) continue;

                // 1. Parse default.xml for Warpgates, Terminals, and Generators
                string defXml = Path.Combine(fdir, "default.xml");
                if (File.Exists(defXml))
                {
                    try
                    {
                        string text = File.ReadAllText(defXml, Encoding.GetEncoding("Shift_JIS"));

                        // Warpgates (<warpgate ... />)
                        foreach (Match m in Regex.Matches(text, @"<warpgate\s+([^>]+)>", RegexOptions.IgnoreCase))
                        {
                            string attr = m.Groups[1].Value;
                            var gate = new FieldWarpGate
                            {
                                Id = GetIntAttr(attr, "id"),
                                X = GetIntAttr(attr, "x"),
                                Y = GetIntAttr(attr, "y"),
                                Shell = GetIntAttr(attr, "shell"),
                                CliId = GetIntAttr(attr, "idCli"),
                                Dir = GetIntAttr(attr, "dir"),
                                DestFieldId = GetIntAttr(attr, "idDestField"),
                                DestX = GetIntAttr(attr, "xDest"),
                                DestY = GetIntAttr(attr, "yDest")
                            };
                            field.WarpGates.Add(gate);
                            totalGates++;
                        }

                        // Episode gates (<episodegate ... /> -> ObjectType = 2)
                        foreach (Match m in Regex.Matches(text, @"<episodegate\s+([^>]+)>", RegexOptions.IgnoreCase))
                        {
                            string attr = m.Groups[1].Value;
                            var obj = new FieldTerminalObject
                            {
                                ObjectId = field.TerminalObjects.Count + 1,
                                ObjectType = 2,
                                SubId = GetIntAttr(attr, "subid"),
                                CliId = GetIntAttr(attr, "idCli"),
                                ShellId = GetIntAttr(attr, "shell"),
                                X = GetFloatAttr(attr, "x"),
                                Y = GetFloatAttr(attr, "y"),
                                Dir = GetIntAttr(attr, "dir")
                            };
                            field.TerminalObjects.Add(obj);
                            totalTerminals++;
                        }

                        // Hairdresser (<hairdresser ... /> -> ObjectType = 6)
                        foreach (Match m in Regex.Matches(text, @"<hairdresser\s+([^>]+)>", RegexOptions.IgnoreCase))
                        {
                            string attr = m.Groups[1].Value;
                            var obj = new FieldTerminalObject
                            {
                                ObjectId = field.TerminalObjects.Count + 1,
                                ObjectType = 6,
                                SubId = GetIntAttr(attr, "subid"),
                                CliId = GetIntAttr(attr, "idCli"),
                                ShellId = GetIntAttr(attr, "shell"),
                                X = GetFloatAttr(attr, "x"),
                                Y = GetFloatAttr(attr, "y"),
                                Dir = GetIntAttr(attr, "dir")
                            };
                            field.TerminalObjects.Add(obj);
                            totalTerminals++;
                        }

                        // Generators (<generator> ... </generator>)
                        foreach (Match genMatch in Regex.Matches(text, @"<generator>([\s\S]*?)</generator>", RegexOptions.IgnoreCase))
                        {
                            string genContent = genMatch.Groups[1].Value;
                            var gen = new FieldGeneratorDef();

                            foreach (Match ptMatch in Regex.Matches(genContent, @"<point\s+([^>]+)>", RegexOptions.IgnoreCase))
                            {
                                string attr = ptMatch.Groups[1].Value;
                                gen.Points.Add(new FieldGeneratorPoint
                                {
                                    X = GetFloatAttr(attr, "x"),
                                    Y = GetFloatAttr(attr, "y")
                                });
                            }

                            foreach (Match monMatch in Regex.Matches(genContent, @"<monster\s+([^>]+)>", RegexOptions.IgnoreCase))
                            {
                                string attr = monMatch.Groups[1].Value;
                                gen.Monsters.Add(new FieldGeneratorMonster
                                {
                                    MonsterType = GetIntAttr(attr, "type"),
                                    Count = GetIntAttr(attr, "count")
                                });
                            }

                            if (gen.Points.Count > 0 && gen.Monsters.Count > 0)
                            {
                                field.Generators.Add(gen);
                            }
                        }

                        // Instantiate active FieldMonster entities with authentic sequential IDs per field (matching Delphi TField.FNowMobID)
                        int nextEntityId = 1;
                        foreach (var gen in field.Generators)
                        {
                            if (gen.Points.Count == 0) continue;
                            int ptIdx = 0;
                            foreach (var gmon in gen.Monsters)
                            {
                                if (HuntMonsters.TryGetValue(gmon.MonsterType, out var monDef))
                                {
                                    int spawnCount = Math.Min(30, Math.Max(1, gmon.Count));
                                    for (int i = 0; i < spawnCount; i++, ptIdx++)
                                    {
                                        var pt = gen.Points[ptIdx % gen.Points.Count];
                                        float spawnX = pt.X;
                                        float spawnY = pt.Y;

                                        double angle = (2.0 * Math.PI * i) / Math.Max(1, spawnCount);
                                        float radius = 1.2f + ((i % 3) * 0.8f);
                                        float tryX = pt.X + (float)Math.Cos(angle) * radius;
                                        float tryY = pt.Y + (float)Math.Sin(angle) * radius;
                                        if (MapGridManager.IsWalkable(field.Id, tryX, tryY))
                                        {
                                            spawnX = tryX;
                                            spawnY = tryY;
                                        }

                                        var monster = new FieldMonster
                                        {
                                            EntityId = nextEntityId++,
                                            MonsterType = monDef.TypeMonster,
                                            Name = monDef.Name,
                                            Level = monDef.Level,
                                            CurrentHp = monDef.HpMax,
                                            MaxHp = monDef.HpMax,
                                            X = spawnX,
                                            Y = spawnY,
                                            SpawnX = spawnX,
                                            SpawnY = spawnY,
                                            DirX = 0,
                                            DirY = 1,
                                            StartX = spawnX,
                                            StartY = spawnY,
                                            DestX = spawnX,
                                            DestY = spawnY,
                                            State = MonsterState.Wait,
                                            MoveMotion = 1,
                                            MoveSpeedRate = 80,
                                            AttackPower = Math.Max(5, monDef.Level * 4),
                                            MotionType = monDef.Motion > 0 ? monDef.Motion : 300011,
                                            ExpReward = monDef.Exp,
                                            DropItemType = monDef.DropItemType,
                                            DropCount = monDef.DropCount,
                                            DropRate = monDef.DropRate,
                                            RespawnSeconds = 15,
                                            Frame = (uint)Random.Shared.Next(0, 15),
                                            NextWanderInterval = Random.Shared.Next(10, 30)
                                        };
                                        field.Monsters.Add(monster);
                                        totalMonsters++;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[GameDatabase] Error reading {defXml}: {ex.Message}");
                    }
                }

                // 2. Parse all other XML files for NPCs and Dialogs
                try
                {
                    var files = Directory.GetFiles(fdir, "*.xml");
                    Array.Sort(files, StringComparer.OrdinalIgnoreCase);

                    foreach (var f in files)
                    {
                        string fname = Path.GetFileName(f);
                        if (fname.Equals("default.xml", StringComparison.OrdinalIgnoreCase))
                            continue;

                        string text = File.ReadAllText(f, Encoding.GetEncoding("Shift_JIS"));

                        // 1. Spawn field NPCs defined in this XML (including Absolute Knowledge / Guide Books)
                        foreach (Match m in Regex.Matches(text, @"<npc\s+([^>]+)>", RegexOptions.IgnoreCase))
                        {
                            string attr = m.Groups[1].Value;
                            int id = GetIntAttr(attr, "id");
                            int shell = GetIntAttr(attr, "shell");
                            int x = GetIntAttr(attr, "x");
                            int y = GetIntAttr(attr, "y");
                            int dir = GetIntAttr(attr, "dir");

                            if (id > 0 && !field.Npcs.Any(n => n.NpcId == id && n.X == x && n.Y == y))
                            {
                                field.Npcs.Add(new FieldNpcSpawn
                                {
                                    NpcId = id,
                                    ShellType = shell,
                                    X = (ushort)x,
                                    Y = (ushort)y,
                                    Dir = dir,
                                    InitScript = GetStringAttr(attr, "init")
                                });
                                totalNpcs++;
                            }
                        }

                        // 2. Attach dialogue scripts directly to THIS field instance and global cache
                        ParseNpcDialogsFromXml(text, field);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GameDatabase] Error parsing NPCs in {fdir}: {ex.Message}");
                }
            }

            Console.WriteLine($"[GameDatabase] Loaded {totalNpcs} Field NPCs, {NpcScripts.Count} NPC Dialog Trees, {totalGates} WarpGates, {totalTerminals} Terminal Objects, {totalMonsters} Field Monsters across {Fields.Count} maps.");
        }

        protected void ParseNpcDialogsFromXml(string xmlContent, GameFieldDef? field = null)
        {
            try
            {
                var npcMatches = Regex.Matches(xmlContent, @"<npc\s+([^>]+)>(.*?)</npc>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                foreach (Match nm in npcMatches)
                {
                    string attr = nm.Groups[1].Value;
                    string body = nm.Groups[2].Value;
                    int id = GetIntAttr(attr, "id");
                    if (id <= 0) continue;

                    var script = new NpcScriptDef { NpcId = id };
                    var actionMatches = Regex.Matches(body, @"<(script|dialog)\s+([^>]+)>(.*?)</\1>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

                    int actionIndex = 1;
                    foreach (Match am in actionMatches)
                    {
                        string tag = am.Groups[1].Value.ToLowerInvariant();
                        string tagAttr = am.Groups[2].Value;
                        string tagBody = am.Groups[3].Value;

                        if (tag == "script")
                        {
                            string sName = GetStringAttr(tagAttr, "name");
                            script.Scripts[sName] = tagBody;
                            if (sName.Equals("init", StringComparison.OrdinalIgnoreCase))
                            {
                                script.InitScriptBody = tagBody;
                            }
                        }
                        else if (tag == "dialog")
                        {
                            string dlgName = GetStringAttr(tagAttr, "name");
                            int dlgId = GetIntAttr(tagAttr, "id");
                            int cutin = GetIntAttr(tagAttr, "cutin");

                            if (dlgId <= 0) dlgId = actionIndex;
                            if (string.IsNullOrEmpty(dlgName)) dlgName = dlgId.ToString();
                            if (string.IsNullOrEmpty(script.InitialDialogName)) script.InitialDialogName = dlgName;

                            var textMatch = Regex.Match(tagBody, @"<text>(?:<!\[CDATA\[)?(.*?)(?:\]\]>)?</text>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                            string rawText = textMatch.Success ? textMatch.Groups[1].Value : string.Empty;
                            rawText = rawText.Replace("\r\n", "\n");

                            int closebutton = GetIntAttr(attr, "closebutton");

                            var dlgDef = new NpcDialogDef
                            {
                                Id = dlgId,
                                Name = dlgName,
                                Text = rawText,
                                CutIn = cutin,
                                CloseButton = closebutton
                            };

                            var selMatches = Regex.Matches(tagBody, @"<selection\s+([^>]+)/>", RegexOptions.IgnoreCase);
                            foreach (Match sm in selMatches)
                            {
                                string selAttr = sm.Groups[1].Value;
                                dlgDef.Selections.Add(new NpcDialogSelectionDef
                                {
                                    Text = GetStringAttr(selAttr, "text"),
                                    Next = GetStringAttr(selAttr, "next"),
                                    Op = GetStringAttr(selAttr, "op"),
                                    VarName = GetStringAttr(selAttr, "varname"),
                                    Value = GetIntAttr(selAttr, "value")
                                });
                            }

                            if (!string.IsNullOrEmpty(dlgName)) script.Dialogs[dlgName] = dlgDef;
                            script.Dialogs[dlgId.ToString()] = dlgDef;
                        }

                        actionIndex++;
                    }

                    if (script.Dialogs.Count > 0 || script.Scripts.Count > 0)
                    {
                        NpcScripts[id] = script;
                        if (field != null) field.NpcScripts[id] = script;
                    }
                }
            }
            catch { }
        }

        private static int GetIntAttr(string attr, string name)
        {
            var m = Regex.Match(attr, name + @"=""?([0-9]+)""?", RegexOptions.IgnoreCase);
            return m.Success && int.TryParse(m.Groups[1].Value, out int v) ? v : 0;
        }

        private static float GetFloatAttr(string attr, string name)
        {
            var m = Regex.Match(attr, name + @"=""?([0-9.]+)""?", RegexOptions.IgnoreCase);
            return m.Success && float.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : 0f;
        }

        private static string GetStringAttr(string attr, string name)
        {
            var m = Regex.Match(attr, name + @"=""([^""]*)""", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : string.Empty;
        }

        #endregion

        #region JSON Transfer Models

        private sealed class JsonExpEntry
        {
            public int Level { get; set; }
            public int RequiredExp { get; set; }
        }

        private sealed class JsonStatusEntry
        {
            public int Level { get; set; }
            public int Power { get; set; }
            public int Speed { get; set; }
            public int Skill { get; set; }
            public int Luck { get; set; }
        }

        private sealed class JsonDexEntry
        {
            public int DexLevel { get; set; }
            public int RequiredExp { get; set; }
        }

        private sealed class JsonBeItemEntry
        {
            public int Id { get; set; }
            public string? Code { get; set; }
            public string? Name { get; set; }
            public string? Description { get; set; }
            public int ItemType { get; set; }
            public int Flag { get; set; }
            public int EquipPos { get; set; }
            public int WeaponType { get; set; }
            public int Sex { get; set; }
            public int School { get; set; }
            public int AttackGroup { get; set; }
            public int SkillId { get; set; }
            public int LevelReq { get; set; }
            public int BeItemSlot { get; set; }
            public int Price { get; set; }
        }

        private sealed class JsonCoItemEntry
        {
            public int Id { get; set; }
            public string? Code { get; set; }
            public string? Name { get; set; }
            public string? Description { get; set; }
            public int Attr { get; set; }
            public int UseType { get; set; }
            public bool Quick { get; set; }
            public bool QuickUsable { get; set; }
            public int Price { get; set; }
        }

        private sealed class JsonByulBeItemEntry
        {
            public int Id { get; set; }
            public string? Code { get; set; }
            public string? Name { get; set; }
            public string? Description { get; set; }
            public int Attr { get; set; }
            public int EquipPos { get; set; }
            public int WeaponType { get; set; }
            public int Sex { get; set; }
            public int School { get; set; }
            public int AttackGroup { get; set; }
            public int SkillId { get; set; }
            public int GradeReq { get; set; }
            public int LevelReq { get; set; }
        }

        private sealed class JsonByulItemEntry
        {
            public int Id { get; set; }
            public string? Code { get; set; }
            public string? Name { get; set; }
            public string? Description { get; set; }
            public int DurationDays { get; set; }
            public int BeType { get; set; }
            public int Sex { get; set; }
            public int School { get; set; }
            public int Effect { get; set; }
        }

        private sealed class JsonEnItemEntry
        {
            public int Id { get; set; }
            public string? Code { get; set; }
            public string? Name { get; set; }
            public string? Description { get; set; }
            public int Element { get; set; }
            public int UseType { get; set; }
            public int Grade { get; set; }
            public int Price { get; set; }
        }

        private sealed class JsonReinforceStoneEntry
        {
            public int CoItemTypeId { get; set; }
            public int Grade { get; set; }
            public int Level { get; set; }
            public int Hp { get; set; }
            public int Atk { get; set; }
            public int Def { get; set; }
            public int ExpandGauge { get; set; }
            public int WeaponType { get; set; }
            public int EquipPos { get; set; }
        }

        private sealed class JsonEpisodeEntry
        {
            public int Id { get; set; }
            public string? Title { get; set; }
            public string? CleanTitle { get; set; }
            public int School { get; set; }
            public int GradeReq { get; set; }
            public int GateSubId { get; set; }
        }

        private sealed class JsonTitleEntry
        {
            public int Id { get; set; }
            public string? Name { get; set; }
        }

        private sealed class JsonNpcEntry
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public int PortraitCutinId { get; set; }
        }

        private sealed class JsonShopItemEntry
        {
            public int ItemId { get; set; }
            public int Price { get; set; }
            public int Grade { get; set; }
            public int Category { get; set; }
        }

        private sealed class JsonHuntMonEntry
        {
            public int Id { get; set; }
            public int HuntFieldId { get; set; }
            public int BasisMobId { get; set; }
            public string? Name { get; set; }
            public int MaxHp { get; set; }
            public int Level { get; set; }
            public int Motion { get; set; }
            public int DropItemId { get; set; }
            public int DropCount { get; set; }
            public int DropRate { get; set; }
            public int ExpReward { get; set; }
        }

        private sealed class JsonFieldEntry
        {
            public int Id { get; set; }
            public string? Code { get; set; }
            public string? Name { get; set; }
            public int BgmId { get; set; }
            public bool IsHuntField { get; set; }
            public int HuntFieldId { get; set; }
        }

        private sealed class JsonAtkWeaponEntry
        {
            public int Id { get; set; }
            public int Range { get; set; }
            public int Area { get; set; }
            public int Param1 { get; set; }
            public int Param2 { get; set; }
            public int Power { get; set; }
        }

        private sealed class JsonSkillWeaponEntry
        {
            public int Id { get; set; }
            public string? Code { get; set; }
            public string? Name { get; set; }
            public float RangeMax { get; set; }
            public int DelayMs { get; set; }
        }

        private sealed class JsonSkillDescEntry
        {
            public int Id { get; set; }
            public string? Code { get; set; }
            public string? Name { get; set; }
            public string? Description { get; set; }
            public int ParentId { get; set; }
            public int WeaponType { get; set; }
        }

        private sealed class JsonSkillDesc2Entry
        {
            public int Id { get; set; }
            public int BaseSkillId { get; set; }
            public int ReqGrade { get; set; }
            public int Level { get; set; }
            public int NextSkillId { get; set; }
            public int ReqDex { get; set; }
            public int SkillType { get; set; }
            public int Power { get; set; }
            public int Duration { get; set; }
            public int EnhancedAtk { get; set; }
            public int EnhancedDef { get; set; }
            public int EnhancedHit { get; set; }
            public int EnhancedEva { get; set; }
            public int EnhancedCri { get; set; }
            public int EnhancedHp { get; set; }
            public int EnhancedAtkSpd { get; set; }
            public int EnhancedMovSpd { get; set; }
            public int EnhancedCooldown { get; set; }
        }

        private sealed class JsonMatchingBgmEntry
        {
            public int Id { get; set; }
            public string? TrackName { get; set; }
        }

        private sealed class JsonKujiEntry
        {
            public int Id { get; set; }
            public string? Result { get; set; }
        }

        #endregion
    }

    public sealed class NpcDialogSelectionDef
    {
        public string Text { get; set; } = string.Empty;
        public string Next { get; set; } = string.Empty;
        public string Op { get; set; } = string.Empty;
        public string VarName { get; set; } = string.Empty;
        public int Value { get; set; } = 0;
    }

    public sealed class NpcDialogDef
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int CutIn { get; set; } = 0;
        public int CloseButton { get; set; } = 0;
        public List<NpcDialogSelectionDef> Selections { get; set; } = new();
    }

    public sealed class NpcScriptDef
    {
        public int NpcId { get; set; }
        public string InitialDialogName { get; set; } = string.Empty;
        public string InitScriptBody { get; set; } = string.Empty;
        public Dictionary<string, string> Scripts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, NpcDialogDef> Dialogs { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public string EvaluateInit(int school, int level, int grade, int epiYoi, int epiEs, Dictionary<string, int>? scriptVars = null)
        {
            if (string.IsNullOrWhiteSpace(InitScriptBody))
            {
                return !string.IsNullOrEmpty(InitialDialogName) ? InitialDialogName : (Dialogs.Keys.FirstOrDefault() ?? string.Empty);
            }

            var vars = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["local.school"] = school,
                ["local.level"] = level,
                ["local.grade"] = grade,
                ["peke.epi.yoi"] = school == 2 ? epiYoi : -1,
                ["peke.epi.es"] = school == 1 ? epiEs : -1
            };

            if (scriptVars != null)
            {
                foreach (var kvp in scriptVars)
                {
                    vars[kvp.Key] = kvp.Value;
                }
            }

            string? targetJump = EvaluateBlock(InitScriptBody, vars);

            if (scriptVars != null)
            {
                foreach (var kvp in vars)
                {
                    if (!kvp.Key.StartsWith("local.", StringComparison.OrdinalIgnoreCase))
                    {
                        scriptVars[kvp.Key] = kvp.Value;
                    }
                }
            }

            if (!string.IsNullOrEmpty(targetJump))
            {
                return ResolveToDialog(targetJump, vars);
            }

            return string.Empty;
        }

        public string ResolveNext(string nextLabel, int school, int level, int grade, ref int epiYoi, ref int epiEs, Dictionary<string, int>? scriptVars = null)
        {
            var vars = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["local.school"] = school,
                ["local.level"] = level,
                ["local.grade"] = grade,
                ["peke.epi.yoi"] = epiYoi,
                ["peke.epi.es"] = epiEs
            };

            if (scriptVars != null)
            {
                foreach (var kvp in scriptVars)
                {
                    vars[kvp.Key] = kvp.Value;
                }
            }

            string result = ResolveToDialog(nextLabel, vars);
            if (vars.TryGetValue("peke.epi.yoi", out int yoiVal)) epiYoi = yoiVal;
            if (vars.TryGetValue("peke.epi.es", out int esVal)) epiEs = esVal;

            if (scriptVars != null)
            {
                foreach (var kvp in vars)
                {
                    if (!kvp.Key.StartsWith("local.", StringComparison.OrdinalIgnoreCase))
                    {
                        scriptVars[kvp.Key] = kvp.Value;
                    }
                }
            }

            return result;
        }

        private string ResolveToDialog(string label, Dictionary<string, int> vars)
        {
            int depth = 0;
            string current = label;

            while (depth++ < 10)
            {
                if (Dialogs.ContainsKey(current))
                {
                    return current;
                }

                if (Scripts.TryGetValue(current, out var scriptBody))
                {
                    string? nextJump = EvaluateBlock(scriptBody, vars);
                    if (!string.IsNullOrEmpty(nextJump))
                    {
                        current = nextJump;
                        continue;
                    }
                }

                break;
            }

            return current;
        }

        private static string? EvaluateBlock(string xmlBlock, Dictionary<string, int> vars)
        {
            var matches = Regex.Matches(xmlBlock, @"<(?<tag>ifeq|ifne|ifge|ifle|ifgt|iflt|ifdef|ifndef|switch|case|jump|set|inc|dec)\s*([^>]*?)(?:>(.*?)</\k<tag>>|/>)", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            foreach (Match m in matches)
            {
                string tag = m.Groups["tag"].Value.ToLowerInvariant();
                string attr = m.Groups[1].Value;
                string inner = m.Groups[2].Value;

                if (tag == "jump")
                {
                    string label = GetAttr(attr, "label");
                    if (!string.IsNullOrEmpty(label)) return label;
                }
                else if (tag == "set")
                {
                    string varname = GetAttr(attr, "varname");
                    int val = GetIntAttrVal(attr, "value");
                    vars[varname] = val;
                }
                else if (tag == "inc")
                {
                    string varname = GetAttr(attr, "varname");
                    int curr = vars.TryGetValue(varname, out var v) ? v : 0;
                    vars[varname] = curr + 1;
                }
                else if (tag == "dec")
                {
                    string varname = GetAttr(attr, "varname");
                    int curr = vars.TryGetValue(varname, out var v) ? v : 0;
                    vars[varname] = curr - 1;
                }
                else if (tag == "ifndef")
                {
                    string varname = GetAttr(attr, "varname");
                    if (!vars.ContainsKey(varname))
                    {
                        string? j = EvaluateBlock(inner, vars);
                        if (!string.IsNullOrEmpty(j)) return j;
                    }
                }
                else if (tag == "ifdef")
                {
                    string varname = GetAttr(attr, "varname");
                    if (vars.ContainsKey(varname))
                    {
                        string? j = EvaluateBlock(inner, vars);
                        if (!string.IsNullOrEmpty(j)) return j;
                    }
                }
                else if (tag == "switch")
                {
                    string varname = GetAttr(attr, "varname");
                    int currentVal = vars.TryGetValue(varname, out var v) ? v : 0;
                    var cases = Regex.Matches(inner, @"<case\s+value=""?(\d+)""?\s*>(.*?)</case>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                    foreach (Match cm in cases)
                    {
                        if (int.TryParse(cm.Groups[1].Value, out int caseVal) && caseVal == currentVal)
                        {
                            string? j = EvaluateBlock(cm.Groups[2].Value, vars);
                            if (!string.IsNullOrEmpty(j)) return j;
                        }
                    }
                }
                else if (tag.StartsWith("if"))
                {
                    string varname = GetAttr(attr, "varname");
                    int val = GetIntAttrVal(attr, "value");
                    int currentVal = vars.TryGetValue(varname, out var v) ? v : 0;

                    bool conditionMet = tag switch
                    {
                        "ifeq" => currentVal == val,
                        "ifne" => currentVal != val,
                        "ifge" => currentVal >= val,
                        "ifle" => currentVal <= val,
                        "ifgt" => currentVal > val,
                        "iflt" => currentVal < val,
                        _ => false
                    };

                    if (conditionMet)
                    {
                        string? j = EvaluateBlock(inner, vars);
                        if (!string.IsNullOrEmpty(j)) return j;
                    }
                }
            }

            return null;
        }

        private static int GetIntAttrVal(string attr, string name)
        {
            var m = Regex.Match(attr, name + @"=""?([0-9]+)""?", RegexOptions.IgnoreCase);
            return m.Success && int.TryParse(m.Groups[1].Value, out int v) ? v : 0;
        }

        private static string GetAttr(string attr, string name)
        {
            var m = Regex.Match(attr, name + @"=""([^""]*)""", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : string.Empty;
        }
    }

    public struct StatusDef
    {
        public int Level;
        public int Pow;
        public int Speed;
        public int Skill;
        public int Luck;
    }

    public sealed class GameItemDef
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Attr { get; set; }
        public int Flag { get; set; }
        public int BaseItemType { get; set; }
        public int EquipPos { get; set; }
        public int WeaponType { get; set; }
        public int Sex { get; set; }
        public int School { get; set; }
        public int GradeReq { get; set; }
        public int AttackGroup { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int SkillId { get; set; }
        public int Price { get; set; }
        public int UseType { get; set; }
        public bool QuickUsable { get; set; }
        public int DurationDays { get; set; }
        public int RecoveryAmount { get; set; }
        public int EffectId { get; set; }

        public bool IsEquipment => EquipPos > 0 || WeaponType > 0;
        public bool IsWeapon => WeaponType > 0;

        public ushort GetTargetEquipSlot()
        {
            if (WeaponType > 0) return 4;
            if (EquipPos == 1 || (EquipPos & 1) != 0) return 1;
            if (EquipPos == 2) return 2;
            if (EquipPos == 4) return 3;
            if (EquipPos == 8 || (EquipPos & 8) != 0) return 5;
            if (EquipPos == 16) return 6;
            if (EquipPos == 128) return 8;
            if (EquipPos == 256) return 9;
            if (EquipPos == 64 || EquipPos == 192 || EquipPos == 448 || (EquipPos & 64) != 0) return 7;
            return 0;
        }
    }

    public sealed class ReinforceStoneDef
    {
        public int Id { get; set; }
        public int Grade { get; set; }
        public int Level { get; set; }
        public int Hp { get; set; }
        public int Atk { get; set; }
        public int Def { get; set; }
        public int ExpandGauge { get; set; }
        public int WeaponType { get; set; }
        public int EquipPos { get; set; }
    }

    public class GameEpisodeDef
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CleanTitle { get; set; } = string.Empty;
        public int School { get; set; }
        public int GradeReq { get; set; }
        public int GateSubId { get; set; }
    }

    public class GameSkillDef
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public sealed class GameFieldDef
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsHuntField { get; set; }
        public bool IsEpisode { get; set; }
        public int HuntFieldId { get; set; }
        public int Bgm { get; set; } = 6;
        public List<FieldNpcSpawn> Npcs { get; } = new();
        public List<FieldWarpGate> WarpGates { get; } = new();
        public List<FieldTerminalObject> TerminalObjects { get; } = new();
        public Dictionary<int, NpcScriptDef> NpcScripts { get; } = new();
        public List<FieldMonster> Monsters { get; } = new();
        public List<FieldGeneratorDef> Generators { get; } = new();
    }

    public class FieldGeneratorPoint
    {
        public float X { get; set; }
        public float Y { get; set; }
    }

    public class FieldGeneratorMonster
    {
        public int MonsterType { get; set; }
        public int Count { get; set; }
    }

    public class FieldGeneratorDef
    {
        public List<FieldGeneratorPoint> Points { get; } = new();
        public List<FieldGeneratorMonster> Monsters { get; } = new();
    }

    public class HuntMonsterDef
    {
        public int IdHuntField { get; set; }
        public int TypeMonster { get; set; }
        public int HpMax { get; set; }
        public int Level { get; set; }
        public int TypeBasis { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Motion { get; set; }
        public int DropItemType { get; set; }
        public int DropCount { get; set; }
        public int DropRate { get; set; }
        public int Exp { get; set; }
    }

    public class FieldNpcSpawn
    {
        public int NpcId { get; set; }
        public int ShellType { get; set; }
        public ushort X { get; set; }
        public ushort Y { get; set; }
        public int Dir { get; set; }
        public string InitScript { get; set; } = string.Empty;
    }

    public class FieldWarpGate
    {
        public int Id { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Shell { get; set; }
        public int CliId { get; set; }
        public int Dir { get; set; }
        public int DestFieldId { get; set; }
        public int DestX { get; set; }
        public int DestY { get; set; }
    }

    public class FieldTerminalObject
    {
        public int ObjectId { get; set; }
        public int ObjectType { get; set; }
        public int SubId { get; set; }
        public int CliId { get; set; }
        public int ShellId { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public int Dir { get; set; }
    }

    public class ShopItemDef
    {
        public int ItemId { get; set; }
        public int Price { get; set; }
        public int Grade { get; set; }
        public int Category { get; set; }
    }

    public class AtkWeaponDef
    {
        public int ItemType { get; set; }
        public int Category { get; set; }
        public int AtkRatio { get; set; }
        public int HitMotion { get; set; }
        public int Range { get; set; }
        public int Angle { get; set; }
    }

    public class SkillDescDef
    {
        public int SkillId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int RequiredSkill { get; set; }
        public int WeaponType { get; set; }
    }

    public class SkillWeaponDef
    {
        public int SkillId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Range { get; set; }
        public int Delay { get; set; } = 100;
    }

    public class SkillDesc2Def
    {
        public int SkillId { get; set; }
        public int BaseId { get; set; }
        public int RequiredGrade { get; set; }
        public int Level { get; set; }
        public int NextId { get; set; }
        public int RequiredDex { get; set; }
        public int SkillType { get; set; }
        public int Power { get; set; }
        public int Time { get; set; }
        public int Atk { get; set; }
        public int Def { get; set; }
        public int Hit { get; set; }
        public int Eva { get; set; }
        public int Cri { get; set; }
        public int Hp { get; set; }
        public int AtkSpd { get; set; }
        public int MovSpd { get; set; }
        public int CoolTime { get; set; }
    }
}
