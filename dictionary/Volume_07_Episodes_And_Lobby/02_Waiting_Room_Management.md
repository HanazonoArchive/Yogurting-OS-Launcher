# Volume 7, Chapter 2: Waiting Room Management & Team Coordination

## 1. Waiting Room State Machine

```mermaid
sequenceDiagram
    autonumber
    actor Host as Room Host
    actor Guest as Joining Player
    participant Episode as EpisodeServer (:10003)

    Host->>Episode: 0x7670 [MsgRoomMakeReq] (Title, EpId, MaxUsers, Password)
    Episode-->>Host: 0x7671 [MsgRoomMakeAns] (Room Created, RoomId: 101)

    Guest->>Episode: 0x7672 [MsgRoomJoinReq] (RoomId: 101)
    Episode-->>Guest: 0x7673 [MsgRoomJoinAns] (Join OK)
    Episode-->>Host: 0x7674 [MsgRoomPcListNtf] (Updated Player Roster)
    Episode-->>Guest: 0x7674 [MsgRoomPcListNtf] (Updated Player Roster)

    Guest->>Episode: 0x7678 [MsgRoomSelectTeamReq] (Team 1 or Team 2)
    Episode-->>Host: 0x7679 [MsgRoomSelectTeamNtf] (Guest switched team)

    Guest->>Episode: 0x768A [MsgRoomReadyReq] (Toggle Ready)
    Episode-->>Host: 0x768B [MsgRoomReadyNtf] (Guest is READY!)

    Host->>Episode: 0x768C [MsgRoomStartReq] (Launch Episode)
    Episode-->>Host: 0x768D [MsgRoomStartAns] (Begin Dungeon Loading)
    Episode-->>Guest: 0x768D [MsgRoomStartAns] (Begin Dungeon Loading)
```

---

## 2. Roster Serialization (`pc_info` Sub-Structure - 44 Bytes)

Defined in [`struct.dms`](../dms/struct.dms) and transmitted via `0x7674 (MsgRoomPcListNtf)`:

| Offset | Field Name | Type | Size | Description |
| :--- | :--- | :--- | :--- | :--- |
| `0x00` | `idChar` | `Int32` | 4B | Player Character ID |
| `0x04` | `name` | `WStr[13]` | 26B | UTF-16LE Character Name |
| `0x1E` | `gender` | `Byte` | 1B | `1` = Male, `2` = Female |
| `0x1F` | `grade` | `Byte` | 1B | School Year / Grade |
| `0x20` | `weapon` | `UInt16` | 2B | Currently Equipped Weapon Category |
| `0x22` | `idTeam` | `UInt16` | 2B | Team Assignment (`1` = Red / Team A, `2` = Blue / Team B) |
| `0x24` | `phone` | `Int32` | 4B | In-game Phone Number |
| `0x28` | `idPromotion` | `Int32` | 4B | Promotion / Title Rank ID |
