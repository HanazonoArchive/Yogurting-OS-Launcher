# Volume 8, Chapter 2: In-Game Chat, Whispers & Announcements

## 1. Chat Message Classification & Text Colors

Yogurting categorizes in-game chat messages using color tokens defined in `_Unit47.pas`:

| Color ID | Chat Category | Prefix / Scope | Visual Font Color in Client |
| :--- | :--- | :--- | :--- |
| **`0`** | **General / Area Chat** | Zone Broadcast | White (`#FFFFFF`) |
| **`1`** | **Whisper / Direct PM** | Target Player | Pink / Magenta (`#FF69B4`) |
| **`2`** | **Party / Team Chat** | Active Room / Party | Cyan / Light Blue (`#00FFFF`) |
| **`3`** | **Club / Guild Chat** | Cross-Server Club | Green (`#00FF00`) |
| **`4`** | **School / Campus Shout**| Whole Map | Orange (`#FFA500`) |
| **`5`** | **System Announcement** | Global Notice | Yellow / Red Banner (`#FFD700`) |

---

## 2. Opcode Specifications

### 1. Opcode 0x7963 (31075): `MsgGameChatReq` / `MsgGameChatNtf` [チャット通知]
* **Direction**: Client $\longrightarrow$ Server (`Req`) / Server $\longrightarrow$ Client (`Ntf`)
* **Delphi Class**: `TMsgGameChatNtf` (`_Unit47.pas:005ADC78`)
* **AnaYg Script**: [`31075.dms`](../dms/31075.dms)

#### Payload Structure:
```text
Offset +0: Int32 speakerCharaId   (0 for System / Server)
Offset +4: Byte  chatColorId      (0 to 5)
Offset +5: UInt16 messageLen      (Length of text in characters)
Offset +7: WStr[messageLen]       (UTF-16LE text payload)
```

---

### 2. Opcode 0x7964 (31076): `MsgGameAnnounceNtf` [アナウンス通知]
* **Direction**: Server $\longrightarrow$ Client (Global Broadcast)
* **Delphi Class**: `TMsgGameAnnounceNtf` (`_Unit47.pas:005AE678`)
* **Function**: Renders a high-priority marquee scrolling broadcast message across the top of all players' viewports.
