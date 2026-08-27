# Volume 4, Chapter 4: Reinforcement, Sockets & Crystallization

## 1. Item Enhancement Systems

Yogurting provides three distinct equipment modification systems:

```text
1. Reinforce Stone Attachment (0x794A)
   - Sockets 1 to 5 on BeItems accept EnItem Reinforce Stones.
   - Adds fixed Attack, Defense, HP, or Expand Gauge.

2. Crystal Level Upgrading (0x79A8 / 0x79A9)
   - Upgrades an existing Crystal to a higher enhancement tier.

3. Item Crystallization (0x79AA / 0x79AB)
   - Dismantles unwanted weapons or clothes to produce raw EnItem Crystals.
```

---

## 2. Opcode Specifications

### 1. Opcode 0x794A (31050): `MsgGameReinforceItemReq` [アイテム強化要求]
* **Direction**: Client $\longrightarrow$ Server
* **Delphi Class**: `TSchoolSession.sub_006C12E0`
* **AnaYg Script**: [`31050.dms`](../dms/31050.dms)
* **Payload (16 Bytes)**:
  * `0x00`: `Int32 targetItemUid` (The weapon or clothing receiving the stone)
  * `0x04`: `Int32 stoneItemUid` (The Reinforce Stone consumed)
  * `0x08`: `Int32 socketIndex` (Target Socket `0` to `4`)
  * `0x0C`: `Int32 unk`

---

### 2. Opcode 0x79A8 (31144): `MsgGameEnchantCrystalReq` / `Ans` (0x79A9)
* **Direction**: Client $\longrightarrow$ Server (`0x79A8`) / Server $\longrightarrow$ Client (`0x79A9`)
* **AnaYg Scripts**: [`31144.dms`](../dms/31144.dms), [`31145.dms`](../dms/31145.dms)
* **Delphi Handlers**: `TSchoolSession.sub_006C1D68`, `TMsgGameEnchantCrystalLevelAns.Create` (`0x005AEDF0`)

#### `MsgGameEnchantCrystalAns` Payload (8 Bytes):
| Offset | Field Name | Type | Size | Description |
| :--- | :--- | :--- | :--- | :--- |
| `0x00` | `enchantType` | `Int32` | 4B | Crystal Enchant Category |
| `0x04` | `newLevel` | `Int32` | 4B | Resulting Enhancement Level |

---

### 3. Opcode 0x79AA (31146): `MsgGameCrystallizeReq` / `Ans` (0x79AB)
* **Direction**: Client $\longrightarrow$ Server (`0x79AA`) / Server $\longrightarrow$ Client (`0x79AB`)
* **AnaYg Scripts**: [`31146.dms`](../dms/31146.dms), [`31147.dms`](../dms/31147.dms)
* **Delphi Handlers**: `TSchoolSession.sub_006C1E20`, `TMsgGameEnchantCrystallizeAns.Create` (`0x005AEE6C`)

#### `MsgGameCrystallizeAns` Payload Structure:
* Returns an array of consumed item IDs and the resulting `EnItem` crystal type and count generated from the dismantle formula in `_Unit49.pas`.
