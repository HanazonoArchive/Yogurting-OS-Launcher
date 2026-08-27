# Volume 8, Chapter 3: Clubs, Circles & Guild Systems

## 1. Club Management Architecture

Clubs (Circles / Guilds) allow players to band together under a shared club name, emblem, and private cross-server chat channel:

```text
Club Lifecycle:
1. Creation at Club Administrator NPC (Requires 100,000 Sod & Level 10)
2. Member Invitation & Roster Synchronization
3. Club Level Progression & Skill Buffs
4. Custom Nametag Colors & Club Placards
```

---

## 2. Opcode Specifications

### 1. Opcode 0x7604 (30212): `MsgClubMakeReq` / `Ans` (0x7605)
* **Direction**: Client $\longrightarrow$ Server (`0x7604`) / Server $\longrightarrow$ Client (`0x7605`)
* **AnaYg Scripts**: [`30212.dms`](../dms/30212.dms), [`30213.dms`](../dms/30213.dms)
* **Payload Structure (`MsgClubMakeReq`)**:
  * `0x00`: `WStr[13] clubName` (UTF-16LE Club Name, 26 bytes)
  * `0x1A`: `Int32 emblemId` (Visual Emblem Graphic ID)

---

### 2. Opcode 0x7606 (30214): `MsgClubMemberListNtf`
* **Direction**: Server $\longrightarrow$ Client
* **Delphi Class**: `TMsgClubMemberListNtf` (`_Unit47.pas`)
* **Payload Structure**:
  * Delivers the array of all online and offline club members, their grade, promotion status, and contribution points.
