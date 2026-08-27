# Volume 5, Chapter 4: Weapon Proficiency & Dexterity Mastery Progression

## 1. Weapon Categories & Array Indexing

Yogurting tracks separate proficiency mastery levels for all 4 weapon disciplines. In both memory and network packets, weapons are indexed in a 5-element array (`0..4`):

| Array Index | Weapon Category | Item Type Range | Core Discipline | Combat Characteristics |
| :--- | :--- | :--- | :--- | :--- |
| **`0`** | **General / Universal** | — | Core Dexterity | Base physical agility. |
| **`1`** | **Blade / Sword** | `110000` – `119999` | Slash / Edge | Fast attack speed, high combo chains, single-target precision. |
| **`2`** | **Glove / Martial Arts** | `120000` – `129999` | Strike / Fist | Rapid charge point generation, knockdown impacts. |
| **`3`** | **Blunt / Club / Bat** | `130000` – `139999` | Smash / Heavy | High base attack, wide horizontal splash damage. |
| **`4`** | **Spirit / Wand / Focus** | `140000` – `149999` | Ranged / Magic | Multi-target burst radius, elemental scaling. |

---

## 2. Proficiency Progression & EXP Requirements

* **DEX EXP Accumulation**: Every successful attack hit with a weapon awards `+1` proficiency EXP to that weapon's category (`player.DexExps[weaponCategory] += 1`).
* **Level-Up Condition**: When `DexExps[cat] >= RequiredDexForLevel(curLevel)`:
  * `DexLevels[cat] += 1`
  * `DexExps[cat] -= RequiredDexForLevel(curLevel)`
  * The server broadcasts `0x7970 (MsgGameCharLvUpNtf)` or `MsgGameCharDexLvUpNtf` to trigger the level-up golden ring visual effect!

### Dexterity Progression Table (`DexTable.txt` / `_Unit49.pas`):
| Mastery Level | Required DEX EXP | Total Cumulative EXP | Stat / Damage Bonus |
| :--- | :--- | :--- | :--- |
| **Level 1** | `20` | `0` | Base weapon damage. |
| **Level 2** | `50` | `20` | +2% Weapon Damage bonus. |
| **Level 3** | `100` | `70` | +4% Weapon Damage bonus. |
| **Level 4** | `180` | `170` | +6% Weapon Damage, unlocks Tier 1 Weapon Skills. |
| **Level 5** | `300` | `350` | +8% Weapon Damage, +1 Charge Point generation. |
| **Level 10** | `1,500` | `3,200` | +20% Weapon Damage, Tier 2 Weapon Skills. |

---

## 3. DEX Level Synchronization in Character State (`0x520F`)

The character's active weapon masteries are continuously synchronized with the client HUD via `0x520F (MsgGameSetStateNtf)` at offsets `+4` to `+8`:

```text
Offset +4:  UInt16 dexLevel_Blade   (e.g. 1)
Offset +6:  UInt16 dexLevel_Glove   (e.g. 1)
Offset +8:  UInt16 dexLevel_Blunt   (e.g. 1)
Offset +10: UInt16 dexLevel_Spirit  (e.g. 1)
```
