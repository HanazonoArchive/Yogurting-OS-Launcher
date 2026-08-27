using System;
using System.Text;
using System.Threading.Tasks;
using Yogurting.Core.Logging;
using Yogurting.Core.Models;
using Yogurting.Core.Network;

namespace Yogurting.Server.Handlers.Npc
{
    /// <summary>
    /// Handles NPC dialogue windows, cut-in portraits, store inventories, and Byul Star shops.
    /// </summary>
    public sealed class NpcAndShopHandlers
    {
        private readonly Yogurting.Data.Loaders.GameDatabase? _gameDb;

        public NpcAndShopHandlers(Yogurting.Data.Loaders.GameDatabase? gameDb = null)
        {
            _gameDb = gameDb;
        }

        /// <summary>
        /// 0x5228 (21032): MsgNpcDialogActionReq - Talk to NPC / Touch Trigger
        /// </summary>
        [PacketHandler(PacketOpcode.MsgNpcDialogActionReq)]
        public async Task HandleNpcTouchAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                int npcId = packetData.Length >= 10 ? BitConverter.ToInt32(packetData, 6) : 1;
                Logger.Info($"[FieldServer] '{state.Player.CharacterName}' interacted with NPC ID: {npcId}");

                string npcName = (_gameDb != null && _gameDb.Npcs.TryGetValue(npcId, out var dbName) && !string.IsNullOrWhiteSpace(dbName))
                    ? dbName
                    : "Campus Staff";
                string greeting = "Welcome to campus! How can I help you today?";
                string[] choices = { "1. Browse Store", "2. Campus Information", "3. Nevermind" };

                switch (npcId)
                {
                    case 1:
                        npcName = "Principal";
                        greeting = "Study hard and protect the academy from the Endless Vacation!";
                        choices = new[] { "1. Promotion Exam", "2. School History", "3. Farewell" };
                        break;
                    case 2:
                        npcName = "Teacher";
                        greeting = "Are you ready for your next lesson?";
                        choices = new[] { "1. Start Class", "2. Ask Questions", "3. Exit" };
                        break;
                    case 3:
                        npcName = "Nurse";
                        greeting = "You look tired! Let me heal your wounds.";
                        state.Player.Hp = state.Player.MaxHp;
                        state.Player.Sp = state.Player.MaxSp;
                        await state.Session.SendAsync(YogurtingPackets.MakeGameStatDeltaNtf());
                        await state.Session.SendAsync(YogurtingPackets.MakeGameSetStateNtf(state.Player));
                        choices = new[] { "1. Full Recovery Done!", "2. Bye" };
                        break;
                    case 4:
                        npcName = "Store Auntie";
                        greeting = "Welcome to the Store! Hungry? Try our fresh red bean bread and strawberry milk!";
                        choices = new[] { "1. Open Food Store", "2. Chat", "3. Exit" };
                        break;
                    case 5:
                        npcName = "Locker Guy";
                        greeting = "Need to store extra items in your school locker?";
                        choices = new[] { "1. Open Locker", "2. Locker Rules", "3. Close" };
                        break;
                    case 6:
                        npcName = "Johnny";
                        greeting = "Yo! Looking for starter weapons and blade upgrades?";
                        choices = new[] { "1. Weapon Store", "2. Weapon Maintenance", "3. Later" };
                        break;
                    case 7:
                        npcName = "Librarian";
                        greeting = "Shh! The library is a place for quiet study and research.";
                        choices = new[] { "1. Episode Records", "2. Academy Lore", "3. Exit" };
                        break;
                    case 14:
                        npcName = "Campus Guard";
                        greeting = "Campus security is on high alert. Stay safe, student!";
                        choices = new[] { "1. Incident Reports", "2. Lost & Found", "3. Dismiss" };
                        break;
                }

                // 1. Send Rich Interactive Dialog Window with CutIn portrait
                byte[] npcDialog = YogurtingPackets.MakeGameNpcDialogNtf(
                    npcId: npcId,
                    dialogId: 100,
                    dialogText: $"<font color=\"#FFEE88\"><b>[{npcName}]</b></font><br>{greeting}",
                    choices: choices,
                    cutInCategory: 1
                );
                await state.Session.SendAsync(npcDialog);

                // 2. If Store NPC, send store catalog
                if (npcId == 4)
                {
                    var products = new (byte category, int itemId)[]
                    {
                        (3, 200001), // Red Bean Bread
                        (3, 200002), // Melon Bread
                        (3, 200003), // Toast Bread
                        (3, 200011), // Strawberry Milk
                        (3, 200012), // Banana Milk
                        (3, 200013), // Yogurt 70
                    };
                    await state.Session.SendAsync(YogurtingPackets.MakeGameShopListNtf(1, products));
                }
                else if (npcId == 6)
                {
                    var products = new (byte category, int itemId)[]
                    {
                        (1, 110001), // Starter Blade
                        (1, 110002), // Iron Blade
                        (1, 120001), // Boxing Glove
                        (1, 130001), // Warm Muffler
                        (1, 140001), // Training Pistol
                    };
                    await state.Session.SendAsync(YogurtingPackets.MakeGameShopListNtf(1, products));
                }

                // 3. Broadcast NPC Voice Chat to Area
                byte[] npcChat = YogurtingPackets.MakeGameChatNtf(npcId, npcName, greeting, 0);
                await state.Session.SendAsync(npcChat);
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] NPC Touch error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x522C (21036): MsgNpcDialogSelectReq - Player selected a dialogue option
        /// </summary>
        [PacketHandler(PacketOpcode.MsgNpcDialogSelectReq)]
        public async Task HandleNpcDialogSelectAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                int dialogId = packetData.Length >= 10 ? BitConverter.ToInt32(packetData, 6) : 0;
                int choiceIndex = packetData.Length >= 14 ? BitConverter.ToInt32(packetData, 10) : 0;
                Logger.Info($"[FieldServer] '{state.Player.CharacterName}' selected NPC dialogue option {choiceIndex} (Dialog {dialogId}).");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] HandleNpcDialogSelect error: {ex.Message}");
            }
        }
    }
}
