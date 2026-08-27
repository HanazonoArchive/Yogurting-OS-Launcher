# Volume 4, Chapter 2: Item Classification, Types & Prefix Masks

## 1. Item Classification Hierarchy

Yogurting categorizes all in-game assets into 5 distinct item classes governed by the high-byte prefix mask in `struct.dms`:

```text
High-Byte Mask (Bitwise & 0xFF000000)
├── 0x01000000 : Taff (In-Game Currency / Points)
├── 0x02000000 : BeItem (Equippable Wearables & Weapons with Sockets)
├── 0x03000000 : CoItem (Consumables, Potions & Food)
├── 0x04000000 : QuestItem (Episode Mission Objectives & Keys)
└── 0x05000000 : EnItem (Reinforcement Stones & Enchantment Crystals)
```

---

## 2. Item Class Details

### 1. BeItem (Equippable Items / Wearables)
* **Definition**: All permanent armor, uniforms, hairstyles, and weapons.
* **Socket Slots**: Every BeItem contains **5 Reinforce Sockets** (`reinforceslot[0..4]`) that can hold `EnItem` enhancement stones.
* **Sub-categories**:
  * `Weapon`: Blade (`11xxxx`), Glove (`12xxxx`), Blunt (`13xxxx`), Spirit (`14xxxx`).
  * `Costume / Cloth`: Top (`1500xx`), Bottom (`15005x`), Shoes (`15006x`), Hat (`1501xx`).

---

### 2. Star BeItem (Premium / Cash Items)
* **Definition**: Items purchased from the Star Cash Shop (`0x5233` / `0x523C`) or activated via buffs.
* **Lifespan**: Contains an active timer (`remainTimeInSecond`) counting down to item expiration.
* **Visual Effects**: Can provide ID Tag badges (`bShowIdTag`), background borders (`bgTypeIdTag`), and overhead text placards (`bShowPicket`).

---

### 3. CoItem (Consumable Items)
* **Definition**: Stackable items consumed on use (`countCoItem`).
* **Usage**: Health recovery (Strawberry Milk, Banana Milk), Energy snacks, and EXP boosters.
* **Packet**: Consumed via `MsgGameUseCoItemReq` (`0x7928`) $\rightarrow$ `MsgGameUseCoItemAns` (`0x7929`).

---

### 4. EnItem (Enhancement Crystals & Reinforce Stones)
* **Definition**: Stones attached to BeItem sockets to increase Attack (`Atk`), Defense (`Def`), Max HP, or Expand Charge Gauge.
* **Grades**: Grades 1 to 5 (Bronze, Silver, Gold, Platinum, Diamond).
* **Usage**: Applied via `MsgGameReinforceItemReq` (`0x794A`) or crystallized via `MsgGameCrystallizeReq` (`0x79AA`).
