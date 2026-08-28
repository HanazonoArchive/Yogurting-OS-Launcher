using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Yogurting.Core.Logging;
using Yogurting.Core.Models;
using Yogurting.Core.Network;
using Yogurting.Data.Loaders;
using Yogurting.Data.Repositories;

namespace Yogurting.Server.Handlers.Field
{
    /// <summary>
    /// Handles Interactive NPC Dialogue Engine, NPC Facility Shops, Hair Salon & Beauty Parlor, and Storage.
    /// Exact 1-to-1 match with Delphi TChara / TNpcScript / TMsgGameExNpcDialogNtf (0x5229 / 0x522C / 0x793B / 0x793C / 0x793F / 0x526D / 0x526E).
    /// </summary>
    public sealed class NpcAndDialogueHandlers
    {
        private readonly GameDatabase? _gameDb;
        private readonly IAccountRepository _repository;
        private readonly Func<PlayerSessionState, byte[], Task> _broadcastDelegate;

        public NpcAndDialogueHandlers(GameDatabase? gameDb, IAccountRepository repository, Func<PlayerSessionState, byte[], Task> broadcastDelegate)
        {
            _gameDb = gameDb;
            _repository = repository;
            _broadcastDelegate = broadcastDelegate;
        }

        /// <summary>
        /// 0x793B (31035): MsgGameExNpcDialogReq - Player clicks/talks to NPC
        /// Delphi TMsgGameExNpcDialogReq: ReadInt32(NpcId)
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameExNpcDialogReq)]
        public async Task HandleNpcDialogReqAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                var player = state.Player;
                if (player == null || packetData.Length < 10) return;

                int npcId = BitConverter.ToInt32(packetData, 6);
                state.ActiveNpcId = npcId;
                state.ActiveDialogId = 1;

                string npcName = _gameDb != null && _gameDb.Npcs.TryGetValue(npcId, out var name) ? name : $"NPC #{npcId}";
                Logger.Info($"[FieldServer] '{player.CharacterName}' opened dialogue with '{npcName}' (ID: {npcId})");

                // Check for dynamic XML-driven script from official database
                if (_gameDb != null && _gameDb.NpcScripts.TryGetValue(npcId, out var script) && script.Dialogs.Count > 0)
                {
                    NpcDialogDef? initialDlg = null;
                    if (!string.IsNullOrEmpty(script.InitialDialogName) && script.Dialogs.TryGetValue(script.InitialDialogName, out var d))
                    {
                        initialDlg = d;
                    }
                    else
                    {
                        initialDlg = script.Dialogs.Values.FirstOrDefault();
                    }

                    if (initialDlg != null)
                    {
                        state.CurrentNpcDialogNode = initialDlg.Name;
                        var choices = initialDlg.Selections.Select(s => s.Text).ToList();
                        if (choices.Count == 0)
                        {
                            choices.Add("…またね (Farewell)");
                        }

                        byte[] dialogNtf = YogurtingPackets.MakeGameExNpcDialogNtf(npcId, 1, initialDlg.CutIn, initialDlg.Text, choices);
                        await state.Session.SendAsync(dialogNtf);
                        return;
                    }
                }

                // Fallback for special NPCs (Store Auntie / Salon) or unscripted NPCs
                var (dialogText, fallbackChoices) = GetNpcGreetingAndChoices(npcId, npcName, player);
                byte[] fallbackNtf = YogurtingPackets.MakeGameExNpcDialogNtf(npcId, 1, 0, dialogText, fallbackChoices);
                await state.Session.SendAsync(fallbackNtf);
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] HandleNpcDialogReq error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x5228 (21032): MsgNpcDialogActionReq - Talk to NPC / Touch Trigger
        /// </summary>
        [PacketHandler(PacketOpcode.MsgNpcDialogActionReq)]
        public async Task HandleNpcDialogActionReqAsync(PlayerSessionState state, byte[] packetData)
        {
            await HandleNpcDialogReqAsync(state, packetData);
        }

        /// <summary>
        /// 0x522C (21036): MsgNpcDialogSelectReq - Player selects dialogue option
        /// Exact layout from 21036.dms & Delphi _Unit67.pas:006BFA5A:
        ///   ReadInt32(DialogId)
        ///   ReadUInt32(SelectionIndex | 0x80000000)
        ///   ReadInt32(QuestId)
        /// </summary>
        [PacketHandler(PacketOpcode.MsgNpcDialogSelectReq)]
        [PacketHandler(PacketOpcode.MsgGameExNpcDialogSelectNtf)]
        [PacketHandler(PacketOpcode.MsgGameExNpcDialogSelectReq)]
        public async Task HandleNpcDialogSelectReqAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                var player = state.Player;
                if (player == null || packetData.Length < 14) return;

                int dialogId = BitConverter.ToInt32(packetData, 6);
                uint rawSelection = BitConverter.ToUInt32(packetData, 10);
                int selectedIndex = (int)(rawSelection & 0x7FFFFFFF);
                int questId = packetData.Length >= 18 ? BitConverter.ToInt32(packetData, 14) : 0;

                int npcId = state.ActiveNpcId > 0 ? state.ActiveNpcId : 1;
                Logger.Info($"[FieldServer] '{player.CharacterName}' selected option [{selectedIndex}] (Quest #{questId}) for NPC #{npcId} (Dialog #{dialogId}, Node '{state.CurrentNpcDialogNode}')");

                // Handle NPC Facility Actions
                switch (npcId)
                {
                    case 4 or 27: // 購買おばちゃん (Store Auntie / School Canteen)
                        if (selectedIndex == 1) // Choice 1: "Browse Store Goods"
                        {
                            await OpenNpcShopAsync(state, npcId);
                            return;
                        }
                        break;

                    case 7: // メイ (Hairdresser / Salon)
                        if (selectedIndex == 1) // Choice 1: "Change Hairstyle"
                        {
                            await OpenHairShopAsync(state);
                            return;
                        }
                        break;
                }

                // Close Dialogue on Close button ([X] = 101 or 102)
                if (selectedIndex == 101 || selectedIndex == 102)
                {
                    byte[] eventNtf = YogurtingPackets.MakeGameNpcDialogEventNtf(npcId, 1);
                    await state.Session.SendAsync(eventNtf);

                    byte[] endNtf = YogurtingPackets.MakeGameNpcDialogEndNtf();
                    await state.Session.SendAsync(endNtf);
                    return;
                }

                // Check dynamic XML dialogue tree transitions
                if (_gameDb != null && _gameDb.NpcScripts.TryGetValue(npcId, out var script) &&
                    script.Dialogs.TryGetValue(state.CurrentNpcDialogNode, out var currentDlg))
                {
                    int selIdx = selectedIndex - 1; // 1-indexed to 0-indexed
                    if (selIdx >= 0 && selIdx < currentDlg.Selections.Count)
                    {
                        var selection = currentDlg.Selections[selIdx];
                        if (!string.IsNullOrEmpty(selection.Next) && script.Dialogs.TryGetValue(selection.Next, out var nextDlg))
                        {
                            state.CurrentNpcDialogNode = nextDlg.Name;
                            var nextChoices = nextDlg.Selections.Select(s => s.Text).ToList();
                            if (nextChoices.Count == 0)
                            {
                                nextChoices.Add("…またね (Farewell)");
                            }

                            byte[] nextDialogNtf = YogurtingPackets.MakeGameExNpcDialogNtf(npcId, 2, nextDlg.CutIn, nextDlg.Text, nextChoices);
                            await state.Session.SendAsync(nextDialogNtf);
                            return;
                        }
                    }
                }

                // Dialogue ended / farewell chosen -> Send 0x793E and 0x7940 to close and restore mobility
                byte[] closeEventNtf = YogurtingPackets.MakeGameNpcDialogEventNtf(npcId, 1);
                await state.Session.SendAsync(closeEventNtf);

                byte[] closeEndNtf = YogurtingPackets.MakeGameNpcDialogEndNtf();
                await state.Session.SendAsync(closeEndNtf);
            }
            catch (Exception ex)
            {
                Logger.Error($"[FieldServer] HandleNpcDialogSelectReq error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x526D / 0x526E: Hair Shop Salon Menu & Change Handler
        /// </summary>
        public async Task OpenHairShopAsync(PlayerSessionState state)
        {
            byte[] hairShopNtf = YogurtingPackets.MakeGameHairShopEnterNtf();
            await state.Session.SendAsync(hairShopNtf);
        }

        /// <summary>
        /// 0x793E / 0x793F: Opens the Campus Shop Item Catalog
        /// </summary>
        public async Task OpenNpcShopAsync(PlayerSessionState state, int npcId)
        {
            using var writer = PacketWriter.Create((PacketOpcode)0x793F);
            writer.WriteInt32(npcId);

            var items = _gameDb != null ? _gameDb.ShopItems : new List<ShopItemDef>();
            writer.WriteUInt16((ushort)items.Count);

            foreach (var item in items)
            {
                int compositeId = item.ItemId | (item.Grade << 24);
                writer.WriteInt32(compositeId);
                writer.WriteInt32(item.Price);
                writer.WriteInt32(item.Category);
                writer.WriteInt32(1); // Default quantity
            }

            await state.Session.SendAsync(writer.Build());
        }

        private static (string Text, List<string> Choices) GetNpcGreetingAndChoices(int npcId, string npcName, Player player)
        {
            return npcId switch
            {
                4 or 27 => (
                    $"Hello there {player.CharacterName}! Welcome to the School Store. Need any drinks, snacks, or stationery?",
                    new List<string> { "Browse Store Goods", "School Information", "Goodbye" }
                ),
                7 => (
                    $"Welcome to the Salon, {player.CharacterName}! Looking for a stylish new cut or fresh color today?",
                    new List<string> { "Change Hairstyle", "Styling Advice", "Maybe Next Time" }
                ),
                _ => (
                    $"Hello {player.CharacterName}! Good luck with your studies today!",
                    new List<string> { "Talk", "…またね (Farewell)" }
                )
            };
        }
    }
}
