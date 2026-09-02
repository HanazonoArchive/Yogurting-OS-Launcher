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

                // 0x7919 Payload layout (Verified against raw packet stream & Delphi):
                // Header (6B): Size(2B) + Reserved(2B) + Opcode(2B: 0x7919)
                // Offset 6:  charaId        (4B Int32)
                // Offset 10: animId         (1B Byte)
                // Offset 11: targetMainType (4B Int32)
                // Offset 15: targetMainId   (4B Int32)
                // Offset 19: targetMainX    (4B Int32)
                // Offset 23: targetMainY    (4B Int32)
                // Offset 27: flag           (1B Byte)
                // Offset 28: targetsCount   (2B UInt16)
                // Offset 30: targets array (16B each: type[4B], id[4B], x[4B], y[4B])
                int charaId        = packetData.Length >= 10 ? BitConverter.ToInt32(packetData, 6) : player.CharacterId;
                byte animId        = packetData.Length >= 11 ? packetData[10] : (byte)0;
                int targetMainType = packetData.Length >= 15 ? BitConverter.ToInt32(packetData, 11) : 2;
                int targetMainId   = packetData.Length >= 19 ? BitConverter.ToInt32(packetData, 15) : -1;
                int targetMainX    = packetData.Length >= 23 ? BitConverter.ToInt32(packetData, 19) : (int)player.Position.X;
                int targetMainY    = packetData.Length >= 27 ? BitConverter.ToInt32(packetData, 23) : (int)player.Position.Y;
                byte flag          = packetData.Length >= 28 ? packetData[27] : (byte)0;
                ushort targetsCount = packetData.Length >= 30 ? BitConverter.ToUInt16(packetData, 28) : (ushort)0;

                // Read all targets in list (16 bytes per target)
                var reqTargets = new List<(int type, int entityId, int x, int y)>();
                int targetOffset = 30;
                for (int i = 0; i < targetsCount && targetOffset + 16 <= packetData.Length; i++)
                {
                    int tType = BitConverter.ToInt32(packetData, targetOffset);
                    int tId   = BitConverter.ToInt32(packetData, targetOffset + 4);
                    int tX    = BitConverter.ToInt32(packetData, targetOffset + 8);
                    int tY    = BitConverter.ToInt32(packetData, targetOffset + 12);
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

                // Detect equipped weapon category dynamically from GameDatabase (1=Blade, 2=Glove, 3=Blunt, 4=Spirit)
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
                        if (_gameDb != null && _gameDb.Items.TryGetValue(weaponTypeId, out var wDef) && wDef.WeaponType > 0)
                        {
                            weaponCat = wDef.WeaponType;
                        }
                        else
                        {
                            weaponCat = (weaponTypeId / 10000) switch
                            {
                                11 => 1, // Blade
                                12 => 2, // Glove
                                13 => 3, // Blunt
                                14 => 4, // Spirit / Gun
                                140 => 4, // Star Spirit
                                _ => 1
                            };
                        }
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
                player.LastCombatTime = DateTime.UtcNow;

                // Calculate authentic weapon combo attack skill ID from AtkWeapon group (BeItemType.txt column 12 / SkillWeapon.txt)
                int atkGroup = 0;
                if (_gameDb != null && _gameDb.Items.TryGetValue(weaponTypeId, out var eqDef) && (eqDef.AttackGroup > 0 || eqDef.Attack > 0))
                {
                    atkGroup = eqDef.AttackGroup > 0 ? eqDef.AttackGroup : eqDef.Attack;
                }
                else
                {
                    atkGroup = weaponCat switch
                    {
                        1 => 101, // Blade
                        2 => 301, // Glove
                        3 => 501, // Blunt
                        4 => 701, // Spirit
                        _ => 101
                    };
                }

                // Combo attack IDs are 1-indexed (e.g. 10201 for animId 0, 10202 for animId 1, 10203 for animId 2)
                int attackSkillId = (atkGroup * 100) + (int)(animId % 4) + 1;

                // Check field monsters (Only hit explicit targets requested by client in MsgGameAttackReq)
                var hitMonsters = new List<FieldMonster>();
                if (_gameDb != null && _gameDb.Fields.TryGetValue(player.FieldId, out var fieldDef))
                {
                    lock (fieldDef.Monsters)
                    {
                        if (reqTargets.Count > 0)
                        {
                            foreach (var rt in reqTargets)
                            {
                                var m = fieldDef.Monsters.Find(mon => mon.EntityId == rt.entityId && !mon.IsDead);
                                if (m != null && !hitMonsters.Contains(m)) hitMonsters.Add(m);
                            }
                        }
                        else if (targetMainId > 0 && targetMainId != unchecked((int)0xFFFFFFFF))
                        {
                            var m = fieldDef.Monsters.Find(mon => mon.EntityId == targetMainId && !mon.IsDead);
                            if (m != null && !hitMonsters.Contains(m)) hitMonsters.Add(m);
                        }
                    }
                }

                // Empty swing if no valid targets were hit
                if (hitMonsters.Count == 0)
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
                        skillId: attackSkillId,
                        addDexExp: 0);
                    await state.Session.SendAsync(swingAns);
                    await _broadcastDelegate(state, swingAns);
                    return;
                }

                if (hitMonsters.Count > 0)
                {
                    // 1. Authentic TChara.FAtk Calculation (Delphi _Unit49.pas:20268-20280)
                    // Base POW is from StatusTable for player level; bonus attack is from reinforce stones and passive skills
                    int bonusAtk = 0;
                    if (_gameDb != null && player.Equips != null)
                    {
                        foreach (var eq in player.Equips)
                        {
                            if (_gameDb.Items.TryGetValue(eq.TypeId, out var itemDef) && itemDef.Attack > 0)
                            {
                                bonusAtk += itemDef.Attack;
                            }
                        }
                    }

                    var status = _gameDb != null ? _gameDb.GetStatusForLevel(player.Level) : new StatusDef { Pow = player.Level * 4, Luck = player.Level * 2 };
                    int fAtk = (status.Pow * 65) + (bonusAtk * 100);
                    float dmgMultiplier = player.GetDamageMultiplier(); // e.g. 1.2x if Attack Buff active

                    // 2. Authentic AtkRatio from AtkWeapon.txt (Delphi _Unit49.pas:21915-21935)
                    // Keyed by combo action skill ID (e.g. 10201), fallback is 1
                    int atkRatio = 1;
                    if (_gameDb != null && _gameDb.AtkWeapons.TryGetValue(attackSkillId, out var atkDef) && atkDef.AtkRatio > 0)
                    {
                        atkRatio = atkDef.AtkRatio;
                    }

                    // 3. Authentic Level Variance (Delphi _Unit49.pas:21936-21949: variance = Random(Max(5, Level / 2)))
                    int varianceMax = Math.Max(5, player.Level / 2);

                    // 4. Authentic Critical Threshold Check (Delphi _Unit49.pas:22000-22007: FCritical = status.Luck * 65 vs Random(113750))
                    int fCritical = (int)(status.Luck * 65 * player.GetCritMultiplier());

                    var targetEntries = new List<(int entityId, int x, int y, int damage, bool isCrit)>();
                    var killedMonsters = new List<FieldMonster>();

                    foreach (var targetMonster in hitMonsters)
                    {
                        int scaledAtk = (int)((fAtk * (long)atkRatio) / 10000);
                        int levelVariance = _random.Next(0, varianceMax);
                        int damage = Math.Max(1, (int)((scaledAtk + levelVariance) * dmgMultiplier));
                        bool isCrit = _random.Next(0, 113750) < fCritical;
                        if (isCrit) damage = (int)(damage * 2.0);

                        // Apply damage to monster
                        bool wasKilled = targetMonster.TakeDamage(damage);

                        // Wake up monster into Chase state immediately (First-attacker locks target: _Unit49.pas:0060F167)
                        if (!targetMonster.IsDead)
                        {
                            if (targetMonster.TargetPlayerId == 0)
                            {
                                targetMonster.TargetPlayerId = player.CharacterId;
                                targetMonster.State = MonsterState.Chase;
                                targetMonster.Frame = 8; // Force immediate path broadcast on next AI tick
                            }
                        }

                        targetEntries.Add((targetMonster.EntityId, (int)targetMonster.X, (int)targetMonster.Y, damage, isCrit));
                        Logger.Info($"[Combat] '{player.CharacterName}' attacked '{targetMonster.Name}' (ID: {targetMonster.EntityId}) for {damage} dmg (Crit: {isCrit})! Remaining HP: {targetMonster.CurrentHp}/{targetMonster.MaxHp}");

                        if (wasKilled)
                        {
                            targetMonster.TargetPlayerId = 0;
                            killedMonsters.Add(targetMonster);
                        }
                    }

                    // 1. Dispatch Target Ownership Lock (0x7A00) upon initial acquisition of target
                    if (hitMonsters.Count > 0)
                    {
                        var primaryTarget = hitMonsters[0];
                        if (player.TargetMonsterId != primaryTarget.EntityId)
                        {
                            player.TargetMonsterId = primaryTarget.EntityId;
                            byte[] lockPkt = YogurtingPackets.MakeGameMonsterOwnershipAcquiredNtf(primaryTarget.EntityId);
                            await state.Session.SendAsync(lockPkt);
                        }
                    }

                    // 2. Charge Point & Skill Gauge Update on Hit (_Unit49.pas:22236-22258)
                    player.GaugeCurrent += 600 + (player.ComboCount * 15);
                    if (player.GaugeMax <= 0) player.GaugeMax = 70000;
                    if (player.GaugeCurrent >= player.GaugeMax)
                    {
                        if (player.ChargePoint < 3)
                        {
                            player.ChargePoint++;
                        }
                        player.GaugeCurrent = player.ChargePoint < 3 ? (player.GaugeCurrent - player.GaugeMax) : 0;
                    }
                    byte[] chargePkt = YogurtingPackets.MakeGameChargePointUpdateNtf(player.ChargePoint, player.GaugeMax, player.GaugeCurrent);
                    await state.Session.SendAsync(chargePkt);

                    // 3. Broadcast attack answer & floating damage numbers across all hit enemies (0x791A)
                    byte[] atkAns = YogurtingPackets.MakeGameAttackAns(
                        player.CharacterId,
                        targetEntries,
                        player.ComboCount,
                        weaponCategory: weaponCat,
                        skillId: attackSkillId,
                        addDexExp: 1);

                    await state.Session.SendAsync(atkAns);
                    await _broadcastDelegate(state, atkAns);

                    // 4. Synchronously process monster kills immediately following 0x791A (matching Delphi Quartet pipeline: _Unit49.pas:0060F494)
                    // SRC: server_legacy/DELPHI PROJECT/_Unit49.pas:0060F48B-0060F494 (Delphi does NOT send 0x79E8 on attack damage; client subtracts damage locally from 0x791A)

                    // 5. Synchronously process monster kills immediately following 0x791A (matching Delphi Quartet pipeline)
                    if (killedMonsters.Count > 0)
                    {
                        foreach (var kMon in killedMonsters)
                        {
                            if (player.TargetMonsterId == kMon.EntityId)
                            {
                                player.TargetMonsterId = 0;
                            }
                            await ProcessMonsterDefeatAsync(state, player, kMon);
                        }
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
                        player.ComboCount,
                        weaponCategory: weaponCat,
                        skillId: attackSkillId,
                        addDexExp: 0);

                    await state.Session.SendAsync(atkAns);
                    await _broadcastDelegate(state, atkAns);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[Combat] HandleAttack failed: {ex.Message}");
            }
        }

        private async Task ProcessMonsterDefeatAsync(PlayerSessionState state, Player player, FieldMonster monster)
        {
            // Authentic Delphi Parabolic Level Penalty (_Unit49.pas:0060CD9F-0060CDE3)
            int baseExp = monster.ExpReward > 0 ? monster.ExpReward : (monster.Level * 3 + 2);
            int levelDiff = Math.Abs(player.Level - monster.Level);
            int expEarned;
            if (levelDiff > 15)
            {
                expEarned = 1;
            }
            else if (levelDiff == 0)
            {
                expEarned = Math.Max(1, baseExp);
            }
            else
            {
                double scale = 1.0 - (levelDiff * levelDiff * 0.003907);
                expEarned = Math.Max(1, (int)Math.Round(baseExp * scale));
            }

            float expMultiplier = player.GetExpMultiplier();
            expEarned = Math.Max(1, (int)(expEarned * expMultiplier));

            // Delphi Loot Drop (_Unit49.pas:19028-19360: Matches HuntMon.txt)
            var lootList = new List<(int itemId, int count, bool isEquip)>();

            // Monster Drop Item / Material / Equipment from HuntMon.txt
            if (monster.DropItemType > 0 && _random.Next(0, 1000) < monster.DropRate)
            {
                int dropCount = Math.Max(1, monster.DropCount);
                bool isEquip = _gameDb != null && _gameDb.Items.TryGetValue(monster.DropItemType, out var idf) && idf.IsEquipment;
                lootList.Add((monster.DropItemType, dropCount, isEquip));

                var existing = player.Inventory?.Find(i => i.TypeId == monster.DropItemType);
                if (existing != null && !isEquip)
                {
                    existing.Quantity += dropCount;
                }
                else
                {
                    player.Inventory?.Add(new Item
                    {
                        Id = ((player.Inventory?.Count > 0) ? player.Inventory.Max(i => i.Id) : 0) + 1,
                        TypeId = monster.DropItemType,
                        SlotIndex = player.Inventory?.Count ?? 0,
                        SlotType = ItemSlotType.Inventory,
                        Quantity = dropCount,
                        Name = _gameDb != null && _gameDb.Items.TryGetValue(monster.DropItemType, out var itemDef) ? itemDef.Name : "Loot Item"
                    });
                }
                Logger.Info($"[Combat] '{player.CharacterName}' collected dropped loot: {dropCount}x Item #{monster.DropItemType}!");
            }

            player.CurrentExp += expEarned;
            Logger.Info($"[Combat] '{monster.Name}' defeated by '{player.CharacterName}'! Gained {expEarned} EXP. Total EXP: {player.CurrentExp}/{player.MaxExp}");

            // Reset player target tracking internally
            // SRC: server_legacy/DELPHI PROJECT/_Unit49.pas:0060CCFC (TMonster.Dead does NOT send 0x7A01 on death; death is signaled exclusively via 0x5276 / 0x5277)
            if (player.TargetMonsterId == monster.EntityId)
            {
                player.TargetMonsterId = 0;
            }

            // Broadcast Monster Dead & Loot Delivery to Top-Right Cardboard Booty Box (0x5276)
            byte[] deadNtf = YogurtingPackets.MakeGameHuntMonDeadNtf(
                monster.EntityId,
                (ushort)monster.X,
                (ushort)monster.Y,
                player.CharacterId,
                expEarned,
                (int)player.CurrentExp,
                lootList,
                monster.MonsterType);
            await state.Session.SendAsync(deadNtf);
            await _broadcastDelegate(state, deadNtf);

            // Send EXP Gain notice (0x5277) to update client EXP bar (_Unit47.pas:49068)
            byte[] expNtf = YogurtingPackets.MakeGameHuntCharExpUpNtf((int)player.CurrentExp);
            await state.Session.SendAsync(expNtf);

            // Authentic Level-Up Check using ExpTable.txt & StatusTable.txt
            int reqExp = _gameDb != null ? _gameDb.GetMaxExpForLevel(player.Level) : (int)player.MaxExp;
            if (player.CurrentExp >= reqExp)
            {
                player.Level++;
                player.CurrentExp -= reqExp;
                player.MaxExp = _gameDb != null ? _gameDb.GetMaxExpForLevel(player.Level) : (int)(player.MaxExp * 1.30);
                
                var statusLv = _gameDb != null ? _gameDb.GetStatusForLevel(player.Level) : new StatusDef { Pow = player.Level * 4, Speed = player.Level * 3, Skill = player.Level * 3, Luck = player.Level * 2 };
                // SRC: server_legacy/DELPHI PROJECT/_Unit49.pas:0060DD6A (TChara.CalcStatus(True) restores HP to full on level up)
                player.RecalculateStats(statusLv.Pow, statusLv.Speed, statusLv.Skill, statusLv.Luck, isLevelUp: true);

                Logger.Info($"[Combat] *** LEVEL UP! *** '{player.CharacterName}' reached Level {player.Level}! HP restored to full ({player.CurrentHp}/{player.MaxHp}).");

                player.SkillPoint++;
                // 1. Broadcast Field/Hunt Level-Up Fanfare & Visual FX (0x5275 - Delphi TMsgGameHuntCharLvUpNtf)
                byte[] huntLvUpNtf = YogurtingPackets.MakeGameHuntCharLvUpNtf(player.Level, (int)player.CurrentExp, (int)player.MaxExp, player.SkillPoint, player.CharacterId);
                await state.Session.SendAsync(huntLvUpNtf);
                await _broadcastDelegate(state, huntLvUpNtf);

                // 2. Synchronize new Level Stats and HP (0x520F + 0x520D)
                await state.Session.SendAsync(YogurtingPackets.MakeGameSetStateNtf(player));
                await state.Session.SendAsync(YogurtingPackets.MakeGameSetHpNtf((ushort)player.CurrentHp));
            }

            // Persist player progression asynchronously to avoid combat thread disk I/O lag
            if (_repository != null)
            {
                _ = Task.Run(() => _repository.SaveAccountAsync(player));
            }
        }

        /// <summary>
        /// 0x7925 (31013): MsgGameSkillPrepNtf - Skill Cast Preparation Notice
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameSkillPrepNtf)]
        public async Task HandleSkillPrepNtfAsync(PlayerSessionState state, byte[] packetData)
        {
            // Forward skill preparation stance broadcast to other students on map
            await _broadcastDelegate(state, packetData);
        }

        /// <summary>
        /// 0x7926 (31014): MsgGameSkillEndNtf - Skill Cast End / Animation Finish Notice
        /// </summary>
        [PacketHandler(PacketOpcode.MsgGameSkillEndNtf)]
        public async Task HandleSkillEndNtfAsync(PlayerSessionState state, byte[] packetData)
        {
            // Forward skill completion broadcast to other students on map
            await _broadcastDelegate(state, packetData);
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

                int weaponCat = 1; // Default Blade (1=Blade, 2=Glove, 3=Muffler/Blunt, 4=Spirit/Shooting)
                int weaponTypeId = 0;
                if (player.EquippedSlotUids != null && player.EquippedSlotUids.Length > 4 && player.EquippedSlotUids[4] != 0)
                {
                    int wUid = player.EquippedSlotUids[4];
                    var weaponItem = player.StarBeItems?.Find(i => i.Id == wUid || i.SerialId == wUid || (i.TypeId > 0 && i.TypeId == wUid))
                                  ?? player.Inventory?.Find(i => i.Id == wUid);
                    if (weaponItem != null)
                    {
                        weaponTypeId = weaponItem.TypeId > 0 ? weaponItem.TypeId : weaponItem.ItemId;
                        if (_gameDb?.Items.TryGetValue(weaponTypeId, out var wDef) == true && wDef.WeaponType > 0)
                        {
                            weaponCat = wDef.WeaponType;
                        }
                        else
                        {
                            weaponCat = (weaponTypeId / 10000) switch
                            {
                                11 => 1, // Blade
                                12 => 2, // Glove
                                13 => 3, // Blunt
                                14 => 4, // Spirit / Gun
                                140 => 4, // Star Spirit
                                _ => 1
                            };
                        }
                    }
                }

                Logger.Info($"[Combat] '{player.CharacterName}' cast Skill #{skillId} Seq={seqNum} WeaponCat={weaponCat} TargetMainId={targetMainId} TargetsCount={targetsCount}");

                // Consume Charge Point for skill execution
                player.ChargePoint = (byte)Math.Max(0, player.ChargePoint - 1);
                player.GaugeCurrent = 0;
                await state.Session.SendAsync(YogurtingPackets.MakeGameChargePointUpdateNtf(player.ChargePoint, player.GaugeMax, player.GaugeCurrent));

                // Retrieve dynamic skill power multiplier from SkillDesc2.txt (Delphi _Unit49.pas:22478)
                int skillPower = 0;
                if (_gameDb != null && _gameDb.SkillDesc2s.TryGetValue(skillId, out var sDesc2))
                {
                    skillPower = sDesc2.Power;
                }

                int skillAtkRatio = 1;
                if (_gameDb != null && _gameDb.AtkWeapons.TryGetValue(skillId, out var atkDef) && atkDef.AtkRatio > 0)
                {
                    skillAtkRatio = atkDef.AtkRatio;
                }

                var status = _gameDb != null ? _gameDb.GetStatusForLevel(player.Level) : new StatusDef { Pow = player.Level * 4, Luck = player.Level * 2 };
                int fAtk = status.Pow * 65;
                int varianceMax = Math.Max(5, player.Level / 2);

                // Resolve splash damage for all hit targets
                var targetResults = new List<(int targetType, int targetId, int targetX, int targetY, int damage, byte hitType)>();
                var killedMonsters = new List<FieldMonster>();

                int targetOffset = 33;
                for (int i = 0; i < targetsCount && targetOffset + 16 <= packetData.Length; i++)
                {
                    int tType = BitConverter.ToInt32(packetData, targetOffset);
                    int tId = BitConverter.ToInt32(packetData, targetOffset + 4);
                    int tX = BitConverter.ToInt32(packetData, targetOffset + 8);
                    int tY = BitConverter.ToInt32(packetData, targetOffset + 12);
                    targetOffset += 16;

                    // Dynamic Skill damage calculation (Delphi _Unit49.pas:22478: ((FAtk + SkillPower * 100) * AtkRatio) / 10000 + variance)
                    int scaledDmg = (int)(((fAtk + (skillPower * 100)) * (long)skillAtkRatio) / 10000);
                    int levelVariance = _random.Next(0, varianceMax);
                    int dmg = Math.Max(1, (int)((scaledDmg + levelVariance) * player.GetDamageMultiplier()));

                    // Active combat skills in Quartet do not crit (hitType = 0 per Delphi 0060F6A6)
                    targetResults.Add((tType, tId, tX, tY, dmg, (byte)0));

                    // Apply to monster if present
                    if (_gameDb != null && _gameDb.Fields.TryGetValue(player.FieldId, out var fDef))
                    {
                        lock (fDef.Monsters)
                        {
                            var mon = fDef.Monsters.Find(m => m.EntityId == tId && !m.IsDead);
                            if (mon != null)
                            {
                                bool wasKilled = mon.TakeDamage(dmg);
                                if (wasKilled)
                                {
                                    killedMonsters.Add(mon);
                                }
                                else if (!mon.IsDead)
                                {
                                    if (mon.TargetPlayerId == 0)
                                    {
                                        mon.TargetPlayerId = player.CharacterId;
                                        mon.State = MonsterState.Chase;
                                        mon.Frame = 8;
                                    }
                                }
                            }
                        }
                    }
                }

                // If single-target (targetsCount == 0) and targetMainId > 0 (valid target entity)
                if (targetResults.Count == 0 && targetMainId > 0)
                {
                    int baseAtk = player.Pow + (player.Level * 2);
                    int dmg = Math.Max(10, (int)((baseAtk * (skillPower / 100.0f)) + _random.Next(-3, 8)));
                    bool isCrit = _random.Next(0, 100) < 30;
                    if (isCrit) dmg = (int)(dmg * 1.5f);

                    targetResults.Add((targetMainType, targetMainId, targetMainX, targetMainY, dmg, isCrit ? (byte)1 : (byte)0));

                    if (_gameDb != null && _gameDb.Fields.TryGetValue(player.FieldId, out var fDef))
                    {
                        lock (fDef.Monsters)
                        {
                            var mon = fDef.Monsters.Find(m => m.EntityId == targetMainId && !m.IsDead);
                            if (mon != null)
                            {
                                bool wasKilled = mon.TakeDamage(dmg);
                                mon.TargetPlayerId = player.CharacterId;
                                if (wasKilled)
                                {
                                    killedMonsters.Add(mon);
                                }
                                else if (!mon.IsDead)
                                {
                                    mon.State = MonsterState.Chase;
                                    mon.Frame = 6;
                                }
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

                // Process monster kills and loot after skill VFX animation delay
                if (killedMonsters.Count > 0)
                {
                    int skillDelayMs = 500;
                    if (_gameDb != null && _gameDb.SkillDesc2s.TryGetValue(skillId, out var skDef) && skDef.Time > 0)
                    {
                        skillDelayMs = Math.Clamp(skDef.Time * 70, 300, 1500);
                    }
                    else
                    {
                        skillDelayMs = weaponCat switch
                        {
                            1 => 450, // Blade (刀/剣)
                            2 => 550, // Glove (グローブ)
                            3 => 650, // Muffler / Blunt (マフラー/鈍器)
                            4 => 320, // Spirit / Shooting (霊/銃・楽器)
                            _ => 500
                        };
                    }

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(skillDelayMs);
                            foreach (var kMon in killedMonsters)
                            {
                                await ProcessMonsterDefeatAsync(state, player, kMon);
                            }
                        }
                        catch { }
                    });
                }

                // Award proficiency EXP & check for Weapon Mastery Level-Up (0x79EE)
                if (player.DexExps != null && player.DexExps.Length > weaponCat)
                {
                    player.DexExps[weaponCat] += 2;
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
                        int nextReqDex = _gameDb != null ? _gameDb.GetRequiredDexForLevel(newLvl) : 20;
                        Logger.Info($"[Combat] *** WEAPON MASTERY UP! *** '{player.CharacterName}' Weapon Category {weaponCat} reached Level {newLvl}!");

                        byte[] dexLvUpPkt = YogurtingPackets.MakeGameCharDexLvUpNtf(player.CharacterId, weaponCat, newLvl, player.DexExps[weaponCat], nextReqDex);
                        await state.Session.SendAsync(dexLvUpPkt);
                        await _broadcastDelegate(state, dexLvUpPkt);
                    }
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

                // Award reward item dynamically (e.g. Monster drop item, field reward, or valid database item)
                int rewardTypeId = 10001; // Triangle Milk default
                if (_gameDb != null && _gameDb.Fields.TryGetValue(player.FieldId, out var curF) && curF.Monsters.Count > 0)
                {
                    var dropMon = curF.Monsters.Find(m => m.DropItemType > 0);
                    if (dropMon != null)
                    {
                        rewardTypeId = dropMon.DropItemType;
                    }
                }
                else if (_gameDb != null && _gameDb.ShopItems.Count > 0)
                {
                    rewardTypeId = _gameDb.ShopItems[Math.Abs(selectedBoxIndex) % _gameDb.ShopItems.Count].ItemId;
                }

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
