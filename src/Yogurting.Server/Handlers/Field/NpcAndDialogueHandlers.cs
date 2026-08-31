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
                int currentFieldId = state.Player.FieldId;

                // 1. Check if NPC has an active dialogue script attached in this map
                NpcScriptDef? script = null;
                if (_gameDb != null && _gameDb.Fields.TryGetValue(currentFieldId, out var curField))
                {
                    curField.NpcScripts.TryGetValue(npcId, out script);
                }

                if (script == null && _gameDb != null && (npcId == 600 || npcId == 486))
                {
                    _gameDb.NpcScripts.TryGetValue(npcId, out script);
                }

                if (script != null && (script.Dialogs.Count > 0 || script.Scripts.Count > 0))
                {
                    player.ScriptVariables ??= new(StringComparer.OrdinalIgnoreCase);
                    // Evaluate authentic XML init script conditionals dynamically against live player stats & episode progress
                    string initialNode = script.EvaluateInit((int)player.School, player.Level, player.Grade, player.EpisodeYoi, player.EpisodeEs, player.ScriptVariables);

                    if (!string.IsNullOrEmpty(initialNode) && script.Dialogs.TryGetValue(initialNode, out var initialDlg))
                    {
                        state.CurrentNpcDialogNode = initialDlg.Name;
                        var choices = initialDlg.Selections.Select(s => s.Text).ToList();

                        string tempEs = _gameDb?.GetEpisodeTitleForProgress(1, player.EpisodeEs) ?? "クリア";
                        string tempYoi = _gameDb?.GetEpisodeTitleForProgress(2, player.EpisodeYoi) ?? "クリア";

                        string formattedText = initialDlg.Text
                            .Replace("${local.grade}", player.Grade.ToString())
                            .Replace("${local.level}", player.Level.ToString())
                            .Replace("${local.name}", player.CharacterName)
                            .Replace("${peke.epi.es}", player.EpisodeEs.ToString())
                            .Replace("${peke.epi.yoi}", player.EpisodeYoi.ToString())
                            .Replace("${peke.temp}", tempEs)
                            .Replace("${peke.temp2}", tempYoi);

                        int startingDialogId = initialDlg.Id > 0 ? initialDlg.Id : (int.TryParse(initialDlg.Name, out int parsedId) ? parsedId : 2);
                        byte[] dialogNtf = YogurtingPackets.MakeGameExNpcDialogNtf(npcId, startingDialogId, initialDlg.CutIn, formattedText, choices, initialDlg.CloseButton);
                        await state.Session.SendAsync(dialogNtf);
                        Logger.Info($"[FieldServer] '{player.CharacterName}' opened dialogue with '{npcName}' (ID: {npcId}) Dialog #{startingDialogId} Node='{initialDlg.Name}' on Field {currentFieldId}");
                        return;
                    }
                }

                // 2. Ambient / Unscripted NPC -> Dismiss immediately via 0x7940 (matching Delphi Quartet _Unit67.pas:006C2663)
                Logger.Info($"[FieldServer] '{player.CharacterName}' interacted with unscripted entity '{npcName}' (ID: #{npcId}) on Field {currentFieldId} -> Dismissing via 0x7940");
                byte[] cancelAmbientNtf = YogurtingPackets.MakeGameNpcDialogEndNtf();
                await state.Session.SendAsync(cancelAmbientNtf);
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

                // Close Dialogue on Close button ([X] = 101 or 102) -> Send ONLY 0x793E matching Delphi Quartet
                if (selectedIndex == 101 || selectedIndex == 102)
                {
                    byte[] eventNtf = YogurtingPackets.MakeGameNpcDialogEventNtf(npcId, 1);
                    await state.Session.SendAsync(eventNtf);
                    return;
                }

                // Check dynamic XML dialogue tree transitions
                NpcScriptDef? script = null;
                if (_gameDb != null)
                {
                    if (_gameDb.Fields.TryGetValue(state.Player.FieldId, out var curField))
                    {
                        curField.NpcScripts.TryGetValue(npcId, out script);
                    }
                    if (script == null && (npcId == 600 || npcId == 486))
                    {
                        _gameDb.NpcScripts.TryGetValue(npcId, out script);
                    }
                }

                if (script != null && script.Dialogs.TryGetValue(state.CurrentNpcDialogNode, out var currentDlg))
                {
                    int selIdx = selectedIndex - 1; // 1-indexed to 0-indexed
                    if (selIdx >= 0 && selIdx < currentDlg.Selections.Count)
                    {
                        var selection = currentDlg.Selections[selIdx];

                        // Dynamic shop / salon facility detection from selection action/text
                        if (selection.Text.Contains("購買") || selection.Text.Contains("Shop") || selection.Text.Contains("店"))
                        {
                            await OpenNpcShopAsync(state, npcId);
                            return;
                        }
                        if (selection.Text.Contains("Hair") || selection.Text.Contains("美容") || selection.Text.Contains("Salon"))
                        {
                            await OpenHairShopAsync(state);
                            return;
                        }

                        player.ScriptVariables ??= new(StringComparer.OrdinalIgnoreCase);
                        if (!string.IsNullOrEmpty(selection.VarName))
                        {
                            if (string.Equals(selection.Op, "set", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(selection.Op))
                            {
                                player.ScriptVariables[selection.VarName] = selection.Value;
                            }
                            else if (string.Equals(selection.Op, "inc", StringComparison.OrdinalIgnoreCase))
                            {
                                player.ScriptVariables[selection.VarName] = player.ScriptVariables.TryGetValue(selection.VarName, out var v) ? v + 1 : 1;
                            }
                            else if (string.Equals(selection.Op, "dec", StringComparison.OrdinalIgnoreCase))
                            {
                                player.ScriptVariables[selection.VarName] = player.ScriptVariables.TryGetValue(selection.VarName, out var v) ? v - 1 : 0;
                            }
                            if (_repository != null)
                            {
                                _ = _repository.SaveAccountAsync(player);
                            }
                        }

                        if (!string.IsNullOrEmpty(selection.Next))
                        {
                            int epiYoi = player.EpisodeYoi;
                            int epiEs = player.EpisodeEs;
                            string nextNode = script.ResolveNext(selection.Next, (int)player.School, player.Level, player.Grade, ref epiYoi, ref epiEs, player.ScriptVariables);

                            if (epiYoi != player.EpisodeYoi || epiEs != player.EpisodeEs)
                            {
                                player.EpisodeYoi = epiYoi;
                                player.EpisodeEs = epiEs;
                                if (_repository != null)
                                {
                                    _ = _repository.SaveAccountAsync(player);
                                }
                                Logger.Info($"[FieldServer] '{player.CharacterName}' episode progress updated: So-il={epiYoi}, Estiva={epiEs}");
                            }

                            if (script.Dialogs.TryGetValue(nextNode, out var nextDlg))
                            {
                                state.CurrentNpcDialogNode = nextDlg.Name;
                                var nextChoices = nextDlg.Selections.Select(s => s.Text).ToList();

                                string tempEs = _gameDb?.GetEpisodeTitleForProgress(1, epiEs) ?? "クリア";
                                string tempYoi = _gameDb?.GetEpisodeTitleForProgress(2, epiYoi) ?? "クリア";

                                string formattedNextText = nextDlg.Text
                                    .Replace("${local.grade}", player.Grade.ToString())
                                    .Replace("${local.level}", player.Level.ToString())
                                    .Replace("${local.name}", player.CharacterName)
                                    .Replace("${peke.epi.es}", epiEs.ToString())
                                    .Replace("${peke.epi.yoi}", epiYoi.ToString())
                                    .Replace("${peke.temp}", tempEs)
                                    .Replace("${peke.temp2}", tempYoi);

                                int nextDialogId = nextDlg.Id > 0 ? nextDlg.Id : (int.TryParse(nextDlg.Name, out int parsedId) ? parsedId : 2);
                                byte[] nextDialogNtf = YogurtingPackets.MakeGameExNpcDialogNtf(npcId, nextDialogId, nextDlg.CutIn, formattedNextText, nextChoices, nextDlg.CloseButton);
                                await state.Session.SendAsync(nextDialogNtf);
                                return;
                            }
                        }
                    }
                }

                // Dialogue ended / farewell chosen -> Send ONLY 0x793E to close and restore mobility
                byte[] closeEventNtf = YogurtingPackets.MakeGameNpcDialogEventNtf(npcId, 1);
                await state.Session.SendAsync(closeEventNtf);
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
    }
}
