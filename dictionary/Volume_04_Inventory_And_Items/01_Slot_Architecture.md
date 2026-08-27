# Volume 4, Chapter 1: Inventory Slot Architecture & Paperdoll Mapping

## 1. Dual-Slot State Model (`SlotType` & `SlotIndex`)

Yogurting uses a dual-state indexing model to track whether an item resides in the backpack inventory or is actively equipped on the character paperdoll:

```text
+-------------------+--------------------+------------------------------------------+
| SlotType Value    | Meaning            | SlotIndex Range & Semantics              |
+-------------------+--------------------+------------------------------------------+
| SlotType = 0      | Backpack Inventory | SlotIndex 0..47 (Grid Inventory Cells)   |
| SlotType = 1      | Equipped Paperdoll | SlotIndex 0..11 (Specific Body Position) |
+-------------------+--------------------+------------------------------------------+
```

---

## 2. The 12 Paperdoll Equipment Positions (`SlotType = 1`)

When an item is equipped, its `SlotIndex` matches one of the 12 defined paperdoll anchors:

| Equip Position | Slot Index | Category Name | Typical Item Type Range | Example Items |
| :--- | :--- | :--- | :--- | :--- |
| **`0`** | `0` | **Hat / Headgear** | `150100` – `150199` | Beret, Estiva Cap, Ribbon |
| **`1`** | `1` | **Hairstyle / Wig** | `1202400` – `1202499` | Long Twin Tails, Short Shaggy |
| **`2`** | `2` | **Face / Glasses / Mask** | `150200` – `150299` | Glasses, Cat Whisker Mask |
| **`3`** | `3` | **Earrings / Accessories**| `150300` – `150399` | Pearl Earrings, Silver Studs |
| **`4`** | `4` | **Main Weapon (Right Hand)**| `110000` – `149999` | Blade, Glove, Blunt, Spirit |
| **`5`** | `5` | **Off-hand / Gloves** | `1202200` – `1202299` | Combat Gloves, Leather Mitts |
| **`6`** | `6` | **Backpack / Back Wing** | `150400` – `150499` | School Bag, Angel Wings |
| **`7`** | `7` | **Top / Uniform Shirt** | `150000` – `150049` | Estiva Summer Top, Winter Blazer |
| **`8`** | `8` | **Bottom / Skirt / Pants** | `150050` – `150099` | Pleated Skirt, School Slacks |
| **`9`** | `9` | **Shoes / Loafers** | `150060` – `150099` | Estiva Winter Shoes, Sneakers |
| **`10`** | `10` | **Special / Aura / Effect** | `160000` – `169999` | Sparkle Aura, Taff Balloon |
| **`11`** | `11` | **Ring / Title Badge** | `170000` – `179999` | Honor Student Ring |

---

## 3. Item Identification: Serial ID vs Instance UID vs Type ID

Every item instance in memory and database is tracked through 3 distinct identifiers:

1. **`TypeId` (`typeBeItem`)**: 
   * The static item template ID defined in `ItemTable.txt` (e.g. `150010` = Estiva Summer Top F).
   * Determines weapon category, attack power, grade requirement, and 3D visual mesh.
2. **`SerialId` (`idBeItem`)**: 
   * Unique 64-bit (`Int64`) persistent database serial number assigned when the item is created/looted.
3. **`UID` (`Entity UID`)**: 
   * Sequential integer (`1..N`) used in packet transmissions for short-form referencing.
