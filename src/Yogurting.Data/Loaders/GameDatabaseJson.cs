using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Yogurting.Core.Models;

namespace Yogurting.Data.Loaders
{
    /// <summary>
    /// Modern JSON-based Game Database Loader.
    /// Reads all 30 game database tables from server_modern/data/dbJson/ with high-performance System.Text.Json deserialization.
    /// </summary>
    public class GameDatabaseJson : GameDatabase
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public override void LoadAll(string dbJsonDir)
        {
            try { System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance); } catch { }
            Console.WriteLine("[GameDatabaseJson] Loading parameter tables from JSON dataset...");

            LoadExpTableJson(Path.Combine(dbJsonDir, "ExpTable.json"));
            LoadStatusTableJson(Path.Combine(dbJsonDir, "StatusTable.json"));
            LoadDexTableJson(Path.Combine(dbJsonDir, "DexTable.json"));
            LoadBeItemsJson(Path.Combine(dbJsonDir, "BeItemType.json"));
            LoadCoItemsJson(Path.Combine(dbJsonDir, "CoItemType.json"));
            LoadByulBeItemsJson(Path.Combine(dbJsonDir, "ByulItemBeType.json"));
            LoadByulItemsJson(Path.Combine(dbJsonDir, "ByulItemType.json"));
            LoadEnItemsJson(Path.Combine(dbJsonDir, "EnItemType.json"));
            LoadReinforceStonesJson(Path.Combine(dbJsonDir, "ReinforceStone.json"));
            LoadEpisodesJson(Path.Combine(dbJsonDir, "Episode.json"));
            LoadTitlesJson(Path.Combine(dbJsonDir, "Title.json"));
            LoadNpcsJson(Path.Combine(dbJsonDir, "NpcEx.json"));
            LoadShopItemListJson(Path.Combine(dbJsonDir, "ShopItemList.json"));
            LoadHuntMonstersJson(Path.Combine(dbJsonDir, "HuntMon.json"));
            LoadFieldsJson(Path.Combine(dbJsonDir, "Field.json"));

            string mapDbPath = Path.Combine(dbJsonDir, "map.db");
            if (!File.Exists(mapDbPath))
            {
                mapDbPath = Path.Combine(dbJsonDir, "..", "db", "map.db");
            }
            MapGridManager.Initialize(mapDbPath, Fields);

            LoadAtkWeaponsJson(Path.Combine(dbJsonDir, "AtkWeapon.json"));
            LoadSkillWeaponsJson(Path.Combine(dbJsonDir, "SkillWeapon.json"));
            LoadSkillDescsJson(Path.Combine(dbJsonDir, "SkillDesc.json"));
            LoadSkillDesc2sJson(Path.Combine(dbJsonDir, "SkillDesc2.json"));
            LoadMatchingBgmJson(Path.Combine(dbJsonDir, "MatchingBGM.json"));
            LoadKujiTableJson(Path.Combine(dbJsonDir, "kuji.json"));

            string scoreDir = Path.Combine(dbJsonDir, "..", "score");
            if (!Directory.Exists(scoreDir))
            {
                scoreDir = Path.Combine(AppContext.BaseDirectory, "data", "score");
            }
            if (Directory.Exists(scoreDir))
            {
                LoadFieldScoreData(scoreDir);
            }

            string productListXml = Path.Combine(dbJsonDir, "..", "score", "ProductList.xml");
            if (!File.Exists(productListXml))
            {
                productListXml = Path.Combine(AppContext.BaseDirectory, "data", "score", "ProductList.xml");
            }
            LoadProductListXml(productListXml);

            // Link ByulItemType (Cash/Star) to ByulItemBeType (Base equipment properties)
            foreach (var item in Items.Values)
            {
                if (item.BaseItemType > 0 && Items.TryGetValue(item.BaseItemType, out var baseItem))
                {
                    if (item.EquipPos == 0) item.EquipPos = baseItem.EquipPos;
                    if (item.WeaponType == 0) item.WeaponType = baseItem.WeaponType;
                    if (item.Attack == 0) item.Attack = baseItem.Attack;
                    if (item.SkillId == 0) item.SkillId = baseItem.SkillId;
                }
            }

            Console.WriteLine($"[GameDatabaseJson] Loaded {Items.Count} Items, {ReinforceStones.Count} Reinforce Stones, {Episodes.Count} Episodes, {Titles.Count} Titles, {Npcs.Count} NPCs, {ShopItems.Count} Shop Items, {StarProducts.Count} Star Products, {Fields.Count} Fields!");
        }

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
                Console.WriteLine($"[GameDatabaseJson] Error loading ExpTable.json: {ex.Message}");
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
                Console.WriteLine($"[GameDatabaseJson] Error loading StatusTable.json: {ex.Message}");
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
                Console.WriteLine($"[GameDatabaseJson] Error loading DexTable.json: {ex.Message}");
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
                            Attack = row.AttackGroup,
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
                Console.WriteLine($"[GameDatabaseJson] Error loading BeItemType.json: {ex.Message}");
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
                Console.WriteLine($"[GameDatabaseJson] Error loading CoItemType.json: {ex.Message}");
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
                        item.Attack = row.AttackGroup > 0 ? row.AttackGroup : item.Attack;
                        item.SkillId = row.SkillId;
                        item.GradeReq = row.LevelReq > 0 ? row.LevelReq : (row.GradeReq > 0 ? row.GradeReq : item.GradeReq);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDatabaseJson] Error loading ByulItemBeType.json: {ex.Message}");
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
                Console.WriteLine($"[GameDatabaseJson] Error loading ByulItemType.json: {ex.Message}");
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
                Console.WriteLine($"[GameDatabaseJson] Error loading EnItemType.json: {ex.Message}");
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
                Console.WriteLine($"[GameDatabaseJson] Error loading ReinforceStone.json: {ex.Message}");
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
                            Title = row.Title ?? string.Empty
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDatabaseJson] Error loading Episode.json: {ex.Message}");
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
                Console.WriteLine($"[GameDatabaseJson] Error loading Title.json: {ex.Message}");
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
                Console.WriteLine($"[GameDatabaseJson] Error loading NpcEx.json: {ex.Message}");
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
                Console.WriteLine($"[GameDatabaseJson] Error loading ShopItemList.json: {ex.Message}");
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
                Console.WriteLine($"[GameDatabaseJson] Error loading HuntMon.json: {ex.Message}");
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
                Console.WriteLine($"[GameDatabaseJson] Error loading Field.json: {ex.Message}");
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
                            AtkRatio = row.Area,
                            HitMotion = row.Param1,
                            Range = row.Param2 > 0 ? row.Param2 : 22,
                            Angle = row.Power > 0 ? row.Power : 63
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDatabaseJson] Error loading AtkWeapon.json: {ex.Message}");
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
                            Delay = row.DelayMs > 0 ? row.DelayMs : 100
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameDatabaseJson] Error loading SkillWeapon.json: {ex.Message}");
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
                Console.WriteLine($"[GameDatabaseJson] Error loading SkillDesc.json: {ex.Message}");
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
                            Power = row.Power,
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
                Console.WriteLine($"[GameDatabaseJson] Error loading SkillDesc2.json: {ex.Message}");
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
                Console.WriteLine($"[GameDatabaseJson] Error loading MatchingBGM.json: {ex.Message}");
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
                Console.WriteLine($"[GameDatabaseJson] Error loading kuji.json: {ex.Message}");
            }
        }

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
}
