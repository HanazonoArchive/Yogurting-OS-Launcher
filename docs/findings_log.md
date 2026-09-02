# Findings Log

This log tracks verified facts, source conflicts, and technical decisions made during the development of the modern server rewrite, per the `documentation_and_citation` rule.

---

### [2026-09-02] Character Selection 0x7597 Struct Layout Documentation Conflict
- Status: CONFLICTED
- Sources: `dictionary/dms/struct.dms`, `server_legacy/DELPHI PROJECT/_Unit47.pas`, `server_modern/dictionary/Volume_02_Authentication_And_Login/02_Character_Selection_0x7597.md`
- Finding: The markdown documentation in Volume 2 §2 asserts 1-byte values for Grade, School, Gender, FaceId and 12 equipment slots of 24 bytes. However, the authoritative AnaYg grammar (`struct.dms`) and Quartet disassembly (`_Unit47.pas`) define `char_disp_info` using 13 4-byte/8-byte/WStr fields followed by 9 parts of 5 reinforce slots.
- Notes: The modern server implementation in `YogurtingPackets.WriteCharDispInfo` conforms to `struct.dms` and Delphi `TYgPacket`. The Volume 2 documentation must be marked as conflicted/erroneous.

### [2026-09-02] Phase 4 Field Loading Completion Handshake
- Status: VERIFIED
- Sources: `dictionary/Volume_03_Campus_And_World/01_Field_Loading_Lifecycle.md`, `server_legacy/DELPHI PROJECT/_Unit49.pas`
- Finding: Client map initialization on 0x795B requires `0x520F` (`MsgGameSetStateNtf`) followed by `0x7968` (`MsgGameWarpResultNtf`) to unclamp the campus 3D camera and fade in the character model.
- Notes: Implemented in `MovementAndFieldHandlers.HandleEmoteAsync`.

### [2026-09-02] Campus Chat Opcode Ownership (0x7963)
- Status: VERIFIED
- Sources: `dictionary/README.md` (Port Map), `server_modern/src/Yogurting.Core/Network/PacketOpcodes.cs`
- Finding: `MsgGameChatReq` (`0x7963` / 31075) belongs to the FieldServer port :10002 opcode space (`0x7900`–`0x79FF`), not CommServer.
- Notes: Moved handler from orphaned `CommHandlers.cs` into `MovementAndFieldHandlers.HandleChatAsync`, broadcasting campus chat to all players in the zone.

### [2026-09-02] Socket Write Serialization
- Status: VERIFIED
- Sources: .NET 8 `System.Net.Sockets.NetworkStream` concurrency documentation
- Finding: Concurrent calls to `NetworkStream.WriteAsync` cause `InvalidOperationException` and frame interleaving.
- Notes: Added `SemaphoreSlim(1, 1)` write lock per `ClientSession` in `AsyncTcpServer.cs`.

### [2026-09-02] Combat Damage Formula & AtkWeapon Discrepancies
- Status: VERIFIED
- Sources: `server_legacy/DELPHI PROJECT/_Unit49.pas` (lines 20268-20280, 21980-22010, 22440-22490), `server_legacy/DELPHI PROJECT/_Unit48.pas` (lines 23-35, 1080-1090), `server_legacy/quartet.exe` (RTTI CSVColumn attribute table for `TAtkWeaponData`), `server_legacy/db/AtkWeapon.txt`, `StatusTable.txt`, `SkillDesc2.txt`.
- Finding: In `server_modern`, Level 1 normal attacks dealt 127–136 damage (and 258–268 crits), one-shotting 18 HP monsters due to four compounding bugs: (1) `GameDatabase.cs:287` mapped `BeItemType.AttackGroup` (102 for Blade) to `item.Attack` as flat damage, (2) `CombatHandlers.cs:206` looked up `AtkWeapon` using `weaponTypeId` (110001) instead of combo `attackSkillId` (10201), defaulting to 100% multiplier, (3) `GameDatabase.cs:682` loaded `row.Area` (102031) as `AtkRatio` instead of Column 5 (`Power` = 63), and (4) combat damage bypassed original Quartet fixed-point math. Quartet calculates `FAtk = (FPow * 65) + (BonusAtk * 100)` and `Damage = ((FAtk * AtkRatio) / 10000) + Random(0..Max(5, Level/2))`, which yields 1–5 damage per hit for a Level 1 player (3–5 hits to kill an 18 HP monster).
- Notes: Requires updating `GameDatabase.cs` loaders for `BeItemType` and `AtkWeapon`, and updating `CombatHandlers.cs` normal attack and active skill damage calculation to replicate Quartet's byte-accurate formula.

### [2026-09-02] Mob AI State Machine, Pathing, and Loot System Audit
- Status: VERIFIED
- Sources: `server_legacy/DELPHI PROJECT/_Unit49.pas` (lines 1009-1065, 17560-17950, 18360-19450, 22037), `_Unit47.pas` (lines 15042-15126, 48838-49033, 57278-57320), `server_modern/src/Yogurting.Server/World/FieldInstance.cs`, `CombatHandlers.cs`, `FieldMonster.cs`.
- Finding: Reverse-engineered Quartet's 5-state AI loop (`stWait`, `stWalk`, `stChase`, `stAttack`, `stDead`):
  1. `stWalk`: Movement steps coordinates per frame along unit direction vector `DeltaX, DeltaY` at 0.75 tiles/s. `server_modern` currently teleports coordinates instantly on wander, causing coordinate desync.
  2. `stChase`: Uses Motion 2 (Run animation) at 2.0 tiles/s. Leash breaks strictly at 16 tiles from spawn base point `FBasePoint` (`0060C394`), not 30 tiles from current position.
  3. Ownership release: Leash breaks, player deaths, and monster resets must broadcast `0x7A01` (`MsgGameMonsterOwnershipLostNtf`) to clear client targeting lock rings.
  4. Melee combat distance: Checked as Chebyshev distance `abs(dx) < 2 && abs(dy) < 2` (allowing 8 adjacent tiles including diagonals). Euclidean check `dist <= 1.4f` incorrectly rejected diagonal tiles ($\sqrt{2} \approx 1.414\text{f}$).
  5. Monster attack miss chance & damage: 20% miss rate (`Random(100) < 20`), min damage 1 (`Math.Max(1, ...)`).
  6. EXP reward: Scales parabolically with level difference (`Exp * (1.0 - diff^2 * 0.003907)` with 1 EXP floor if $|PlayerLv - MobLv| > 15$).
  7. Loot: Field hunt monsters drop directly to inventory and send `0x5276` (`MsgGameHuntMonDeadNtf`), matching `server_modern`.
- Notes: Detailed in `audit_report.md`. Awaits user confirmation before proceeding with implementation.

### [2026-09-02] Monster Death Packet Timing and Wire Layout
- Status: VERIFIED
- Sources: `server_legacy/DELPHI PROJECT/_Unit49.pas` (lines 19020-19450, 22285-22310, 22820-22870), `_Unit47.pas` (lines 48845-49048), `server_modern/logs/server_20260902_212610.log`, `server_modern/src/Yogurting.Server/Handlers/Field/CombatHandlers.cs`, `YogurtingPackets.cs`.
- Finding: In live testing, monsters whose HP reached 0 remained standing with empty HP bars until an extra player attack swing. Disassembly of Delphi Quartet revealed three compounding causes: (1) `CombatHandlers.cs` sent `0x79E8` (`MsgGameMonHpInfoNtf`) with `HP = 0` on every lethal attack; Delphi never sends `0x79E8` on attack damage (the client deducts HP locally from `0x791A` `MsgGameAttackAns`), (2) `CombatHandlers.cs` sent `0x7A01` (`MsgGameMonsterOwnershipLostNtf`) on monster defeat; Delphi `TMonster.Dead` never sends `0x7A01` on death (sending it caused the client to strip ownership/target reticle and ignore or stall the death animation), (3) `MakeGameHuntMonDeadNtf` (`0x5276`) wrote `monsterType` for the trailing `type` field; Delphi `_Unit47.pas:005AA793` writes `0` (`xor edx, edx; call WriteInt32`). Removing `0x79E8` and `0x7A01` from the combat loop and setting trailing type to 0 aligns the death pipeline cleanly: `0x791C` -> `0x791A` -> `0x5276` -> `0x5277`.
- Notes: Verified with 12/12 unit tests including `Combat_MsgGameHuntMonDeadNtf_ExactDelphiWireLayout`.

### [2026-09-02] Player Full HP Restore on Level Up
- Status: VERIFIED
- Sources: `server_legacy/DELPHI PROJECT/_Unit49.pas` (lines 20330-20370, `0060DD66-0060DD75`, 22920-22960), `_Unit47.pas` (lines 48792-48830).
- Finding: In original Delphi Quartet, when `TChara.CalcStatus(IsLevelUp: Boolean)` is called during level-up (`IsLevelUp = True`), it explicitly sets `FInnerCurrentHP = FMaxHP` (`0060DD6A: fild dword ptr [eax+74]; fstp tbyte ptr [eax+58]`), completely restoring the player's HP to 100% full health, followed by `TMsgGameSetHpNtf (0x7940)` to synchronize the client's HP orb/bar. Implemented via `Player.RecalculateStats(..., isLevelUp: true)` in `CombatHandlers.cs` on `player.Level++`.
- Notes: Verified with unit test `Player_RecalculateStats_RestoresFullHpOnLevelUp`.


