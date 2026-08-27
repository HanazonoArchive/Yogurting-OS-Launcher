# 📖 The Yogurting Online Revival Encyclopedia
### *The Definitive Standalone Protocol, Architecture & Data Specification*

Welcome to the **Yogurting Revival Encyclopedia**. This comprehensive technical dictionary documents the entire architecture, network protocol, state machines, binary packet layouts, formulas, and client-server workflows of *Yogurting Online*.

Every opcode, packet structure, and system documented here is backed by:
1. **Raw AnaYg DMS Scripts** (`./dms/*.dms`): 270 verified packet parser scripts with authentic Japanese and Korean field identifiers.
2. **Legacy Delphi Disassembly** (`server_legacy/DELPHI PROJECT/`): Decompiled server logic from `_Unit47.pas`, `_Unit49.pas`, `_Unit67.pas`, and `UYgDB.pas`.
3. **Client Reverse-Engineering**: Vtable, class, and memory layouts extracted from `LuGameEngineDX9.dll` and `Yogurting.exe`.
4. **Live Packet Captures**: Verified hex payloads from `tools/packet_logs/`.

---

## 📚 Table of Contents & Volume Overview

```
server_modern/dictionary/
├── README.md                              # This Master Index
│
├── dms/                                   # Standalone Raw DMS Scripts & Reference Texts
│   ├── 20001.dms ... 42005.dms            # 270 standalone DMS parser scripts
│   ├── struct.dms                         # Universal structs (CharDisp, CharaInfo, Items, Rooms)
│   ├── packet_structures_jp.txt           # Master Japanese compilation
│   ├── packet_structures_en.txt           # Master English annotated compilation
│   └── script_syntax_guide.txt            # DMS parser grammar & data types guide
│
├── Volume_01_Core_Protocol/
│   ├── 01_Packet_Header_And_Framing.md    # 4-byte prefix, 2-byte opcode, alignment rules
│   ├── 02_Data_Types_And_Encoding.md      # Shift-JIS vs UTF-16LE, MapPoints, Direction vectors
│   └── 03_Master_Opcode_Index.md          # Complete directory of all 270 opcodes mapped to DMS/Delphi
│
├── Volume_02_Authentication_And_Login/
│   ├── 01_Auth_Handshake_0x7595_0x7596.md # MsgAuthTypeNtf, MsgLoginAuthReq, Session key issuance
│   ├── 02_Character_Selection_0x7597.md   # Exact 458-byte CharDisp payload & equipment memory layout
│   └── 03_Character_Creation_0x75A8.md    # Name check, Phone check, Hair/Face/Gender validation
│
├── Volume_03_Campus_And_World/
│   ├── 01_Field_Loading_Lifecycle.md      # Field loading sequence (0x795A -> 0x795B -> 0x7956 -> 0x520B -> 0x7968)
│   ├── 02_Warp_Gates_And_Portals.md       # Gate triggers (0x7965), Fadeout, Warp start (0x7966), Warp gate req (0x7967)
│   └── 03_Interactive_Entities.md         # Terminal kiosks (0x521B), Campus NPCs (0x7942), Props
│
├── Volume_04_Inventory_And_Items/
│   ├── 01_Slot_Architecture.md            # Slot types (0=Inventory, 1=Equipped), 12 equip positions
│   ├── 02_Item_Classification.md          # Normal equipment, Star BeItems, Consumables (CoItems)
│   ├── 03_Equip_And_Unequip_Flows.md      # 0x7944/0x7945, 0x5265/0x5266, live paperdoll sync
│   └── 04_Reinforce_And_Crystal.md        # Crystal levels (0x79A8), stone attachment, crystallization (0x79AA)
│
├── Volume_05_Combat_And_Monsters/
│   ├── 01_Attack_Packets_0x7919_0x791A.md # Player attack swings, target arrays, damage calculation
│   ├── 02_Monster_Spawns_0x796E.md        # Generator XML mapping, stats, direction, ownership
│   ├── 03_Damage_And_EXP_Formulas.md      # Delphi exact math (_Unit49.pas:18999, combo thresholds)
│   └── 04_Weapon_Proficiency_DEX.md       # 4 weapon categories (Blade, Glove, Blunt, Spirit) & leveling
│
├── Volume_06_Economy_And_Shops/
│   ├── 01_Star_Cash_Shop.md               # Lifecycle (0x5233 -> 0x5234 -> 0x523C -> 0x523D -> 0x5235)
│   ├── 02_Store_Auntie_And_Vending.md     # 0x5225 / 0x5227 buy/sell flows, ItemTable pricing
│   └── 03_Player_To_Player_Trade.md       # 0x792B trade propose, basket lock, final confirm
│
├── Volume_07_Episodes_And_Lobby/
│   ├── 01_Lobby_And_Room_Listing.md       # 0x765E / 0x766C room browser, page status
│   ├── 02_Waiting_Room_Management.md      # 0x7678 team selection, readiness, host start (0x768C)
│   └── 03_Episode_Lifecycle_And_Clear.md  # Timers, mission objectives (0x7990/0x7993), loot box (0x7974)
│
├── Volume_08_Social_And_Messenger/
│   ├── 01_Comm_Server_Port_10004.md       # Friend proposals (0x7728), online status, heartbeats (0x7759)
│   ├── 02_Chat_And_Announcements.md       # General, Club, Whispers, System announcements (0x7964)
│   └── 03_Clubs_And_Guilds.md             # Club creation, member roster, nametag colors
│
└── Volume_09_Complete_Packet_Catalog/     # Grouped numerical catalog of all 270 opcodes
```

---

## 🏛️ Server Architecture & Port Map

Yogurting Online distributes its game world across 4 dedicated TCP server daemon processes:

| Server Daemon | Default Port | Opcode Range | Primary Responsibilities |
| :--- | :--- | :--- | :--- |
| **LoginServer** | `10000` | `0x7595` – `0x75AE` | Authentication, World List, Character Selection, Character Creation/Deletion. |
| **FieldServer** (School) | `10002` | `0x5200` – `0x5274`<br>`0x7900` – `0x79FF`<br>`0xA400` – `0xA415` | Campus maps, Hunting zones, Movement, NPCs, Item inventory, Combat, Shops, Gacha. |
| **EpisodeServer** | `10003` | `0x765E` – `0x7695`<br>`0x7990` – `0x79BF` | Lobby rooms, Matchmaking, Episode instances, Mission objectives, Clear rankings. |
| **CommServer** (Messenger) | `10004` | `0x7604`<br>`0x7720` – `0x7760` | Friend lists, Whisper routing, Club/Guild channels, Cross-server presence. |

---

## 🔍 How to Use This Dictionary

1. **For Feature Implementation**: Read the corresponding **Subsystem Volume (Volumes 2–8)** to understand the full state machine and packet sequences before writing handlers.
2. **For Binary Packet Serialization**: Cross-reference the **Memory Layout Table** in the chapter with the raw DMS script in [`./dms/`](./dms/).
3. **For Mathematical Validation**: Consult **Volume 5 (Combat & Formulas)** for exact Delphi decompiled formulas (damage multipliers, luck/critical scaling, EXP dropoffs).
