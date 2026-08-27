# Volume 2, Chapter 2: Character Selection Payload Specification (0x7597 / 30103)

## 1. Overview & Protocol Role

`0x7597 (30103): MsgLoginAuthAns` is the primary character display packet returned by the **LoginServer** upon successful account authentication. It supplies the client with the 3D character preview model, school background, facial features, equipped clothing, reinforce stone sockets, and security authentication tokens.

* **Direction**: Server $\longrightarrow$ Client
* **Port**: `10000 (LoginServer)`
* **Delphi Class**: `TMsgLoginAuthenticationAns` (`_Unit47.pas:005AAA34`)
* **AnaYg Script**: [`30103.dms`](../dms/30103.dms) referencing [`struct.dms`](../dms/struct.dms) (`char_disp_info`)

---

## 2. Complete Memory Layout Table (452 Bytes Payload / 458 Bytes Total)

| Offset (Hex) | Offset (Dec) | Field Name | Type | Size | Description | Example Values |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `0x000` | `+0` | `ReturnCode` | `UInt32` | 4B | Authentication Result (`1` = Success, `0` = Failed) | `0x00000001` |
| `0x004` | `+4` | `WorldName` | `WStr[11]` | 22B | Server Cluster Name (UTF-16LE, null padded) | `"Estiva\0..."` |
| `0x01A` | `+26` | `SchoolName` | `WStr[33]` | 66B | Academy / School Name (UTF-16LE, null padded) | `"Estiva Academy\0..."` |
| **`0x05C`** | **`+92`** | **`char_disp_info`** | **Struct** | **356B** | **Full 3D Character Model & Equipment Block** | *(See Section 3 below)* |
| `0x1C0` | `+448` | `cntWait` | `UInt32` | 4B | Server Queue Position / Wait Count | `0x00000000` |

---

## 3. Detailed `char_disp_info` Sub-Structure (356 Bytes)

| Offset in Struct | Field Name | Type | Size | Description | Valid Values |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `+0` | `CharacterId` | `Int32` | 4B | Unique Character Database ID | `256`, `1001` |
| `+4` | `CharacterName` | `WStr[13]` | 26B | UTF-16LE Character Nickname | `"Hanazono\0..."` |
| `+30` | `PhoneNumber` | `UInt32` | 4B | In-Game 8-Digit Phone Number | `10000001` |
| `+34` | `SessionKey` | `UInt32` | 4B | Dynamic Redirect Token for Field & Comm Servers | `872863` |
| `+38` | `School` | `Byte` | 1B | Academy (`1` = Estiva, `2` = So-il) | `1` or `2` |
| `+39` | `Gender` | `Byte` | 1B | Character Gender (`1` = Male, `2` = Female) | `1` or `2` |
| `+40` | `FaceId` | `Byte` | 1B | Facial Model Preset Index | `1` to `48` |
| `+41` | `HairId` | `Byte` | 1B | Hairstyle Preset Index | `1` to `48` |
| `+42` | `SkinColor` | `Byte` | 1B | Skin Tone Palette Index | `1` to `8` |
| `+43` | `HairColor` | `Byte` | 1B | Hair Color Palette Index | `1` to `8` |
| `+44` | `BirthMonth` | `Byte` | 1B | Birthday Month | `1` to `12` |
| `+45` | `BirthDay` | `Byte` | 1B | Birthday Day | `1` to `31` |
| `+46` | `BloodType` | `Byte` | 1B | Blood Type (`1`=A, `2`=B, `3`=O, `4`=AB) | `1` to `4` |
| `+47` | `Level` | `Byte` | 1B | Character Base Level | `1` to `100` |
| `+48` | `Grade` | `Byte` | 1B | School Year / Grade | `1` to `3` |
| `+49` | `DexLevels[5]` | `Byte[5]` | 5B | Weapon Mastery (0:General, 1:Blade, 2:Glove, 3:Blunt, 4:Spirit) | `1, 1, 1, 1, 1` |
| `+54` | `Padding` | `Byte[2]` | 2B | 2-byte alignment padding | `0x00, 0x00` |
| **`+56`** | **`EquippedSlots[12]`** | **Array** | **288B** | **12 Visual Equipment Slots (24 bytes per slot)** | *(See Section 4)* |

---

## 4. Visual Equipment Slot Layout (12 Slots $\times$ 24 Bytes)

In the Character Selection preview screen, the client renders up to 12 equipped items:

```text
Slot 0:  Hat / Headgear          Slot 6:  Costume / Accessory
Slot 1:  Hairstyle Preset        Slot 7:  Top / Upper Uniform
Slot 2:  Face / Mask             Slot 8:  Bottom / Skirt / Pants
Slot 3:  Earrings / Glasses      Slot 9:  Shoes / Boots
Slot 4:  Right Hand (Weapon)     Slot 10: Special / Backpack
Slot 5:  Left Hand (Weapon/Acc)  Slot 11: Ring / Badge
```

### Memory Structure of Each Slot (24 Bytes):
```text
Offset +0:  UInt16 idBeItemDim1Index  (Grid Dimension X / Slot Index)
Offset +2:  UInt16 idBeItemDim2Index  (Grid Dimension Y)
Offset +4:  Int32  idBeItem           (Item Instance UID / Serial ID)
Offset +8:  Int32  typeBeItem         (Item Type ID from ItemTable.txt, e.g. 150005)
Offset +12: Int32  reinforceSlot1     (Reinforce Stone 1 Type ID, 0 if empty)
Offset +16: Int32  reinforceSlot2     (Reinforce Stone 2 Type ID, 0 if empty)
Offset +20: Int32  reinforceSlot3     (Reinforce Stone 3 Type ID, 0 if empty)
```

---

## 5. AnaYg Script (`30103.dms`)

```javascript
import struct.*;

function parse(packet){
    packet.setTitle("ログイン認証返答");
    packet.readUInt32("戻り値");           // ReturnCode
    packet.readWStr(0x16, "ワールド名");   // WorldName (22 bytes)
    packet.readWStr(0x42, "学校名");       // SchoolName (66 bytes)
    char_disp_info(packet);                // char_disp_info (356 bytes)
    packet.readUInt32("cntWait");          // cntWait (4 bytes)
}
```
