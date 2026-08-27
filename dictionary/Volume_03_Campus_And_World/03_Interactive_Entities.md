# Volume 3, Chapter 3: Interactive Entities, NPCs, Kiosks & Objects

## 1. World Object Classification

World objects in Yogurting are divided into three primary categories:

```text
1. Interactive Terminal Objects (0x521B / 0x5227)
   - Episode Matchmaking Kiosks (ObjectType = 2)
   - Hairdresser Salon Stations (ObjectType = 6)
   - Vending Machines & Storage Lockers

2. Campus & Visual NPCs (0x7942)
   - Store Auntie, School Nurse, Headmaster, Teachers, Club Promoters

3. Field Warp Gates (0x795C - Type 10)
   - Campus Portals & Outdoor Transition Gates
```

---

## 2. Opcode 0x521B (21019): `MsgObjectCreateNtf` [オブジェクト生成通知]

* **Direction**: Server $\longrightarrow$ Client
* **Port**: `10002 (FieldServer)`
* **Delphi Class**: `TMsgObjectCreateNtf` (`_Unit47.pas:005A958C`)
* **AnaYg Script**: [`21019.dms`](../dms/21019.dms)

### Memory Layout Table (32 Bytes Payload)

| Offset | Field Name | Type | Size | Description | Example Values |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `0x00` | `objectId` | `Int32` | 4B | Unique Entity ID in current field | `1`, `2`, `3` |
| `0x04` | `objectType` | `Int32` | 4B | Functional Object Type (`2` = Episode Gate, `6` = Hairdresser) | `2` |
| `0x08` | `subId` | `Int32` | 4B | Sub-category / Episode ID target | `20004` |
| `0x0C` | `idCli` | `Int32` | 4B | Client resource binding ID | `659` |
| `0x10` | `shell` | `Int32` | 4B | 3D Visual Mesh Shell ID | `62` |
| `0x14` | `worldX` | `Float32` | 4B | Floating-point world X coordinate ($\text{Grid } X \times 4.0$) | `664.0f` |
| `0x18` | `worldY` | `Float32` | 4B | Floating-point world Y coordinate ($\text{Grid } Y \times 4.0$) | `322.0f` |
| `0x1C` | `dir` | `Byte` | 1B | 8-way Facing Direction (`0` to `7`) | `1` |
| `0x1D` | `bActive` | `Byte` | 1B | Active Flag (`1` = Active, `0` = Inactive) | `1` |
| `0x1E` | `bVisible` | `Byte` | 1B | Visible Flag (`1` = Rendered, `0` = Hidden) | `1` |
| `0x1F` | `padding` | `Byte` | 1B | CRT alignment padding | `0xCC` |

---

## 3. Opcode 0x7942 (31042): `MsgGameVisualAttachNtf` [NPCビジュアル生成通知]

* **Direction**: Server $\longrightarrow$ Client
* **Delphi Class**: `TMsgGameNpcCreateNtf` (`_Unit47.pas:005AD088`)
* **AnaYg Script**: [`31042.dms`](../dms/31042.dms)

### Memory Layout Table (19 Bytes Payload)

| Offset | Field Name | Type | Size | Description | Example Values |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `0x00` | `npcId` | `Int32` | 4B | Unique NPC Instance ID | `176`, `175`, `174` |
| `0x04` | `shellType` | `Int32` | 4B | NPC Visual Character Mesh / Costume ID | `39` (Teacher), `40` (Auntie) |
| `0x08` | `gridX` | `UInt16` | 2B | Grid X coordinate | `163` |
| `0x0A` | `gridY` | `UInt16` | 2B | Grid Y coordinate | `81` |
| `0x0C` | `dir` | `Int32` | 4B | Heading direction (`0` to `7`) | `5` |
| `0x10` | `padding` | `Byte[3]` | 3B | CRT alignment padding | `0xCC, 0xCC, 0xCC` |

---

## 4. Opcode 0x5220 (21024): `MsgObjectUseAns` [オブジェクト使用返答]

* **Direction**: Server $\longrightarrow$ Client
* **Function**: Returned when a player clicks on an interactive terminal, door, or portal. Unlocks the client cursor and initiates the interaction dialog or episode window.
* **Payload (8 Bytes)**:
  * `0x00`: `Int32 returnCode` (`0` = Success)
  * `0x04`: `Int32 objectId` (Target Object Entity ID)
