using System;
using System.IO;
using System.Threading.Tasks;
using Yogurting.Core.Logging;
using Yogurting.Core.Models;
using Yogurting.Core.Network;
using Yogurting.Data.Loaders;
using Yogurting.Data.Repositories;

namespace Yogurting.Server.Handlers.Field
{
    /// <summary>
    /// Handles Combat, Field Monster interactions, Damage, Loot, and EXP Progression.
    /// Matches Delphi UClasses, UYgDB, and _Unit47.pas / _Unit49.pas.
    /// </summary>
    public sealed class CombatHandlers
    {
        private readonly Func<PlayerSessionState, byte[], Task> _broadcastDelegate;
        private readonly IAccountRepository? _repository;
        private readonly GameDatabase? _gameDb;
        private readonly Random _random = new();

        public CombatHandlers(Func<PlayerSessionState, byte[], Task> broadcastDelegate, IAccountRepository? repository = null, GameDatabase? gameDb = null)
        {
            _broadcastDelegate = broadcastDelegate ?? throw new ArgumentNullException(nameof(broadcastDelegate));
            _repository = repository;
            _gameDb = gameDb;
        }

        /// <summary>
        /// 0x7919 (31001): MsgGameAttackReq - Player Attack Request
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameAttackReq)]
        public async Task HandleAttackAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                var player = state.Player;
                if (player == null) return;

                int charaId = packetData.Length >= 10 ? BitConverter.ToInt32(packetData, 6) : player.CharacterId;
                byte animId = packetData.Length >= 11 ? packetData[10] : (byte)0;
                int targetMainType = packetData.Length >= 15 ? BitConverter.ToInt32(packetData, 11) : 2;
                int targetMainId = packetData.Length >= 19 ? BitConverter.ToInt32(packetData, 15) : -1;
                int targetMainX = packetData.Length >= 23 ? BitConverter.ToInt32(packetData, 19) : (int)player.Position.X;
                int targetMainY = packetData.Length >= 27 ? BitConverter.ToInt32(packetData, 23) : (int)player.Position.Y;
                byte flag = packetData.Length >= 28 ? packetData[27] : (byte)0;
                ushort targetsCount = packetData.Length >= 30 ? BitConverter.ToUInt16(packetData, 28) : (ushort)0;

                // Read all targets in list (Delphi TSchoolSession.sub_006C17DC: 16 bytes per target)
                var reqTargets = new List<(int type, int entityId, int x, int y)>();
                int targetOffset = 30;
                for (int i = 0; i < targetsCount && targetOffset + 16 <= packetData.Length; i++)
                {
                    int tType = BitConverter.ToInt32(packetData, targetOffset);
                    int tId = BitConverter.ToInt32(packetData, targetOffset + 4);
                    int tX = BitConverter.ToInt32(packetData, targetOffset + 8);
                    int tY = BitConverter.ToInt32(packetData, targetOffset + 12);
                    targetOffset += 16;
                    reqTargets.Add((tType, tId, tX, tY));
                }

                // Fallback: If targetMainId is not specified but target list has entities, use first target
                if (targetMainId <= 0 && reqTargets.Count > 0)
                {
                    targetMainId = reqTargets[0].entityId;
                    targetMainX = reqTargets[0].x;
                    targetMainY = reqTargets[0].y;
                }

                // Detect equipped weapon category (1=Blade, 2=Glove, 3=Blunt, 4=Spirit)
                int weaponCat = 1;
                int weaponTypeId = 0;
                if (player.EquippedSlotUids != null && player.EquippedSlotUids.Length > 4 && player.EquippedSlotUids[4] > 0)
                {
                    int wUid = player.EquippedSlotUids[4];
                    var wItem = player.StarBeItems?.Find(i => i.Id == wUid || i.SerialId == wUid || (i.TypeId > 0 && i.TypeId == wUid))
                             ?? player.Inventory?.Find(i => i.Id == wUid);
                    if (wItem != null)
                    {
                        weaponTypeId = wItem.TypeId > 0 ? wItem.TypeId : wItem.ItemId;
                        weaponCat = (weaponTypeId / 10000) switch
                        {
                            11 => 1,
                            12 => 2,
                            13 => 3,
                            14 => 4,
                            _ => 1
                        };
                    }
                }

                // Combo calculation (Delphi _Unit49.pas: 5000ms reset threshold)
                long now = Environment.TickCount64;
                if (now - player.LastAttackTime > 5000)
                {
                    player.ComboCount = 0;
                }
                player.ComboCount = Math.Min(999, player.ComboCount + 1);
                player.LastAttackTime = now;

                // Check if swinging in empty air (no target selected and target list is empty)
                bool isEmptySwing = (targetMainId <= 0 || targetMainId == -1) && targetsCount == 0 && reqTargets.Count == 0;
                if (isEmptySwing)
                {
                    byte[] swingAns = YogurtingPackets.MakeGameAttackAns(
                        player.CharacterId,
                        targetEntityId: -1,
                        targetX: targetMainX,
                        targetY: targetMainY,
                        damage: 0,
                        isCritical: false,
                        combo: player.ComboCount,
                        weaponCategory: weaponCat,
                        skillId: 215,
                        addDexExp: 0);
                    await state.Session.SendAsync(swingAns);
                    await _broadcastDelegate(state, swingAns);
                    return;
                }

                // Check field monsters
                FieldMonster? targetMonster = null;
                if (_gameDb != null && _gameDb.Fields.TryGetValue(player.FieldId, out var fieldDef))
                {
                    lock (fieldDef.Monsters)
                    {
                        // 1. Exact match by targetMainId
                        if (targetMainId > 0)
                        {
                            targetMonster = fieldDef.Monsters.Find(m => m.EntityId == targetMainId && !m.IsDead);
                        }

                        // 2. Exact match by any target in list
                        if (targetMonster == null && reqTargets.Count > 0)
                        {
                            foreach (var rt in reqTargets)
                            {
                                targetMonster = fieldDef.Monsters.Find(m => m.EntityId == rt.entityId && !m.IsDead);
                                if (targetMonster != null) break;
                            }
                        }

                        // 3. Proximity match fallback if target ID couldn't be matched directly
                        if (targetMonster == null)
                        {
                            float pX = (float)player.Position.X;
                            float pY = (float)player.Position.Y;
                            float closestDistSq = 15f * 15f;

                            foreach (var mon in fieldDef.Monsters)
                            {
                                if (mon.IsDead) continue;
                                float dx = mon.X - pX;
                                float dy = mon.Y - pY;
                                float distSq = dx * dx + dy * dy;
                                if (distSq < closestDistSq)
                                {
                                    closestDistSq = distSq;
                                    targetMonster = mon;
                                }
                            }
                        }
                    }
                }

                if (targetMonster != null)
                {
                    // 1. Dynamic Attack Calculation: Base Level + Equipped Weapon Attack from Database
                    int equipAtk = 0;
                    if (_gameDb != null && player.Equips != null)
                    {
                        foreach (var eq in player.Equips)
                        {
                            if (_gameDb.Items.TryGetValue(eq.TypeId, out var itemDef) && itemDef.Attack > 0)
                            {
                                equipAtk += itemDef.Attack;
                            }
                        }
                    }

                    // 1. Dynamic Attack Calculation: Base POW from StatusTable.txt + Equipped Weapon Attack from Database
                    var status = _gameDb != null ? _gameDb.GetStatusForLevel(player.Level) : new StatusDef { Pow = player.Level * 4, Luck = player.Level * 2 };
                    int baseAtk = Math.Max(15, status.Pow * 4 + equipAtk);
                    float dmgMultiplier = player.GetDamageMultiplier(); // e.g. 1.2x if Attack Buff active
                    int damage = Math.Max(5, (int)((baseAtk + _random.Next(-3, 8)) * dmgMultiplier));

                    // Authentic LUCK scaling from StatusTable.txt + Critical Buff Multiplier
                    float critChance = Math.Min(60f, (status.Luck * 1.5f + 5f) * player.GetCritMultiplier());
                    bool isCrit = _random.Next(0, 100) < critChance;
                    if (isCrit) damage = (int)(damage * 1.5);

                    // Apply damage to monster
                    targetMonster.TakeDamage(damage);
                    targetMonster.TargetPlayerId = player.CharacterId;

                    Logger.Info($"[Combat] '{player.CharacterName}' attacked '{targetMonster.Name}' (ID: {targetMonster.EntityId}) for {damage} dmg (Base: {baseAtk}, BuffMult: {dmgMultiplier:F1}x, Crit: {isCrit})! Remaining HP: {targetMonster.CurrentHp}/{targetMonster.MaxHp}");

                    // Broadcast attack answer & floating damage numbers (0x791A)
                    byte[] atkAns = YogurtingPackets.MakeGameAttackAns(
                        player.CharacterId,
                        targetMonster.EntityId,
                        (int)targetMonster.X,
                        (int)targetMonster.Y,
                        damage,
                        isCrit,
                        player.ComboCount,
                        weaponCategory: weaponCat,
                        skillId: 215,
                        addDexExp: 1);

                    await state.Session.SendAsync(atkAns);
                    await _broadcastDelegate(state, atkAns);

                    // Broadcast overhead HP bar update (0x79D8)
                    byte[] monHpNtf = YogurtingPackets.MakeGameMonHpInfoNtf(targetMonster.EntityId, targetMonster.CurrentHp, targetMonster.MaxHp);
                    await state.Session.SendAsync(monHpNtf);
                    await _broadcastDelegate(state, monHpNtf);

                    // Generate Charge Points (0x791C: MsgGameChargePointUpdateNtf: 0 -> 1 -> 2 -> 3)
                    player.ChargePoint = (byte)Math.Min((byte)3, (byte)(player.ChargePoint + 1));
                    await state.Session.SendAsync(YogurtingPackets.MakeGameChargePointUpdateNtf(player.ChargePoint));

                    // Weapon Proficiency / Dexterity Progression (_Unit49.pas:18950)
                    if (player.DexExps != null && player.DexExps.Length > weaponCat)
                    {
                        player.DexExps[weaponCat] += 1;
                        int curLvl = player.DexLevels != null && player.DexLevels.Length > weaponCat ? player.DexLevels[weaponCat] : 1;
                        int reqDex = _gameDb != null ? _gameDb.GetRequiredDexForLevel(curLvl) : 20;
                        if (player.DexExps[weaponCat] >= reqDex)
                        {
                            int newLvl = curLvl + 1;
                            if (player.DexLevels != null && player.DexLevels.Length > weaponCat)
                            {
                                player.DexLevels[weaponCat] = newLvl;
                            }
                            player.DexExps[weaponCat] -= reqDex;
                            Logger.Info($"[Combat] *** WEAPON MASTERY UP! *** '{player.CharacterName}' Weapon Category {weaponCat} reached Level {newLvl}!");
                        }
                    }

                    // Monster Retaliation Counter-Attack
                    if (!targetMonster.IsDead && (DateTime.UtcNow - targetMonster.LastAttackTime).TotalSeconds >= 1.5)
                    {
                        targetMonster.LastAttackTime = DateTime.UtcNow;
                        int monDmg = Math.Max(1, targetMonster.Level * 2 - (player.Defense / 4));
                        player.CurrentHp = Math.Max(0, player.CurrentHp - monDmg);
                        Logger.Info($"[Combat] '{targetMonster.Name}' counter-attacked '{player.CharacterName}' for {monDmg} dmg! Player HP: {player.CurrentHp}/{player.MaxHp}");
                        await state.Session.SendAsync(YogurtingPackets.MakeGameSetStateNtf(player));
                    }

                    // Check if monster died
                    if (targetMonster.IsDead)
                    {
                        // Authentic Delphi EXP Formula (_Unit49.pas:18999):
                        // levelDiff = abs(player.Level - monster.Level)
                        // exp = monster.Exp * (1.0 - 0.003907 * levelDiff * levelDiff)
                        int levelDiff = Math.Abs(player.Level - targetMonster.Level);
                        int expEarned;
                        if (levelDiff > 15)
                        {
                            expEarned = 1;
                        }
                        else
                        {
                            double factor = 1.0 - (0.003907 * levelDiff * levelDiff);
                            expEarned = Math.Max(1, (int)Math.Floor(targetMonster.ExpReward * Math.Max(0.1, factor)));
                        }

                        // Apply Dynamic EXP Buff Multiplier (e.g. 1.5x with Tempo EXP Buff)
                        float expMultiplier = player.GetExpMultiplier();
                        expEarned = Math.Max(1, (int)(expEarned * expMultiplier));

                        // Determine loot drop
                        int dropItemId = 0;
                        int dropCount = 0;
                        if (targetMonster.DropItemType > 0 && _random.Next(0, 1000) < targetMonster.DropRate)
                        {
                            dropItemId = targetMonster.DropItemType;
                            dropCount = Math.Max(1, targetMonster.DropCount);
                        }

                        // Insert dropped loot directly into player's inventory (_Unit49.pas:19000: TCoItemList.IncItem)
                        if (dropItemId > 0)
                        {
                            var existing = player.Inventory?.Find(i => i.TypeId == dropItemId);
                            if (existing != null)
                            {
                                existing.Quantity += dropCount;
                            }
                            else
                            {
                                player.Inventory?.Add(new Item
                                {
                                    Id = ((player.Inventory?.Count > 0) ? player.Inventory.Max(i => i.Id) : 0) + 1,
                                    TypeId = dropItemId,
                                    SlotIndex = player.Inventory?.Count ?? 0,
                                    SlotType = ItemSlotType.Inventory,
                                    Quantity = dropCount,
                                    Name = _gameDb != null && _gameDb.Items.TryGetValue(dropItemId, out var itemDef) ? itemDef.Name : "Loot Item"
                                });
                            }
                            Logger.Info($"[Combat] '{player.CharacterName}' collected dropped loot: {dropCount}x Item #{dropItemId}!");
                        }

                        player.CurrentExp += expEarned;
                        Logger.Info($"[Combat] '{targetMonster.Name}' defeated by '{player.CharacterName}'! Gained {expEarned} EXP. Total EXP: {player.CurrentExp}/{player.MaxExp}");

                        // 1. Broadcast Monster Death Animation (0x796A)
                        byte[] actionNtf = YogurtingPackets.MakeGameMonActionNtf(targetMonster.EntityId, (ushort)targetMonster.X, (ushort)targetMonster.Y, 5, player.CharacterId);
                        await state.Session.SendAsync(actionNtf);
                        await _broadcastDelegate(state, actionNtf);

                        // 2. Broadcast Monster Overhead Status HP=0 (0x796D)
                        byte[] monStatusNtf = YogurtingPackets.MakeGameMonStatusNtf(targetMonster.EntityId, targetMonster.MonsterType, 0, targetMonster.MaxHp, (ushort)targetMonster.X, (ushort)targetMonster.Y);
                        await state.Session.SendAsync(monStatusNtf);
                        await _broadcastDelegate(state, monStatusNtf);

                        // 3. Broadcast Monster Dead & Loot Delivery to Top-Right Cardboard Booty Box (0x5276)
                        byte[] deadNtf = YogurtingPackets.MakeGameHuntMonDeadNtf(
                            targetMonster.EntityId,
                            (ushort)targetMonster.X,
                            (ushort)targetMonster.Y,
                            player.CharacterId,
                            expEarned,
                            (int)player.CurrentExp,
                            dropItemId,
                            dropCount,
                            isEquipment: false);
                        await state.Session.SendAsync(deadNtf);
                        await _broadcastDelegate(state, deadNtf);

                        // 3b. Broadcast Physical Cardboard Loot Drop Box on Floor (0x796C)
                        if (dropItemId > 0)
                        {
                            byte[] dropBoxNtf = YogurtingPackets.MakeGameFieldDropBoxNtf(targetMonster.EntityId, (ushort)targetMonster.X, (ushort)targetMonster.Y, dropItemId, 1);
                            await state.Session.SendAsync(dropBoxNtf);
                            await _broadcastDelegate(state, dropBoxNtf);
                        }

                        // 4. Send EXP Gain notice (0x5277) to trigger floating EXP popup
                        byte[] expNtf = YogurtingPackets.MakeGameHuntCharExpUpNtf(expEarned);
                        await state.Session.SendAsync(expNtf);

                        // 5. Broadcast Despawn Packet (0x7A00) to clear dead 3D model
                        byte[] despawnNtf = YogurtingPackets.MakeGameMonDeadNtf(targetMonster.EntityId);
                        await state.Session.SendAsync(despawnNtf);
                        await _broadcastDelegate(state, despawnNtf);



                        // Authentic Level-Up Check using ExpTable.txt & StatusTable.txt
                        int reqExp = _gameDb != null ? _gameDb.GetMaxExpForLevel(player.Level) : (int)player.MaxExp;
                        if (player.CurrentExp >= reqExp)
                        {
                            player.Level++;
                            player.CurrentExp -= reqExp;
                            player.MaxExp = _gameDb != null ? _gameDb.GetMaxExpForLevel(player.Level) : (int)(player.MaxExp * 1.30);
                            
                            var statusLv = _gameDb != null ? _gameDb.GetStatusForLevel(player.Level) : new StatusDef { Pow = player.Level * 4, Skill = player.Level * 3 };
                            player.MaxHp = statusLv.Pow * 10 + 200;
                            player.CurrentHp = player.MaxHp;
                            player.MaxMp = statusLv.Skill * 10 + 150;
                            player.CurrentMp = player.MaxMp;

                            Logger.Info($"[Combat] *** LEVEL UP! *** '{player.CharacterName}' reached Level {player.Level}!");

                            // Broadcast Level-Up Fanfare (0x7970)
                            byte[] lvUpNtf = YogurtingPackets.MakeGameCharLvUpNtf(player.Level);
                            await state.Session.SendAsync(lvUpNtf);
                            await _broadcastDelegate(state, lvUpNtf);

                            // Send updated character info (0x7952)
                            await state.Session.SendAsync(YogurtingPackets.MakeGameCharInfoNtf(player));
                        }

                        // Persist player progression
                        if (_repository != null)
                        {
                            await _repository.SaveAccountAsync(player);
                        }

                        // Schedule Monster Respawn after delay (default 5s)
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await Task.Delay(TimeSpan.FromSeconds(targetMonster.RespawnSeconds));
                                targetMonster.Respawn();
                                byte[] triggerNtf = YogurtingPackets.MakeGameTriggerMobNtf(targetMonster.EntityId);
                                byte[] respawnNtf = YogurtingPackets.MakeGameMonInfoNtf(targetMonster);
                                await state.Session.SendAsync(triggerNtf);
                                await _broadcastDelegate(state, triggerNtf);
                                await state.Session.SendAsync(respawnNtf);
                                await _broadcastDelegate(state, respawnNtf);
                                Logger.Debug($"[Combat] '{targetMonster.Name}' respawned at ({targetMonster.X}, {targetMonster.Y}).");
                            }
                            catch { }
                        });
                    }
                }
                else
                {
                    // Empty swing (no monster hit)
                    byte[] atkAns = YogurtingPackets.MakeGameAttackAns(
                        player.CharacterId,
                        0,
                        (int)player.Position.X,
                        (int)player.Position.Y,
                        0,
                        false,
                        1);

                    await state.Session.SendAsync(atkAns);
                    await _broadcastDelegate(state, atkAns);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[Combat] HandleAttack failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x7925 (31013): MsgGameSkillCastReq - Active Skill Cast Initiation (F1 hotkey)
        /// </summary>


        /// <summary>
        /// 0xA413 (42003): MsgGameCapsuleBuyReq - Capsule Vending Machine Purchase
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameCapsuleBuyReq)]
        public async Task HandleCapsuleBuyReqAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                var player = state.Player;
                if (player == null) return;

                int machineSn = packetData.Length >= 10 ? BitConverter.ToInt32(packetData, 6) : 0;
                Logger.Info($"[Capsule] '{player.CharacterName}' pulled Capsule Vending Machine #{machineSn}.");

                // Send success response (0xA414)
                using var writer = PacketWriter.Create(PacketOpcode.MsgGameCapsuleBuyAns);
                writer.WriteInt32(0); // Success
                writer.WriteInt32(machineSn);
                writer.WriteInt32(200001); // Beginner Bread reward
                writer.WriteInt32(1); // Quantity
                await state.Session.SendAsync(writer.Build());
            }
            catch (Exception ex)
            {
                Logger.Error($"[Capsule] HandleCapsuleBuyReq failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 0xA415 (42005): MsgGameCapsuleExitNtf - Capsule Vending Machine Exit
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameCapsuleExitNtf)]
        public Task HandleCapsuleExitNtfAsync(PlayerSessionState state, byte[] packetData)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 0x7923 (31011): MsgGameSkillCastReq - Player Active Combat Skill Cast
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameSkillCastReq)]
        public async Task HandleSkillCastReqAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                var player = state.Player;
                if (player == null) return;

                int skillId = packetData.Length >= 10 ? BitConverter.ToInt32(packetData, 6) : 0;
                int seqNum = packetData.Length >= 14 ? BitConverter.ToInt32(packetData, 10) : 0;
                int targetMainType = packetData.Length >= 18 ? BitConverter.ToInt32(packetData, 14) : 2;
                int targetMainId = packetData.Length >= 22 ? BitConverter.ToInt32(packetData, 18) : -1;
                int targetMainX = packetData.Length >= 26 ? BitConverter.ToInt32(packetData, 22) : (int)player.Position.X;
                int targetMainY = packetData.Length >= 30 ? BitConverter.ToInt32(packetData, 26) : (int)player.Position.Y;
                byte flag = packetData.Length >= 31 ? packetData[30] : (byte)0;
                ushort targetsCount = packetData.Length >= 33 ? BitConverter.ToUInt16(packetData, 31) : (ushort)0;

                int weaponCat = 1; // Default Blade
                if (player.EquippedSlotUids != null && player.EquippedSlotUids.Length > 4 && player.EquippedSlotUids[4] != 0)
                {
                    var weaponItem = player.Inventory?.Find(i => i.Id == player.EquippedSlotUids[4]);
                    if (weaponItem != null && _gameDb?.Items.TryGetValue(weaponItem.TypeId, out var wDef) == true)
                    {
                        weaponCat = Math.Max(1, wDef.WeaponType);
                    }
                }

                Logger.Info($"[Combat] '{player.CharacterName}' cast Skill #{skillId} Seq={seqNum} WeaponCat={weaponCat} TargetMainId={targetMainId} TargetsCount={targetsCount}");

                // Resolve splash damage for all hit targets
                var targetResults = new List<(int targetType, int targetId, int targetX, int targetY, int damage, byte hitType)>();
                int targetOffset = 33;
                for (int i = 0; i < targetsCount && targetOffset + 16 <= packetData.Length; i++)
                {
                    int tType = BitConverter.ToInt32(packetData, targetOffset);
                    int tId = BitConverter.ToInt32(packetData, targetOffset + 4);
                    int tX = BitConverter.ToInt32(packetData, targetOffset + 8);
                    int tY = BitConverter.ToInt32(packetData, targetOffset + 12);
                    targetOffset += 16;

                    // Skill damage calculation (2.5x base weapon attack + variance)
                    int baseAtk = 40 + (player.Level * 4);
                    int dmg = Math.Max(10, (int)(baseAtk * 2.5f + _random.Next(-5, 12)));
                    bool isCrit = _random.Next(0, 100) < 30;
                    if (isCrit) dmg = (int)(dmg * 1.5f);

                    targetResults.Add((tType, tId, tX, tY, dmg, isCrit ? (byte)2 : (byte)1));

                    // Apply to monster if present
                    if (_gameDb != null && _gameDb.Fields.TryGetValue(player.FieldId, out var fDef))
                    {
                        lock (fDef.Monsters)
                        {
                            var mon = fDef.Monsters.Find(m => m.EntityId == tId && !m.IsDead);
                            if (mon != null)
                            {
                                mon.TakeDamage(dmg);
                                byte[] monHpNtf = YogurtingPackets.MakeGameMonHpInfoNtf(mon.EntityId, mon.CurrentHp, mon.MaxHp);
                                _ = state.Session.SendAsync(monHpNtf);
                                _ = _broadcastDelegate(state, monHpNtf);
                            }
                        }
                    }
                }

                // If single-target (targetsCount == 0) and targetMainId != -1
                if (targetResults.Count == 0 && targetMainId != -1)
                {
                    int baseAtk = 40 + (player.Level * 4);
                    int dmg = Math.Max(10, (int)(baseAtk * 2.5f + _random.Next(-5, 12)));
                    bool isCrit = _random.Next(0, 100) < 30;
                    if (isCrit) dmg = (int)(dmg * 1.5f);

                    targetResults.Add((targetMainType, targetMainId, targetMainX, targetMainY, dmg, isCrit ? (byte)2 : (byte)1));

                    if (_gameDb != null && _gameDb.Fields.TryGetValue(player.FieldId, out var fDef))
                    {
                        lock (fDef.Monsters)
                        {
                            var mon = fDef.Monsters.Find(m => m.EntityId == targetMainId && !m.IsDead);
                            if (mon != null)
                            {
                                mon.TakeDamage(dmg);
                                byte[] monHpNtf = YogurtingPackets.MakeGameMonHpInfoNtf(mon.EntityId, mon.CurrentHp, mon.MaxHp);
                                _ = state.Session.SendAsync(monHpNtf);
                                _ = _broadcastDelegate(state, monHpNtf);
                            }
                        }
                    }
                }

                // Dispatch 0x7924 (MsgGameSkillCastAns)
                byte[] skillAns = YogurtingPackets.MakeGameSkillCastAns(
                    player.CharacterId,
                    skillId,
                    seqNum,
                    targetMainType,
                    targetMainId,
                    targetMainX,
                    targetMainY,
                    targetResults,
                    weaponCat,
                    addDexExp: 2);

                await state.Session.SendAsync(skillAns);
                await _broadcastDelegate(state, skillAns);

                // Award proficiency EXP
                if (player.DexExps != null && player.DexExps.Length > weaponCat)
                {
                    player.DexExps[weaponCat] += 2;
                }

                if (_repository != null)
                {
                    await _repository.SaveAccountAsync(player);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[Combat] HandleSkillCastReq error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x79FC (31228): MsgGameSkillHotkeyNtf - Save Player Skill Hotkey Bindings
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameSkillHotkeyNtf)]
        public async Task HandleSkillHotkeyNtfAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                var player = state.Player;
                if (player == null) return;

                ushort weaponCategory = packetData.Length >= 8 ? BitConverter.ToUInt16(packetData, 6) : (ushort)1;
                ushort slotIndex = packetData.Length >= 10 ? BitConverter.ToUInt16(packetData, 8) : (ushort)1;
                int skillId = packetData.Length >= 14 ? BitConverter.ToInt32(packetData, 10) : 0;

                Logger.Info($"[Combat] '{player.CharacterName}' set Skill Hotkey: WeaponCat={weaponCategory} Slot={slotIndex} SkillId={skillId}");

                if (player.SkillHotkeys != null && player.SkillHotkeys.TryGetValue(weaponCategory, out var slots))
                {
                    if (slotIndex < slots.Length)
                    {
                        slots[slotIndex] = skillId;
                    }
                }

                if (_repository != null)
                {
                    await _repository.SaveAccountAsync(player);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[Combat] HandleSkillHotkeyNtf error: {ex.Message}");
            }
        }

        /// <summary>
        /// 0x7974 (31092): MsgGameBootyBoxDoneReq - Player selected and opened a Booty Box
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameBootyBoxDoneReq)]
        public async Task HandleBootyBoxDoneReqAsync(PlayerSessionState state, byte[] packetData)
        {
            try
            {
                var player = state.Player;
                if (player == null) return;

                int selectedBoxIndex = packetData.Length >= 10 ? BitConverter.ToInt32(packetData, 6) : 0;
                Logger.Info($"[Combat] '{player.CharacterName}' opened Booty Box #{selectedBoxIndex}! Unboxing reward...");

                // Award reward item (e.g. Rare Uniform / Consumable)
                int rewardTypeId = selectedBoxIndex switch
                {
                    1 => 110001, // Gym Uniform
                    2 => 310001, // Potion
                    _ => 140001  // Blade Weapon
                };

                player.Inventory.Add(new Item
                {
                    Id = (player.Inventory.Count > 0 ? player.Inventory.Max(i => i.Id) : 0) + 1,
                    TypeId = rewardTypeId,
                    SlotIndex = player.Inventory.Count,
                    SlotType = ItemSlotType.Inventory,
                    Quantity = 1,
                    Name = _gameDb != null && _gameDb.Items.TryGetValue(rewardTypeId, out var itemDef) ? itemDef.Name : "Booty Box Prize"
                });

                if (_repository != null)
                {
                    await _repository.SaveAccountAsync(player);
                }

                // 1. Confirm Booty Box Unbox with particle sparkle trigger (0x7975)
                byte[] doneAns = YogurtingPackets.MakeGameBootyBoxDoneAns(1);
                await state.Session.SendAsync(doneAns);

                // 2. Sync updated inventory to client (0x520F)
                byte[] stateNtf = YogurtingPackets.MakeGameSetStateNtf(player);
                await state.Session.SendAsync(stateNtf);
            }
            catch (Exception ex)
            {
                Logger.Error($"[Combat] HandleBootyBoxDoneReq error: {ex.Message}");
            }
        }
    }
}
