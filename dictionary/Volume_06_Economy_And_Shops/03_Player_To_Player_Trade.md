# Volume 6, Chapter 3: Player-to-Player Direct Trade Protocol

## 1. P2P Trading State Machine

The direct trading protocol uses a 4-step confirmation model to prevent trade scams and item duplication:

```mermaid
sequenceDiagram
    autonumber
    actor PlayerA as Initiator (Player A)
    actor PlayerB as Receiver (Player B)
    participant Field as FieldServer (:10002)

    PlayerA->>Field: 0x792B [MsgGameTradeProposeReq] (Target Player B)
    Field-->>PlayerB: 0x792C [MsgGameTradeProposeNtf] (Proposal from Player A)
    PlayerB->>Field: 0x792D [MsgGameTradeAcceptReq] (Accept Trade)
    Field-->>PlayerA: 0x792E [MsgGameTradeBeginNtf] (Open Trade Window)
    Field-->>PlayerB: 0x792E [MsgGameTradeBeginNtf] (Open Trade Window)

    Note over PlayerA,PlayerB: Phase 2: Adding Items & Sod to Baskets
    PlayerA->>Field: 0x7930 [MsgGameTradeAddItemReq] (Item UIDs & Sod)
    Field-->>PlayerB: 0x7931 [MsgGameTradeAddItemNtf] (Render Player A's items)

    Note over PlayerA,PlayerB: Phase 3: Locking Baskets (No more modifications)
    PlayerA->>Field: 0x7932 [MsgGameTradeLockReq]
    Field-->>PlayerB: 0x7933 [MsgGameTradeLockNtf] (Player A Locked)
    PlayerB->>Field: 0x7932 [MsgGameTradeLockReq]
    Field-->>PlayerA: 0x7933 [MsgGameTradeLockNtf] (Player B Locked)

    Note over PlayerA,PlayerB: Phase 4: Final Mutual Confirmation
    PlayerA->>Field: 0x7934 [MsgGameTradeConfirmReq]
    PlayerB->>Field: 0x7934 [MsgGameTradeConfirmReq]
    Note over Field: Atomic swap of items and Sod balances in database
    Field-->>PlayerA: 0x7935 [MsgGameTradeDoneAns] (Trade Complete!)
    Field-->>PlayerB: 0x7935 [MsgGameTradeDoneAns] (Trade Complete!)
```

---

## 2. Trade Opcode Reference Table

| Opcode (Hex) | Opcode (Dec) | Canonical Name | Direction | Function |
| :--- | :--- | :--- | :--- | :--- |
| `0x792B` | `31019` | `MsgGameTradeProposeReq` | Client $\longrightarrow$ Server | Send trade request to nearby player. |
| `0x792C` | `31020` | `MsgGameTradeProposeAns` | Server $\longrightarrow$ Client | Delivery notification of trade proposal. |
| `0x792D` | `31021` | `MsgGameTradeAcceptReq` | Client $\longrightarrow$ Server | Accept incoming trade proposal. |
| `0x792E` | `31022` | `MsgGameTradeBeginNtf` | Server $\longrightarrow$ Client | Initializes 2-column trade basket UI. |
| `0x7930` | `31024` | `MsgGameTradeAddItemReq` | Client $\longrightarrow$ Server | Place item or Sod into trading basket. |
| `0x7931` | `31025` | `MsgGameTradeAddItemNtf` | Server $\longrightarrow$ Client | Synchronizes opponent's basket. |
| `0x7932` | `31026` | `MsgGameTradeLockReq` | Client $\longrightarrow$ Server | Lock basket to prevent changes. |
| `0x7933` | `31027` | `MsgGameTradeLockNtf` | Server $\longrightarrow$ Client | Update basket lock indicator. |
| `0x7934` | `31028` | `MsgGameTradeConfirmReq` | Client $\longrightarrow$ Server | Final click on "Trade" button. |
| `0x7935` | `31029` | `MsgGameTradeDoneAns` | Server $\longrightarrow$ Client | Complete atomic transfer & close UI. |
| `0x7936` | `31030` | `MsgGameTradeCancelReq` | Client $\longrightarrow$ Server | Cancel / Abort active trade. |
| `0x7937` | `31031` | `MsgGameTradeCancelAns` | Server $\longrightarrow$ Client | Return locked items to backpacks. |
