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
                            11 => 1, // Blade
                            12 => 2, // Glove
                            13 => 3, // Blunt
                            14 => 1, // Starter Weapon (140001) uses Blade attack skills 70101..70104
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

                // Calculate authentic weapon combo attack skill ID (70101..70104 for Blade, 70201..70204 for Glove, etc.)
                int comboStep = Math.Min(4, Math.Max(1, (player.ComboCount - 1) % 4 + 1));
                int attackSkillId = 70000 + (weaponCat * 100) + comboStep;

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
                        combo: comboStep - 1,
                        weaponCategory: weaponCat,
                        skillId: attackSkillId,
                        addDexExp: 0);
                    await state.Session.SendAsync(swingAns);
                    await _broadcastDelegate(state, swingAns);
                    return;
                }

                // Check field monsters (Collect all monsters in attack cone / targets list / melee range)
                var hitMonsters = new List<FieldMonster>();
                if (_gameDb != null && _gameDb.Fields.TryGetValue(player.FieldId, out var fieldDef))
                {
                    lock (fieldDef.Monsters)
                    {
                        // 1. Exact match by targetMainId
                        if (targetMainId > 0)
                        {
                            var m = fieldDef.Monsters.Find(mon => mon.EntityId == targetMainId && !mon.IsDead);
                            if (m != null) hitMonsters.Add(m);
                        }

                        // 2. Exact match by any target in list
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

                        // Proximity fallback only if no explicit targets were specified by client
                        if (hitMonsters.Count == 0)
                        {
                            float pX = (float)player.Position.X;
                            float pY = (float)player.Position.Y;
                            float cleaveRangeSq = 3.0f * 3.0f;

                            foreach (var mon in fieldDef.Monsters)
                            {
                                if (mon.IsDead || hitMonsters.Contains(mon)) continue;
                                float dx = mon.X - pX;
                                float dy = mon.Y - pY;
                                if (dx * dx + dy * dy <= cleaveRangeSq)
                                {
                                    hitMonsters.Add(mon);
                                    break; // Single primary melee target
                                }
                            }
                        }
                    }
                }

                if (hitMonsters.Count > 0)
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
                    int baseAtk = Math.Max(5, status.Pow + equipAtk);
                    float dmgMultiplier = player.GetDamageMultiplier(); // e.g. 1.2x if Attack Buff active

                    // Authentic AtkRatio from AtkWeapon.txt (Delphi _Unit49.pas:21990-21996)
                    int atkRatio = 10000;
                    if (_gameDb != null && weaponTypeId > 0 && _gameDb.AtkWeapons.TryGetValue(weaponTypeId, out var atkDef))
                    {
                        atkRatio = atkDef.AtkRatio;
                    }

                    // Authentic LUCK scaling from StatusTable.txt + Critical Buff Multiplier (2.0x in Delphi _Unit49.pas:22007)
                    float critChance = Math.Min(60f, (status.Luck * 1.5f + 5f) * player.GetCritMultiplier());

                    var targetEntries = new List<(int entityId, int x, int y, int damage, bool isCrit)>();
                    var killedMonsters = new List<FieldMonster>();

                    foreach (var targetMonster in hitMonsters)
                    {
                        int scaledAtk = (int)((baseAtk * (long)atkRatio) / 10000);
                        int levelVariance = _random.Next(0, Math.Max(5, player.Level / 2));
                        int damage = Math.Max(3, (int)((scaledAtk + levelVariance) * dmgMultiplier));
                        bool isCrit = _random.Next(0, 100) < critChance;
                        if (isCrit) damage = (int)(damage * 2.0);

                        // Apply damage to monster
                        targetMonster.TakeDamage(damage);
                        targetMonster.TargetPlayerId = player.CharacterId; // Retaliate against attacker

                        targetEntries.Add((targetMonster.EntityId, (int)targetMonster.X, (int)targetMonster.Y, damage, isCrit));
                        Logger.Info($"[Combat] '{player.CharacterName}' attacked '{targetMonster.Name}' (ID: {targetMonster.EntityId}) for {damage} dmg (Crit: {isCrit})! Remaining HP: {targetMonster.CurrentHp}/{targetMonster.MaxHp}");

                        if (targetMonster.IsDead)
                        {
                            killedMonsters.Add(targetMonster);
                        }
                    }

                    // Send 0x7A00 Target Ownership Acquired notice to activate Green Targeting Circle at mob's feet
                    if (hitMonsters.Count > 0)
                    {
                        byte[] lockPkt = YogurtingPackets.MakeGameMonsterOwnershipAcquiredNtf(hitMonsters[0].EntityId);
                        await state.Session.SendAsync(lockPkt);
                    }

                    // Broadcast attack answer & floating damage numbers across all hit enemies (0x791A)
                    byte[] atkAns = YogurtingPackets.MakeGameAttackAns(
                        player.CharacterId,
                        targetEntries,
                        comboStep - 1,
                        weaponCategory: weaponCat,
                        skillId: attackSkillId,
                        addDexExp: 1);

                    await state.Session.SendAsync(atkAns);
                    await _broadcastDelegate(state, atkAns);

                    // Real-time Overhead HP update for damaged living monsters (0x79E8 - Delphi 0x005AFAE4)
                    foreach (var hMon in hitMonsters)
                    {
                        if (!hMon.IsDead)
                        {
                            byte[] monHpPkt = YogurtingPackets.MakeGameMonHpInfoNtf(hMon.EntityId, hMon.CurrentHp, hMon.MaxHp);
                            await state.Session.SendAsync(monHpPkt);
                            await _broadcastDelegate(state, monHpPkt);
                        }
                    }

                    // Synchronously process monster kills immediately following 0x791A (matching Delphi Quartet pipeline)
                    if (killedMonsters.Count > 0)
                    {
                        foreach (var kMon in killedMonsters)
                        {
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
                        comboStep - 1,
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
            // Award EXP using ExpMultiplier (Delphi _Unit49.pas:19020)
            int expEarned = monster.ExpReward > 0 ? monster.ExpReward : (monster.Level * 3 + 2);
            float expMultiplier = player.GetExpMultiplier();
            expEarned = Math.Max(1, (int)(expEarned * expMultiplier));

            // Delphi Loot Drop (_Unit49.pas:19028-19360: Matches HuntMon.txt)
            var lootList = new List<(int itemId, int count, bool isEquip)>();

            // Monster Drop Item / Material / Equipment from HuntMon.txt
            if (monster.DropItemType > 0 && _random.Next(0, 1000) < monster.DropRate)
            {
                int dropCount = Math.Max(1, monster.DropCount);
                bool isEquip = _gameDb != null && _gameDb.Items.TryGetValue(monster.DropItemType, out var idf) && idf.Attack > 0;
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

            // 1. Broadcast Monster Dead & Loot Delivery to Top-Right Cardboard Booty Box (0x5276)
            byte[] deadNtf = YogurtingPackets.MakeGameHuntMonDeadNtf(
                monster.EntityId,
                (ushort)monster.X,
                (ushort)monster.Y,
                player.CharacterId,
                expEarned,
                (int)player.CurrentExp,
                lootList);
            await state.Session.SendAsync(deadNtf);
            await _broadcastDelegate(state, deadNtf);

            // 2. Send EXP Gain notice (0x5277) to update client EXP bar (_Unit47.pas:49068)
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
                player.RecalculateStats(statusLv.Pow, statusLv.Speed, statusLv.Skill, statusLv.Luck);

                Logger.Info($"[Combat] *** LEVEL UP! *** '{player.CharacterName}' reached Level {player.Level}! MaxHP is now {player.MaxHp}.");

                player.SkillPoint++;
                // 1. Broadcast Field/Hunt Level-Up Fanfare & Visual FX (0x5275 - Delphi TMsgGameHuntCharLvUpNtf)
                byte[] huntLvUpNtf = YogurtingPackets.MakeGameHuntCharLvUpNtf(player.Level, (int)player.CurrentExp, (int)player.MaxExp, player.SkillPoint, player.CharacterId);
                await state.Session.SendAsync(huntLvUpNtf);
                await _broadcastDelegate(state, huntLvUpNtf);

                // 2. Synchronize new Level Stats and HP (0x520F + 0x520D)
                await state.Session.SendAsync(YogurtingPackets.MakeGameSetStateNtf(player));
                await state.Session.SendAsync(YogurtingPackets.MakeGameSetHpNtf((ushort)player.CurrentHp));
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
                    await Task.Delay(TimeSpan.FromSeconds(monster.RespawnSeconds));
                    monster.Respawn();
                    monster.Frame = (uint)monster.NextWanderInterval; // Active movement immediately on respawn
                    byte[] respawnNtf = YogurtingPackets.MakeGameMonInfoNtf(monster);
                    await state.Session.SendAsync(respawnNtf);
                    await _broadcastDelegate(state, respawnNtf);
                    Logger.Debug($"[Combat] '{monster.Name}' respawned at ({monster.X}, {monster.Y}).");
                }
                catch { }
            });
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

                // Consume Charge Point for skill execution
                player.ChargePoint = (byte)Math.Max(0, player.ChargePoint - 1);
                player.GaugeCurrent = 0;
                await state.Session.SendAsync(YogurtingPackets.MakeGameChargePointUpdateNtf(player.ChargePoint, player.GaugeMax, player.GaugeCurrent));

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

                    // Skill damage calculation (2.5x base weapon attack + variance)
                    int baseAtk = player.Pow + 20;
                    int dmg = Math.Max(10, (int)(baseAtk * 2.5f + _random.Next(-3, 8)));
                    bool isCrit = _random.Next(0, 100) < 30;
                    if (isCrit) dmg = (int)(dmg * 1.5f);

                    targetResults.Add((tType, tId, tX, tY, dmg, isCrit ? (byte)1 : (byte)0));

                    // Apply to monster if present
                    if (_gameDb != null && _gameDb.Fields.TryGetValue(player.FieldId, out var fDef))
                    {
                        lock (fDef.Monsters)
                        {
                            var mon = fDef.Monsters.Find(m => m.EntityId == tId && !m.IsDead);
                            if (mon != null)
                            {
                                mon.TakeDamage(dmg);
                                mon.TargetPlayerId = player.CharacterId;
                                if (mon.IsDead)
                                {
                                    killedMonsters.Add(mon);
                                }
                            }
                        }
                    }
                }

                // If single-target (targetsCount == 0) and targetMainId > 0 (valid target entity)
                if (targetResults.Count == 0 && targetMainId > 0)
                {
                    int baseAtk = player.Pow + 20;
                    int dmg = Math.Max(10, (int)(baseAtk * 2.5f + _random.Next(-3, 8)));
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
                                mon.TakeDamage(dmg);
                                mon.TargetPlayerId = player.CharacterId;
                                if (mon.IsDead)
                                {
                                    killedMonsters.Add(mon);
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
                            1 => 450, // Blade skill
                            2 => 550, // Blunt skill
                            3 => 650, // Staff skill
                            4 => 320, // Gun skill
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
