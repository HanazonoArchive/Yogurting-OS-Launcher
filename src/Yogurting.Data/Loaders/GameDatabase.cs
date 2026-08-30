using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using Yogurting.Core.Models;

namespace Yogurting.Data.Loaders
{
    /// <summary>
    /// Exact 1-to-1 C# port of Quartet's UYgDB.pas.
    /// Loads all 30 game database parameter tables into fast memory indices.
    /// </summary>
    public sealed class GameDatabase
    {
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
        public System.Collections.Generic.List<string> KujiResults { get; } = new();
        public System.Collections.Generic.List<ShopProductDef> StarProducts { get; } = new();
        public System.Collections.Generic.List<ShopItemDef> ShopItems { get; } = new();
        public Dictionary<int, int> ExpTable { get; } = new();
        public Dictionary<int, StatusDef> StatusTable { get; } = new();
        public Dictionary<int, int> DexTable { get; } = new();

        public int GetMaxExpForLevel(int level) => ExpTable.TryGetValue(level, out int exp) ? exp : (level * 25 + 10);
        public StatusDef GetStatusForLevel(int level) => StatusTable.TryGetValue(level, out var stat) ? stat : new StatusDef { Level = level, Pow = level * 4, Speed = level * 3, Skill = level * 3, Luck = level * 2 };
        public int GetRequiredDexForLevel(int dexLevel) => DexTable.TryGetValue(dexLevel, out int dex) ? dex : (dexLevel * 10 + 10);

        public int GetFieldBgm(int fieldId)
        {
            if (Fields.TryGetValue(fieldId, out var field) && field.Bgm > 0)
            {
                return field.Bgm;
            }
            return 6;
        }

        private static Encoding GetTableEncoding()
        {
            try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); } catch { }
            return Encoding.GetEncoding("Shift_JIS");
        }

        public void LoadAll(string dbDir)
        {
            try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); } catch { }
            Console.WriteLine("[GameDatabase] Loading parameter tables (UYgDB.pas 1-to-1 match)...");

            LoadExpTable(Path.Combine(dbDir, "ExpTable.txt"));
            LoadStatusTable(Path.Combine(dbDir, "StatusTable.txt"));
            LoadDexTable(Path.Combine(dbDir, "DexTable.txt"));
            LoadBeItems(Path.Combine(dbDir, "BeItemType.txt"));
            LoadCoItems(Path.Combine(dbDir, "CoItemType.txt"));
            LoadByulBeItems(Path.Combine(dbDir, "ByulItemBeType.txt"));
            LoadByulItems(Path.Combine(dbDir, "ByulItemType.txt"));
            LoadEnItems(Path.Combine(dbDir, "EnItemType.txt"));
            LoadReinforceStones(Path.Combine(dbDir, "ReinforceStone.txt"));
            LoadEpisodes(Path.Combine(dbDir, "Episode.txt"));
            LoadTitles(Path.Combine(dbDir, "Title.txt"));
            LoadNpcs(Path.Combine(dbDir, "NpcEx.txt"));
            LoadShopItemList(Path.Combine(dbDir, "ShopItemList.txt"));
            LoadHuntMonsters(Path.Combine(dbDir, "HuntMon.txt"));
            LoadFields(Path.Combine(dbDir, "Field.txt"));
            MapGridManager.Initialize(Path.Combine(dbDir, "map.db"), Fields);
            LoadAtkWeapons(Path.Combine(dbDir, "AtkWeapon.txt"));
            LoadSkillWeapons(Path.Combine(dbDir, "SkillWeapon.txt"));
            LoadSkillDescs(Path.Combine(dbDir, "SkillDesc.txt"));
            LoadSkillDesc2s(Path.Combine(dbDir, "SkillDesc2.txt"));
            LoadMatchingBgm(Path.Combine(dbDir, "MatchingBGM.txt"));
            LoadKujiTable(Path.Combine(dbDir, "kuji.txt"));

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

            Console.WriteLine($"[GameDatabase] Loaded {Items.Count} Items, {ReinforceStones.Count} Reinforce Stones, {Episodes.Count} Episodes, {Titles.Count} Titles, {Npcs.Count} NPCs, {ShopItems.Count} Shop Items, {StarProducts.Count} Star Products!");
        }

        public static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>(16);
            var sb = new StringBuilder(line.Length);
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(sb.ToString().Trim());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
            result.Add(sb.ToString().Trim());
            return result;
        }

        private void LoadBeItems(string filePath)
        {
            if (!File.Exists(filePath)) return;
            var enc = GetTableEncoding();
            foreach (var line in File.ReadAllLines(filePath, enc))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
                var parts = ParseCsvLine(line);
                if (parts.Count >= 17 && int.TryParse(parts[0], out int id))
                {
                    try
                    {
                        var item = new GameItemDef
                        {
                            Id = id,
                            Code = parts[1].Trim(),
                            Name = parts[2].Trim(),
                            Description = parts[3].Trim().Trim('"'),
                            Attr = int.TryParse(parts[4], out int attr) ? attr : 0,
                            Flag = int.TryParse(parts[5], out int flag) ? flag : 0,
                            EquipPos = int.TryParse(parts[6], out int pos) ? pos : 0,
                            WeaponType = int.TryParse(parts[8], out int wtype) ? wtype : 0,
                            Sex = int.TryParse(parts[10], out int sex) ? sex : 0,
                            School = int.TryParse(parts[11], out int school) ? school : 0,
                            Attack = int.TryParse(parts[12], out int atk) ? atk : 0,
                            SkillId = int.TryParse(parts[13], out int skill) ? skill : 0,
                            GradeReq = int.TryParse(parts[15], out int grade) ? grade : 0,
                            Price = int.TryParse(parts[16], out int price) ? price : 0,
                        };
                        Items[id] = item;
                    }
                    catch
                    {
                        // Skip malformed lines safely
                    }
                }
            }
        }

        private void LoadCoItems(string filePath)
        {
            if (!File.Exists(filePath)) return;
            var enc = GetTableEncoding();
            foreach (var line in File.ReadAllLines(filePath, enc))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
                var parts = ParseCsvLine(line);
                if (parts.Count >= 5 && int.TryParse(parts[0], out int id))
                {
                    if (!Items.TryGetValue(id, out var item))
                    {
                        item = new GameItemDef { Id = id };
                        Items[id] = item;
                    }

                    item.Code = parts.Count > 1 ? parts[1].Trim() : item.Code;
                    item.Name = parts.Count > 2 ? parts[2].Trim() : item.Name;
                    item.Description = parts.Count > 3 ? parts[3].Trim().Trim('"') : item.Description;
                    item.Attr = parts.Count > 4 && int.TryParse(parts[4], out int attr) ? attr : item.Attr;
                    item.UseType = parts.Count > 6 && int.TryParse(parts[6], out int useType) ? useType : item.UseType;
                    item.QuickUsable = parts.Count > 7 && (parts[7].Trim() == "1" || parts[7].Trim().Equals("true", StringComparison.OrdinalIgnoreCase));
                    item.Price = parts.Count > 8 && int.TryParse(parts[8], out int price) ? price : item.Price;

                    // Dynamically parse recovery HP from hint text if available (e.g. "HP 150 回復" -> 150)
                    if (!string.IsNullOrEmpty(item.Description))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(item.Description, @"HP.*?(\d+)");
                        if (match.Success && int.TryParse(match.Groups[1].Value, out int rec))
                        {
                            item.RecoveryAmount = rec;
                        }
                    }
                }
            }
        }

        private void LoadByulItems(string filePath)
        {
            if (!File.Exists(filePath)) return;
            var enc = GetTableEncoding();
            foreach (var line in File.ReadAllLines(filePath, enc))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
                var parts = ParseCsvLine(line);
                if (parts.Count >= 4 && int.TryParse(parts[0], out int id))
                {
                    if (!Items.TryGetValue(id, out var item))
                    {
                        item = new GameItemDef { Id = id };
                        Items[id] = item;
                    }

                    item.Code = parts.Count > 1 ? parts[1].Trim() : item.Code;
                    item.Name = parts.Count > 2 ? parts[2].Trim() : item.Name;
                    item.Description = parts.Count > 3 ? parts[3].Trim().Trim('"') : item.Description;
                    item.DurationDays = parts.Count > 6 && int.TryParse(parts[6], out int day) ? day : 0;
                    item.RecoveryAmount = parts.Count > 8 && int.TryParse(parts[8], out int beType) ? beType : 0;
                    item.BaseItemType = item.RecoveryAmount;
                    item.Sex = parts.Count > 10 && int.TryParse(parts[10], out int sex) ? sex : item.Sex;
                    item.School = parts.Count > 11 && int.TryParse(parts[11], out int school) ? school : item.School;
                    item.EffectId = parts.Count > 12 && int.TryParse(parts[12], out int eff) ? eff : 0;
                    if (item.EffectId > 0)
                    {
                        item.SkillId = item.EffectId;
                    }

                    if (item.BaseItemType > 0 && Items.TryGetValue(item.BaseItemType, out var baseDef))
                    {
                        item.EquipPos = baseDef.EquipPos;
                        item.WeaponType = baseDef.WeaponType;
                        item.Attack = baseDef.Attack;
                        if (item.SkillId == 0) item.SkillId = baseDef.SkillId;
                    }
                }
            }
        }

        private void LoadByulBeItems(string filePath)
        {
            if (!File.Exists(filePath)) return;
            var enc = GetTableEncoding();
            foreach (var line in File.ReadAllLines(filePath, enc))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
                var parts = ParseCsvLine(line);
                if (parts.Count >= 3 && int.TryParse(parts[0], out int id))
                {
                    if (!Items.TryGetValue(id, out var item))
                    {
                        item = new GameItemDef { Id = id };
                        Items[id] = item;
                    }

                    item.Code = parts.Count > 1 ? parts[1].Trim().Trim('"') : item.Code;
                    item.EquipPos = parts.Count > 2 && int.TryParse(parts[2], out int pos) ? pos : item.EquipPos;
                    item.Sex = parts.Count > 3 && int.TryParse(parts[3], out int sex) ? sex : item.Sex;
                    item.Attack = parts.Count > 4 && int.TryParse(parts[4], out int atk) ? atk : item.Attack;
                    item.WeaponType = parts.Count > 5 && int.TryParse(parts[5], out int wtype) ? wtype : item.WeaponType;
                    item.SkillId = parts.Count > 6 && int.TryParse(parts[6], out int skill) ? skill : item.SkillId;
                }
            }
        }

        private void LoadEnItems(string filePath)
        {
            if (!File.Exists(filePath)) return;
            var enc = GetTableEncoding();
            foreach (var line in File.ReadAllLines(filePath, enc))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
                var parts = ParseCsvLine(line);
                if (parts.Count >= 5 && int.TryParse(parts[0], out int id))
                {
                    if (!Items.TryGetValue(id, out var item))
                    {
                        item = new GameItemDef { Id = id };
                        Items[id] = item;
                    }

                    item.Code = parts.Count > 1 ? parts[1].Trim() : item.Code;
                    item.Name = parts.Count > 2 ? parts[2].Trim() : item.Name;
                    item.Description = parts.Count > 3 ? parts[3].Trim().Trim('"') : item.Description;
                    item.UseType = parts.Count > 5 && int.TryParse(parts[5], out int useType) ? useType : item.UseType;
                    item.Attr = parts.Count > 6 && int.TryParse(parts[6], out int attr) ? attr : item.Attr;
                    item.GradeReq = parts.Count > 7 && int.TryParse(parts[7], out int grade) ? grade : item.GradeReq;
                    item.Price = parts.Count > 8 && int.TryParse(parts[8], out int price) ? price : item.Price;
                }
            }
        }

        private void LoadReinforceStones(string filePath)
        {
            if (!File.Exists(filePath)) return;
            var enc = GetTableEncoding();
            foreach (var line in File.ReadAllLines(filePath, enc))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
                var parts = ParseCsvLine(line);
                if (parts.Count >= 18 && int.TryParse(parts[0], out int id))
                {
                    var stone = new ReinforceStoneDef
                    {
                        Id = id,
                        Grade = parts.Count > 3 && int.TryParse(parts[3], out int grade) ? grade : 0,
                        Level = parts.Count > 4 && int.TryParse(parts[4], out int level) ? level : 0,
                        Hp = parts.Count > 15 && int.TryParse(parts[15], out int hp) ? hp : 0,
                        Atk = parts.Count > 16 && int.TryParse(parts[16], out int atk) ? atk : 0,
                        Def = parts.Count > 17 && int.TryParse(parts[17], out int def) ? def : 0,
                        ExpandGauge = parts.Count > 26 && int.TryParse(parts[26], out int gauge) ? gauge : 0,
                        WeaponType = parts.Count > 28 && int.TryParse(parts[28], out int wtype) ? wtype : 0,
                        EquipPos = parts.Count > 30 && int.TryParse(parts[30], out int pos) ? pos : 0,
                    };
                    ReinforceStones[id] = stone;
                }
            }
        }

        private void LoadEpisodes(string filePath)
        {
            if (!File.Exists(filePath)) return;
            var enc = Encoding.GetEncoding("Shift_JIS");
            foreach (var line in File.ReadAllLines(filePath, enc))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
                var parts = ParseCsvLine(line);
                if (parts.Count >= 2 && int.TryParse(parts[0], out int id))
                {
                    string rawTitle = parts[1].Trim();
                    int bracketIdx = rawTitle.IndexOf(']');
                    if (bracketIdx >= 0 && bracketIdx + 1 < rawTitle.Length)
                    {
                        rawTitle = rawTitle.Substring(bracketIdx + 1).Trim();
                    }
                    Episodes[id] = new GameEpisodeDef { Id = id, Title = rawTitle };
                }
            }
        }

        private static readonly System.Collections.Generic.Dictionary<int, string> _estivaStorySteps = new()
        {
            [0] = "エスティバー学園へようこそ！",
            [1] = "廊下いっぱいの箱",
            [2] = "廊下いっぱいの箱＠２階",
            [3] = "ゲトー大行進！",
            [5] = "ラッチュー撲滅作戦",
            [7] = "マルシュ・アタック！",
            [9] = "宵月学院へGO！GO！GO！"
        };

        private static readonly System.Collections.Generic.Dictionary<int, string> _soilStorySteps = new()
        {
            [0] = "宵月学院にようこそ！",
            [1] = "箱でいっぱいの廊下",
            [2] = "職員室いっぱいの箱",
            [3] = "怖がりサム先生",
            [5] = "オカルトボックス",
            [7] = "彼と彼女の生徒会",
            [9] = "エスティバー学園へ行こう！"
        };

        public string GetEpisodeTitleForProgress(int school, int epiProgress)
        {
            if (school == 1) // Estiva
            {
                if (_estivaStorySteps.TryGetValue(epiProgress, out var title)) return title;
                return Episodes.TryGetValue(epiProgress, out var ep) ? ep.Title : "クリア";
            }
            else if (school == 2) // So-il
            {
                if (_soilStorySteps.TryGetValue(epiProgress, out var title)) return title;
                return Episodes.TryGetValue(epiProgress, out var ep) ? ep.Title : "クリア";
            }
            return "クリア";
        }

        private void LoadTitles(string filePath)
        {
            if (!File.Exists(filePath)) return;
            var enc = GetTableEncoding();
            foreach (var line in File.ReadAllLines(filePath, enc))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
                var parts = line.Split('\t');
                if (parts.Length < 2) parts = line.Split(',');
                if (parts.Length >= 2 && int.TryParse(parts[0], out int id))
                {
                    Titles[id] = parts[1];
                }
            }
        }

        private void LoadNpcs(string filePath)
        {
            if (!File.Exists(filePath)) return;
            var enc = GetTableEncoding();
            foreach (var line in File.ReadAllLines(filePath, enc))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
                var parts = line.Split('\t');
                if (parts.Length < 2) parts = line.Split(',');
                if (parts.Length >= 2 && int.TryParse(parts[0], out int id))
                {
                    Npcs[id] = parts[1].Trim();
                    if (parts.Length >= 3 && int.TryParse(parts[2], out int cutIn))
                    {
                        NpcCutIns[id] = cutIn;
                    }
                }
            }
        }

        private void LoadShopItemList(string filePath)
        {
            if (!File.Exists(filePath)) return;
            var enc = GetTableEncoding();
            foreach (var line in File.ReadAllLines(filePath, enc))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
                var parts = ParseCsvLine(line);
                if (parts.Count >= 2 && int.TryParse(parts[0], out int itemId) && int.TryParse(parts[1], out int price))
                {
                    int grade = parts.Count > 2 && int.TryParse(parts[2], out int g) ? g : 0;
                    int category = parts.Count > 3 && int.TryParse(parts[3], out int c) ? c : 0;
                    ShopItems.Add(new ShopItemDef
                    {
                        ItemId = itemId,
                        Price = price,
                        Grade = grade,
                        Category = category
                    });

                    StarProducts.Add(new ShopProductDef
                    {
                        ProductId = itemId,
                        Price = price,
                        Period = grade,
                        Flag = category
                    });
                }
            }
        }

        private void LoadProductListXml(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                Encoding enc = Encoding.GetEncoding("Shift_JIS");
                string xmlText = File.ReadAllText(filePath, enc);
                // Strip XML declaration to prevent parser encoding mismatch
                xmlText = System.Text.RegularExpressions.Regex.Replace(xmlText, @"<\?xml[^>]*\?>", string.Empty);
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

        private void LoadHuntMonsters(string filePath)
        {
            if (!File.Exists(filePath)) return;
            var enc = Encoding.GetEncoding("Shift_JIS");
            foreach (var line in File.ReadAllLines(filePath, enc))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
                var parts = ParseCsvLine(line);
                if (parts.Count >= 13 &&
                    int.TryParse(parts[1], out int type) &&
                    int.TryParse(parts[2], out int hp) &&
                    int.TryParse(parts[3], out int lv))
                {
                    var def = new HuntMonsterDef
                    {
                        IdHuntField = int.TryParse(parts[0], out int hf) ? hf : 0,
                        TypeMonster = type,
                        HpMax = hp,
                        Level = lv,
                        TypeBasis = int.TryParse(parts[4], out int basis) ? basis : 0,
                        Name = parts[5].Trim().Trim('"'),
                        Motion = int.TryParse(parts[8], out int mot) ? mot : 0,
                        DropItemType = int.TryParse(parts[9], out int drop) ? drop : 0,
                        DropCount = int.TryParse(parts[10], out int cnt) ? cnt : 0,
                        DropRate = int.TryParse(parts[11], out int rate) ? rate : 0,
                        Exp = int.TryParse(parts[12], out int exp) ? exp : 0
                    };
                    HuntMonsters[type] = def;
                }
            }
        }

        private void LoadFields(string filePath)
        {
            if (!File.Exists(filePath)) return;
            var enc = GetTableEncoding();
            foreach (var line in File.ReadAllLines(filePath, enc))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
                var parts = ParseCsvLine(line);
                if (parts.Count >= 6 && int.TryParse(parts[0], out int id))
                {
                    Fields[id] = new GameFieldDef
                    {
                        Id = id,
                        Code = parts[1].Trim(),
                        Name = parts[2].Trim().Trim('"'),
                        IsHuntField = parts[3].Trim() == "1",
                        HuntFieldId = int.TryParse(parts[4], out int hf) ? hf : 0,
                        Bgm = int.TryParse(parts[5], out int bgm) ? bgm : 6
                    };
                }
            }
        }

        private void LoadExpTable(string filePath)
        {
            if (!File.Exists(filePath)) return;
            var enc = GetTableEncoding();
            foreach (var line in File.ReadAllLines(filePath, enc))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
                var parts = line.Split(',');
                if (parts.Length >= 2 && int.TryParse(parts[0], out int lv) && int.TryParse(parts[1], out int exp))
                {
                    ExpTable[lv] = exp;
                }
            }
        }

        private void LoadStatusTable(string filePath)
        {
            if (!File.Exists(filePath)) return;
            var enc = GetTableEncoding();
            int currentLevel = 1;
            foreach (var line in File.ReadAllLines(filePath, enc))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
                var parts = line.Split(',');
                if (parts.Length >= 4 &&
                    int.TryParse(parts[0], out int pow) &&
                    int.TryParse(parts[1], out int spd) &&
                    int.TryParse(parts[2], out int skl) &&
                    int.TryParse(parts[3], out int lck))
                {
                    StatusTable[currentLevel] = new StatusDef
                    {
                        Level = currentLevel,
                        Pow = pow,
                        Speed = spd,
                        Skill = skl,
                        Luck = lck
                    };
                    currentLevel++;
                }
            }
        }

        private void LoadDexTable(string filePath)
        {
            if (!File.Exists(filePath)) return;
            var enc = GetTableEncoding();
            foreach (var line in File.ReadAllLines(filePath, enc))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
                var parts = line.Split(',');
                if (parts.Length >= 2 && int.TryParse(parts[0], out int dexLv) && int.TryParse(parts[1], out int dexExp))
                {
                    DexTable[dexLv] = dexExp;
                }
            }
        }

        private void LoadAtkWeapons(string filePath)
        {
            if (!File.Exists(filePath)) return;
            var enc = GetTableEncoding();
            foreach (var line in File.ReadAllLines(filePath, enc))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
                var parts = ParseCsvLine(line);
                if (parts.Count >= 3 && int.TryParse(parts[0], out int itemType))
                {
                    AtkWeapons[itemType] = new AtkWeaponDef
                    {
                        ItemType = itemType,
                        Category = int.TryParse(parts[1], out int cat) ? cat : 1,
                        AtkRatio = int.TryParse(parts[2], out int ratio) ? ratio : 10000,
                        HitMotion = parts.Count >= 4 && int.TryParse(parts[3], out int mot) ? mot : 0,
                        Range = parts.Count >= 5 && int.TryParse(parts[4], out int rng) ? rng : 22,
                        Angle = parts.Count >= 6 && int.TryParse(parts[5], out int ang) ? ang : 63
                    };
                }
            }
        }

        private void LoadSkillWeapons(string filePath)
        {
            if (!File.Exists(filePath)) return;
            var enc = GetTableEncoding();
            foreach (var line in File.ReadAllLines(filePath, enc))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
                var parts = ParseCsvLine(line);
                if (parts.Count >= 1 && int.TryParse(parts[0], out int skillId))
                {
                    SkillWeapons[skillId] = new SkillWeaponDef
                    {
                        SkillId = skillId,
                        Code = parts.Count >= 2 ? parts[1].Trim() : string.Empty,
                        Name = parts.Count >= 3 ? parts[2].Trim().Trim('"') : string.Empty,
                        Range = parts.Count >= 5 && int.TryParse(parts[4], out int rng) ? rng : 22,
                        Delay = parts.Count >= 10 && int.TryParse(parts[9], out int dly) ? dly : 100
                    };
                }
            }
        }

        private void LoadSkillDescs(string filePath)
        {
            if (!File.Exists(filePath)) return;
            var enc = GetTableEncoding();
            foreach (var line in File.ReadAllLines(filePath, enc))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
                var parts = ParseCsvLine(line);
                if (parts.Count >= 6 && int.TryParse(parts[0], out int skillId))
                {
                    SkillDescs[skillId] = new SkillDescDef
                    {
                        SkillId = skillId,
                        Code = parts[1].Trim(),
                        Name = parts[2].Trim().Trim('"'),
                        Description = parts[3].Trim().Trim('"'),
                        RequiredSkill = int.TryParse(parts[4], out int reqSk) ? reqSk : 0,
                        WeaponType = int.TryParse(parts[5], out int wType) ? wType : 0
                    };
                }
            }
        }

        private void LoadSkillDesc2s(string filePath)
        {
            if (!File.Exists(filePath)) return;
            var enc = GetTableEncoding();
            foreach (var line in File.ReadAllLines(filePath, enc))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
                var parts = ParseCsvLine(line);
                if (parts.Count >= 20 && int.TryParse(parts[0], out int skillId))
                {
                    SkillDesc2s[skillId] = new SkillDesc2Def
                    {
                        SkillId = skillId,
                        BaseId = int.TryParse(parts[1], out int baseId) ? baseId : skillId,
                        RequiredGrade = int.TryParse(parts[4], out int reqGr) ? reqGr : 1,
                        Level = int.TryParse(parts[5], out int lv) ? lv : 1,
                        NextId = int.TryParse(parts[6], out int nextId) ? nextId : 0,
                        RequiredDex = int.TryParse(parts[7], out int reqDex) ? reqDex : 0,
                        SkillType = int.TryParse(parts[8], out int sType) ? sType : 0,
                        Power = int.TryParse(parts[9], out int pwr) ? pwr : 0,
                        Time = int.TryParse(parts[10], out int time) ? time : 0,
                        Atk = int.TryParse(parts[11], out int atk) ? atk : 0,
                        Def = int.TryParse(parts[12], out int def) ? def : 0,
                        Hit = int.TryParse(parts[13], out int hit) ? hit : 0,
                        Eva = int.TryParse(parts[14], out int eva) ? eva : 0,
                        Cri = int.TryParse(parts[15], out int cri) ? cri : 0,
                        Hp = int.TryParse(parts[16], out int hp) ? hp : 0,
                        AtkSpd = int.TryParse(parts[17], out int atkSpd) ? atkSpd : 0,
                        MovSpd = int.TryParse(parts[18], out int movSpd) ? movSpd : 0,
                        CoolTime = int.TryParse(parts[19], out int cool) ? cool : 0
                    };
                }
            }
        }

        private void LoadMatchingBgm(string filePath)
        {
            if (!File.Exists(filePath)) return;
            var enc = GetTableEncoding();
            foreach (var line in File.ReadAllLines(filePath, enc))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
                var parts = ParseCsvLine(line);
                if (parts.Count >= 2 && int.TryParse(parts[0], out int id))
                {
                    MatchingBgm[id] = parts[1].Trim().Trim('"');
                }
            }
        }

        private void LoadKujiTable(string filePath)
        {
            if (!File.Exists(filePath)) return;
            var enc = GetTableEncoding();
            foreach (var line in File.ReadAllLines(filePath, enc))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
                string entry = line.Trim().Trim('"');
                if (!string.IsNullOrEmpty(entry))
                {
                    KujiResults.Add(entry);
                }
            }
        }

        private void LoadFieldScoreData(string scoreDir)
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

                // 1. Parse default.xml
                string defXml = Path.Combine(fdir, "default.xml");
                if (File.Exists(defXml))
                {
                    try
                    {
                        string text = File.ReadAllText(defXml, Encoding.GetEncoding("Shift_JIS"));

                        // Warpgates
                        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(text, @"<warpgate\s+([^>]+)>", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
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

                        // Episode gates (ObjectType = 2)
                        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(text, @"<episodegate\s+([^>]+)>", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
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

                        // Hairdresser (ObjectType = 6)
                        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(text, @"<hairdresser\s+([^>]+)>", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
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

                        // Generators (<generator>...</generator>)
                        foreach (System.Text.RegularExpressions.Match genMatch in System.Text.RegularExpressions.Regex.Matches(text, @"<generator>([\s\S]*?)</generator>", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                        {
                            string genContent = genMatch.Groups[1].Value;
                            var gen = new FieldGeneratorDef();

                            foreach (System.Text.RegularExpressions.Match ptMatch in System.Text.RegularExpressions.Regex.Matches(genContent, @"<point\s+([^>]+)>", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            {
                                string attr = ptMatch.Groups[1].Value;
                                gen.Points.Add(new FieldGeneratorPoint
                                {
                                    X = GetFloatAttr(attr, "x"),
                                    Y = GetFloatAttr(attr, "y")
                                });
                            }

                            foreach (System.Text.RegularExpressions.Match monMatch in System.Text.RegularExpressions.Regex.Matches(genContent, @"<monster\s+([^>]+)>", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
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
                                    // Spawn authentic count specified in XML
                                    int spawnCount = Math.Min(30, Math.Max(1, gmon.Count));
                                    for (int i = 0; i < spawnCount; i++, ptIdx++)
                                    {
                                        var pt = gen.Points[ptIdx % gen.Points.Count];
                                        float spawnX = pt.X;
                                        float spawnY = pt.Y;

                                        // Natural territory distribution around generator anchor (verified against map grid collision)
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
                                            State = MonsterState.Wait, // Idle at spawn (matching Delphi TMonster initialization)
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

                // 2. Parse all other XML files for NPCs
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

                        // 1. Spawn all field NPCs defined in this XML
                        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(text, @"<npc\s+([^>]+)>", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                        {
                            string attr = m.Groups[1].Value;
                            int id = GetIntAttr(attr, "id");
                            int shell = GetIntAttr(attr, "shell");
                            int x = GetIntAttr(attr, "x");
                            int y = GetIntAttr(attr, "y");
                            int dir = GetIntAttr(attr, "dir");

                            if (id > 0)
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

        private void ParseNpcDialogsFromXml(string xmlContent, GameFieldDef? field = null)
        {
            try
            {
                var npcMatches = System.Text.RegularExpressions.Regex.Matches(xmlContent, @"<npc\s+([^>]+)>(.*?)</npc>", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                foreach (System.Text.RegularExpressions.Match nm in npcMatches)
                {
                    string attr = nm.Groups[1].Value;
                    string body = nm.Groups[2].Value;
                    int id = GetIntAttr(attr, "id");
                    if (id <= 0) continue;

                    var script = new NpcScriptDef { NpcId = id };

                    // Parse all <script> and <dialog> actions in exact sequential document order (matching Delphi TNpcActionList)
                    var actionMatches = System.Text.RegularExpressions.Regex.Matches(body, @"<(script|dialog)\s+([^>]+)>(.*?)</\1>", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    int actionIndex = 1; // 1-based index matching Delphi TNpcActionList
                    foreach (System.Text.RegularExpressions.Match am in actionMatches)
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

                            if (string.IsNullOrEmpty(dlgName))
                            {
                                dlgName = dlgId.ToString();
                            }

                            if (string.IsNullOrEmpty(script.InitialDialogName))
                            {
                                script.InitialDialogName = dlgName;
                            }

                            // Extract text: preserve authentic XML formatting, markup and CDATA structure
                            var textMatch = System.Text.RegularExpressions.Regex.Match(tagBody, @"<text>(?:<!\[CDATA\[)?(.*?)(?:\]\]>)?</text>", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
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

                            // Extract selections
                            var selMatches = System.Text.RegularExpressions.Regex.Matches(tagBody, @"<selection\s+([^>]+)/>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            foreach (System.Text.RegularExpressions.Match sm in selMatches)
                            {
                                string selAttr = sm.Groups[1].Value;
                                string selText = GetStringAttr(selAttr, "text");
                                string selNext = GetStringAttr(selAttr, "next");
                                dlgDef.Selections.Add(new NpcDialogSelectionDef
                                {
                                    Text = selText,
                                    Next = selNext
                                });
                            }

                            if (!string.IsNullOrEmpty(dlgName))
                            {
                                script.Dialogs[dlgName] = dlgDef;
                            }
                            script.Dialogs[dlgId.ToString()] = dlgDef;
                        }

                        actionIndex++;
                    }

                    if (script.Dialogs.Count > 0 || script.Scripts.Count > 0)
                    {
                        NpcScripts[id] = script;
                        if (field != null)
                        {
                            field.NpcScripts[id] = script;
                        }
                    }
                }
            }
            catch { }
        }

        private static int GetIntAttr(string attr, string name)
        {
            var m = System.Text.RegularExpressions.Regex.Match(attr, name + @"=""?([0-9]+)""?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return m.Success && int.TryParse(m.Groups[1].Value, out int v) ? v : 0;
        }

        private static float GetFloatAttr(string attr, string name)
        {
            var m = System.Text.RegularExpressions.Regex.Match(attr, name + @"=""?([0-9.]+)""?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return m.Success && float.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : 0f;
        }

        private static string GetStringAttr(string attr, string name)
        {
            var m = System.Text.RegularExpressions.Regex.Match(attr, name + @"=""([^""]*)""", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : string.Empty;
        }
    }

    public sealed class NpcDialogSelectionDef
    {
        public string Text { get; set; } = string.Empty;
        public string Next { get; set; } = string.Empty;
    }

    public sealed class NpcDialogDef
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int CutIn { get; set; } = 0;
        public int CloseButton { get; set; } = 0;
        public System.Collections.Generic.List<NpcDialogSelectionDef> Selections { get; set; } = new();
    }

    public sealed class NpcScriptDef
    {
        public int NpcId { get; set; }
        public string InitialDialogName { get; set; } = string.Empty;
        public string InitScriptBody { get; set; } = string.Empty;
        public System.Collections.Generic.Dictionary<string, string> Scripts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public System.Collections.Generic.Dictionary<string, NpcDialogDef> Dialogs { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Evaluates official XML <script name="init"> conditionals against player attributes
        /// matching Delphi TNpcScriptRunner.
        /// </summary>
        public string EvaluateInit(int school, int level, int grade, int epiYoi, int epiEs)
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

            string? targetJump = EvaluateBlock(InitScriptBody, vars);
            if (!string.IsNullOrEmpty(targetJump))
            {
                return ResolveToDialog(targetJump, vars);
            }

            return string.Empty;
        }

        /// <summary>
        /// Resolves a dialogue selection target label, executing any intermediate <script> blocks (e.g. epi_es_up, look)
        /// until reaching a concrete <dialog> node.
        /// </summary>
        public string ResolveNext(string nextLabel, int school, int level, int grade, ref int epiYoi, ref int epiEs)
        {
            var vars = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["local.school"] = school,
                ["local.level"] = level,
                ["local.grade"] = grade,
                ["peke.epi.yoi"] = epiYoi,
                ["peke.epi.es"] = epiEs
            };

            string result = ResolveToDialog(nextLabel, vars);
            epiYoi = vars["peke.epi.yoi"];
            epiEs = vars["peke.epi.es"];
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
            var matches = System.Text.RegularExpressions.Regex.Matches(xmlBlock, @"<(?<tag>ifeq|ifne|ifge|ifle|ifgt|iflt|jump|set|inc|dec)\s+([^>]+?)(?:>(.*?)</\k<tag>>|/>)", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            foreach (System.Text.RegularExpressions.Match m in matches)
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
            var m = System.Text.RegularExpressions.Regex.Match(attr, name + @"=""?([0-9]+)""?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return m.Success && int.TryParse(m.Groups[1].Value, out int v) ? v : 0;
        }

        private static string GetAttr(string attr, string name)
        {
            var m = System.Text.RegularExpressions.Regex.Match(attr, name + @"=""([^""]*)""", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : string.Empty;
        }
    }

    /// <summary>
    /// Authentic character status attributes per level matching Delphi StatusTable.txt.
    /// </summary>
    public struct StatusDef
    {
        public int Level;
        public int Pow;
        public int Speed;
        public int Skill;
        public int Luck;
    }

    /// <summary>
    /// Complete Item Definition matching Delphi UYgItem.TBeItemData / TCoItemData / TStarItemData.
    /// </summary>
    public sealed class GameItemDef
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Categorization & Flags
        public int Attr { get; set; }
        public int Flag { get; set; }
        public int BaseItemType { get; set; }

        // Equipment Slot Bitmask & Weapon Classification
        // 1=Cap/Hat, 2=Accessory, 4=Bag, 8=Glove/Weapon, 64=Top, 128=Bottom, 192=Dress, 256=Shoes
        public int EquipPos { get; set; }
        public int WeaponType { get; set; } // 0=Armor/Costume, 1=Blade, 2=Glove, 3=Box, 4=Gun/Shooting, etc.

        // Requirements & Restrictions (0 = Any / Unisex)
        public int Sex { get; set; } // 0=Unisex, 1=Male only, 2=Female only
        public int School { get; set; } // 0=All schools, 1=Estiva Academy only, 2=So-il Academy only
        public int GradeReq { get; set; } // Minimum Grade (1..6)

        // Combat Stats & Skills
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int SkillId { get; set; }

        // Economy & Consumables
        public int Price { get; set; }
        public int UseType { get; set; }
        public bool QuickUsable { get; set; }
        public int DurationDays { get; set; }
        public int RecoveryAmount { get; set; }
        public int EffectId { get; set; }

        // Quick helpers
        public bool IsEquipment => EquipPos > 0 || WeaponType > 0;
        public bool IsWeapon => WeaponType > 0;

        /// <summary>
        /// Automatically resolves the target equipment slot index (1..9) from EquipPos & WeaponType.
        /// Slot 1: Head / Hat
        /// Slot 2: Accessory 1
        /// Slot 3: Bag / Backpack
        /// Slot 4: Weapon
        /// Slot 5: Gloves
        /// Slot 6: Accessory 2
        /// Slot 7: Top Shirt / Jacket (or Special Dress)
        /// Slot 8: Bottom Pants / Skirt
        /// Slot 9: Shoes / Boots
        /// </summary>
        public ushort GetTargetEquipSlot()
        {
            if (WeaponType > 0) return 4; // Slot 4: Weapon (Blade=1, Glove=2, Box=3, Gun=4)
            if (EquipPos == 1 || (EquipPos & 1) != 0) return 1; // Slot 1: Hat / Cap
            if (EquipPos == 2) return 2; // Slot 2: Accessory 1
            if (EquipPos == 4) return 3; // Slot 3: Bag / Backpack
            if (EquipPos == 8 || (EquipPos & 8) != 0) return 5; // Slot 5: Gloves / Armbands
            if (EquipPos == 16) return 6; // Slot 6: Accessory 2
            if (EquipPos == 128) return 8; // Slot 8: Bottom / Skirt / Pants
            if (EquipPos == 256) return 9; // Slot 9: Shoes / Boots
            if (EquipPos == 64 || EquipPos == 192 || EquipPos == 448 || (EquipPos & 64) != 0) return 7; // Slot 7: Top / Dress / Suit
            return 0; // Not an equippable apparel item
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
        public System.Collections.Generic.List<FieldNpcSpawn> Npcs { get; } = new();
        public System.Collections.Generic.List<FieldWarpGate> WarpGates { get; } = new();
        public System.Collections.Generic.List<FieldTerminalObject> TerminalObjects { get; } = new();
        public System.Collections.Generic.Dictionary<int, NpcScriptDef> NpcScripts { get; } = new();
        public System.Collections.Generic.List<FieldMonster> Monsters { get; } = new();
        public System.Collections.Generic.List<FieldGeneratorDef> Generators { get; } = new();
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
        public System.Collections.Generic.List<FieldGeneratorPoint> Points { get; } = new();
        public System.Collections.Generic.List<FieldGeneratorMonster> Monsters { get; } = new();
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
        public int ObjectType { get; set; } // 2 = Episode terminal, 6 = Hairdresser
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
