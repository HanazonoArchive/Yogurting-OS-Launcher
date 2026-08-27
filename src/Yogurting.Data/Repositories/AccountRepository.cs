using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Yogurting.Core.Models;

namespace Yogurting.Data.Repositories
{
    public interface IAccountRepository
    {
        Task<Player?> GetAccountAsync(string accountId);
        Task<Player?> GetByUsernameAsync(string username);
        Task<Player?> GetByCharacterNameAsync(string characterName);
        Task<Player> CreateAccountAsync(string accountId, string password, string characterName, SchoolType school, GenderType gender, WeaponClass weapon);
        Task SaveAccountAsync(Player player);
        Task SaveAsync(Player player);
        Task SaveAllAsync();
    }

    /// <summary>
    /// Thread-safe Account & Character Repository with dual XML/JSON persistence.
    /// Exactly compatible with the Yogurting account structure.
    /// </summary>
    public sealed class JsonAccountRepository : IAccountRepository
    {
        private readonly string _storageDir;
        private readonly ConcurrentDictionary<string, Player> _cache = new(StringComparer.OrdinalIgnoreCase);

        public JsonAccountRepository(string storageDir)
        {
            _storageDir = storageDir;
            if (!Directory.Exists(_storageDir))
            {
                Directory.CreateDirectory(_storageDir);
            }

            LoadAllExistingAccounts();
            EnsureDefaultAccount();
        }

        private void LoadAllExistingAccounts()
        {
            try
            {
                foreach (var file in Directory.GetFiles(_storageDir, "*.json"))
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        var player = JsonSerializer.Deserialize<Player>(json);
                        if (player != null && !string.IsNullOrEmpty(player.AccountId))
                        {
                            _cache[player.AccountId] = player;
                        }
                    }
                    catch { }
                }

                // Also check for legacy XML / sqlite files if available
                foreach (var xmlFile in Directory.GetFiles(_storageDir, "*.xml"))
                {
                    try
                    {
                        var player = ParseFromXml(File.ReadAllText(xmlFile));
                        if (player != null && !_cache.ContainsKey(player.AccountId))
                        {
                            _cache[player.AccountId] = player;
                        }
                    }
                    catch { }
                }

                Console.WriteLine($"[AccountRepository] Loaded {_cache.Count} accounts from '{_storageDir}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AccountRepository] Error reading accounts: {ex.Message}");
            }
        }

        private void EnsureDefaultAccount()
        {
            if (!_cache.ContainsKey("test"))
            {
                var testPlayer = new Player
                {
                    AccountId = "test",
                    CharacterName = "Hanazono",
                    PasswordHash = ComputeMd5("test"),
                    School = SchoolType.EstivaAcademy,
                    Gender = GenderType.Female,
                    FaceId = 31,
                    HairId = 103,
                    SkinTone = 2000
                };

                StarterConfigLoader.ApplyDefaultStats(testPlayer);
                StarterConfigLoader.ApplyToPlayer(testPlayer);
                _cache["test"] = testPlayer;
                SaveAccountDirect(testPlayer);
            }
        }

        public static void PopulateStarterInventory(Player player)
        {
            StarterConfigLoader.ApplyToPlayer(player);
        }

        public Task<Player?> GetAccountAsync(string accountId)
        {
            _cache.TryGetValue(accountId, out var player);
            return Task.FromResult(player);
        }

        public Task<Player> CreateAccountAsync(string accountId, string password, string characterName, SchoolType school, GenderType gender, WeaponClass weapon)
        {
            var player = new Player(accountId, characterName, school, gender)
            {
                PasswordHash = ComputeMd5(password),
                FieldId = school == SchoolType.EstivaAcademy ? 1 : 90
            };

            PopulateStarterInventory(player);
            _cache[accountId] = player;
            SaveAccountDirect(player);
            return Task.FromResult(player);
        }

        public Task<Player?> GetByUsernameAsync(string username) => GetAccountAsync(username);

        public Task<Player?> GetByCharacterNameAsync(string characterName)
        {
            foreach (var player in _cache.Values)
            {
                if (string.Equals(player.CharacterName, characterName, StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult<Player?>(player);
                }
            }
            return Task.FromResult<Player?>(null);
        }

        public Task SaveAccountAsync(Player player)
        {
            _cache[player.AccountId] = player;
            SaveAccountDirect(player);
            return Task.CompletedTask;
        }

        public Task SaveAsync(Player player) => SaveAccountAsync(player);

        public Task SaveAllAsync()
        {
            foreach (var player in _cache.Values)
            {
                SaveAccountDirect(player);
            }
            return Task.CompletedTask;
        }

        private void SaveAccountDirect(Player player)
        {
            try
            {
                string jsonPath = Path.Combine(_storageDir, $"{player.AccountId}.json");
                string json = JsonSerializer.Serialize(player, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(jsonPath, json);

                // Also save XML representation for native compatibility
                string xmlPath = Path.Combine(_storageDir, $"{player.AccountId}.xml");
                string xml = GenerateXml(player);
                File.WriteAllText(xmlPath, xml, Encoding.Unicode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AccountRepository] Save error for '{player.AccountId}': {ex.Message}");
            }
        }

        private static string GenerateXml(Player p)
        {
            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-16", null),
                new XElement("Account",
                    new XElement("AccountID", p.AccountId),
                    new XElement("Password", p.PasswordHash),
                    new XElement("HasChara", p.HasCharacter.ToString().ToLower()),
                    new XElement("Name", p.CharacterName),
                    new XElement("TelNumber", p.TelNumber),
                    new XElement("Sex", (int)p.Gender),
                    new XElement("School", (int)p.School),
                    new XElement("Face", p.FaceId),
                    new XElement("Hair", p.HairId),
                    new XElement("Skin", p.SkinTone),
                    new XElement("BirthMonth", p.BirthMonth),
                    new XElement("BirthDay", p.BirthDay),
                    new XElement("BloodType", p.BloodType),
                    new XElement("AuthType", p.AuthType),
                    new XElement("FieldID", p.FieldId),
                    new XElement("X", (int)p.Position.X),
                    new XElement("Y", (int)p.Position.Y),
                    new XElement("SaveFieldID", p.SaveFieldId),
                    new XElement("SaveX", (int)p.SavePosition.X),
                    new XElement("SaveY", (int)p.SavePosition.Y),
                    new XElement("Equips"),
                    new XElement("BeItems")
                )
            );

            return doc.ToString();
        }

        private static Player? ParseFromXml(string xmlContent)
        {
            try
            {
                var doc = XDocument.Parse(xmlContent);
                var root = doc.Element("Account");
                if (root == null) return null;

                var player = new Player
                {
                    AccountId = root.Element("AccountID")?.Value ?? "unknown",
                    PasswordHash = root.Element("Password")?.Value ?? "",
                    CharacterName = root.Element("Name")?.Value ?? "Student",
                    TelNumber = root.Element("TelNumber")?.Value ?? "3456",
                    Gender = (GenderType)int.Parse(root.Element("Sex")?.Value ?? "2"),
                    School = (SchoolType)int.Parse(root.Element("School")?.Value ?? "1"),
                    FaceId = int.Parse(root.Element("Face")?.Value ?? "31"),
                    HairId = int.Parse(root.Element("Hair")?.Value ?? "103"),
                    SkinTone = int.Parse(root.Element("Skin")?.Value ?? "2000"),
                    FieldId = int.Parse(root.Element("FieldID")?.Value ?? "386"),
                    Position = new Position(
                        float.Parse(root.Element("X")?.Value ?? "38"),
                        float.Parse(root.Element("Y")?.Value ?? "14")
                    ),
                    SaveFieldId = int.Parse(root.Element("SaveFieldID")?.Value ?? "90"),
                    SavePosition = new Position(
                        float.Parse(root.Element("SaveX")?.Value ?? "124"),
                        float.Parse(root.Element("SaveY")?.Value ?? "165")
                    )
                };

                return player;
            }
            catch
            {
                return null;
            }
        }

        private static string ComputeMd5(string input)
        {
            using var md5 = MD5.Create();
            byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder();
            foreach (byte b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
