namespace Yogurting.Core.Network
{
    /// <summary>
    /// Exact Packet Opcode definitions extracted from Delphi legacy server quartet.exe DMT tables.
    /// Covers System, Login (Port 10000), School/Field (Port 10002), Attraction/Episode (Port 10003), 
    /// Comm (Port 10004), and Admin (Port 10010).
    /// </summary>
    public enum PacketOpcode : ushort
    {
        // ----------------------------------------------------
        // SYSTEM & TIME NOTIFICATIONS (20000 series)
        // ----------------------------------------------------
        MsgCheckVersionNtf          = 0x4E21, // 20001: Client version handshake
        MsgWorldTimeNtf             = 0x4E25, // 20005: World season & time sync
        MsgTimeNtf                  = 0x4E26, // 20006: Server timestamp sync

        // ----------------------------------------------------
        // GAME & FIELD NOTIFICATIONS (21000 & 31000 series)
        // ----------------------------------------------------
        MsgGameCharInfoNtf          = 0x7952, // 31058: Full Character info & display data (Delphi 0x005ACDBC)
        MsgGameFieldInfoDoneNtf     = 0x7956, // 31062: Field loading complete (Delphi 0x005AD210)
        MsgGameFieldLoadingStartNtf = 0x795A, // 31066: Field transition loading start (Delphi 0x005AD2FC)
        MsgGameUpdateItemNtf        = 0x5209, // 21001: Inventory item list sync
        MsgGameStartRegainNtf       = 0x520B, // 21003: Start HP/SP regeneration
        MsgGameFadeOutNtf           = 0x520C, // 21004: Screen fade out for transition
        MsgGameStatDeltaNtf         = 0x520D, // 21005: Stat delta / update flag
        MsgGameGeneralPotionNtf     = 0x520E, // 21006: Potion effect
        MsgGameSetStateNtf          = 0x520F, // 21007: Full stats & state table (54 bytes)
        MsgGotoSvrNtf               = 0x5210, // 21008: Server redirect notice
        MsgGameFieldEnterStatReadyNtf = 0x520B, // 21003: Activates LocalPlayer stats & live inventory sync listener (Float 0.3f)
        MsgPingTimeReq              = 0x5211, // 21009: Client sync ping / School Entry Handshake (Delphi 0x006BF574)
        MsgEnterScsNtf              = 0x5212, // 21010: Enter School Server notice (Delphi 0x005A9514)
        MsgEnterScsReq              = 0x5213, // 21011: Enter School Server handshake request
        MsgLeaveScsNtf              = 0x5214, // 21012: Leave School Server notice
        MsgLeaveAtsNtf              = 0x5215, // 21013: Leave Attraction Server notice
        MsgObjectCreateNtf          = 0x521B, // 21019: Spawn entity/NPC/Player in field
        MsgObjectDestroyNtf         = 0x521C, // 21020: Despawn entity from field
        MsgObjectClickReq           = 0x521F, // 21023: Interact with fixed field object / Warp Gate
        MsgObjectUseAns             = 0x5220, // 21024: Object interaction response
        MsgGameShopEnterReq         = 0x5221, // 21025: Shop enter request
        MsgGameShopLeaveReq         = 0x5222, // 21026: Shop leave request
        MsgGameShopListReq          = 0x5223, // 21027: Shop product list request
        MsgGameShopListAns          = 0x5224, // 21028: Shop product list status answer
        MsgGameShopListNtf          = 0x5225, // 21029: Shop product catalog list
        MsgLobbyStateNtf            = 0x5227, // 21031: Lobby room state notice
        MsgNpcDialogActionReq       = 0x5228, // 21032: NPC Dialog click/action
        MsgGameNpcDialogNtf         = 0x5229, // 21033: NPC Dialogue text & options response
        MsgNpcDialogSelectReq       = 0x522C, // 21036: NPC Choice selected
        MsgGameShopBuyReq           = 0x5227, // 21031: Shop buy item request
        MsgObjectStateNtf           = 0x5227, // 21031: Object / Warp Gate state update
        MsgGameShopBuyAns           = 0x5224, // 21028: Shop buy item answer
        MsgGameByulShopBeginReq     = 0x5233, // 21043: Open Star Shop Request
        MsgGameByulShopBeginAns     = 0x5234, // 21044: Star Shop open response
        MsgGameByulShopEndReq       = 0x5235, // 21045: Close Star Shop Request
        MsgGameByulShopEndAns       = 0x5236, // 21046: Star Shop close response
        MsgGameByulChargeReq        = 0x523A, // 21050: Request Star coin balance
        MsgGameByulChargeAns        = 0x523B, // 21051: Star currency charged / coin balance response
        MsgGameByulProductListReq   = 0x523C, // 21052: Star item product catalog request
        MsgGameByulProductListAns   = 0x523D, // 21053: Star item product catalog
        MsgGameByulProductBuyReq    = 0x523E, // 21054: Star item purchase request (Delphi 0x006C00A8)
        MsgGameByulProductBuyAns    = 0x523F, // 21055: Star item purchase response (Delphi 0x005A9C34)
        MsgGameUseByulBeItemReq     = 0x524B, // 21067: Use Star Item / Buff item request (Delphi 0x006C0614)
        MsgGameUseByulBeItemAns     = 0x524C, // 21068: Use Star Item / Buff item response (Delphi 0x005A9E71)
        MsgGameSchoolInfoNtf        = 0x5264, // 21092: School campus info
        MsgGameEquipByulBeItemReq   = 0x5265, // 21093: Equip Star/Cash item request (Delphi 0x006C08EB)
        MsgGameEquipByulBeItemAns   = 0x5266, // 21094: Equip Star/Cash item response (Delphi 0x005AA1B0)
        MsgGameStripByulBeItemReq   = 0x5267, // 21095: Unequip Star/Cash item request (Delphi 0x006C0900)
        MsgGameStripByulBeItemAns   = 0x5268, // 21096: Unequip Star/Cash item response (Delphi 0x005AA24C)
        MsgGameUseByulBeItemStartNtf = 0x5269, // 21097: Star item active duration notice (Delphi 0x005AA2C9)
        MsgGameUseByulBeItemEndNtf   = 0x526A, // 21098: Star item expiration notice (Delphi 0x005AA314)
        MsgGameEnterHairShopNtf     = 0x526D, // 21101: Hair salon menu open
        MsgGameLeaveHairShopNtf     = 0x5272, // 21106: Hair salon close
        MsgGameNpcCreateNtf         = 0x7942, // 31042: Static NPC / prop create
        MsgGameVisualAttachNtf      = 0x7942, // 31042: Visual mesh attachment / QuickSlot hotkey sync
        MsgGameEquipReq             = 0x7944, // 31044: Equip item request (Delphi 0x006C08C3)
        MsgGameEquipAns             = 0x7945, // 31045: Equip item response (Delphi 0x005ACAA4)
        MsgGameUnequipReq           = 0x7946, // 31046: Unequip item request (Delphi 0x006C08D8)
        MsgGameUnequipAns           = 0x7947, // 31047: Unequip item response (Delphi 0x005ACB28)
        MsgGameRevivalCharAns       = 0x794C, // 31052: Character Revival Execution Notice (Delphi 0x0060EB1F)
        MsgGameFieldEntitySpawnNtf  = 0x795C, // 31068: Field Entity / NPC Spawn Info
        MsgGameWarpTriggerReq       = 0x7965, // 31077: Step into warp zone request
        MsgGameWarpStartNtf         = 0x7966, // 31078: Warp transition start
        MsgGameWarpGateReq          = 0x7967, // 31079: Warp gate request
        MsgGameWarpResultNtf        = 0x7968, // 31080: Warp Gate success notice
        MsgGameMonActionNtf         = 0x796A, // 31082: Monster Action / Death / Attack Animation (Delphi 0x005AE01C)
        MsgGameMonStatusNtf         = 0x796D, // 31085: Monster Status / Live Overhead HP Update (Delphi 0x005AE185)
        MsgGameMonHpInfoNtf         = 0x79E8, // 31208: Monster HP & Target State Update (Delphi 0x005AFAE4)
        MsgGameMonsterOwnershipAcquiredNtf = 0x7A00, // 31232: Monster Target Ownership Acquired / Green Ring (Delphi TMsgGameMonsterOwnershipAcquiredNtf)
        MsgGameMonsterOwnershipLostNtf = 0x7A01, // 31233: Monster Target Ownership Dropped (Delphi TMsgGameMonsterOwnershipLostNtf)
        MsgGameSitDownReq           = 0x798C, // 31116: Bench / ground sit down request
        MsgGameSitDownAns           = 0x798D, // 31117: Sit down response / broadcast
        MsgGameStandUpNtf           = 0x798E, // 31118: Stand up notification
        MsgGameChargePointUpdateNtf = 0x791C, // 31004: Charge point update notice
        MsgGameCharaNameInfoNtf     = 0x7963, // 31075: Character Overhead Name, Title & Guild Tag
        MsgGameZoneNameNtf          = 0x79D4, // 31188: Zone / Room Title Notice
        MsgGameMoveExReq            = 0x79D5, // 31189: Extended movement sync request (Delphi 0x005AF7F0)
        MsgGameMoveExNtf            = 0x79D5, // 31189: Extended movement sync broadcast
        MsgGameAtkMovChangeNtf      = 0x799F, // 31135: Movement and attack speed change (Delphi 0x005AEC54)
        MsgGameWeaponFrameReq       = 0x5273, // 21107: Weapon socket frame request
        MsgGameWeaponFrameAns       = 0x5274, // 21108: Weapon socket frame answer
        MsgGameHuntCharLvUpNtf      = 0x5275, // 21109: Character Level Up
        MsgGameHuntMonDeadNtf       = 0x5276, // 21110: Monster killed
        MsgGameEquipTitleAns        = 0x79A3, // 31139: Equip Title response (Delphi 0x005AED10)
        MsgGameHuntCharExpUpNtf     = 0x5277, // 21111: Character EXP gain (Delphi 0x005AA7BC)
        MsgGamePromoteInfoNtf       = 0x79B2, // 31154: Grade promotion info (Delphi 0x005AEF1C)
        MsgGameRevivalSchoolAns     = 0x7950, // 31056: School Respawn / Revival Answer
        MsgGameActionAttackReq      = 0xA031, // 41009: Action / Basic Attack request (Delphi 0x00611FD0)
        MsgGameSkillCastReq         = 0x7923, // 31011: Skill Cast Request
        MsgGameSkillCastAns         = 0x7924, // 31012: Skill Cast Answer / Multi-target Damage
        MsgGameSkillPrepNtf         = 0x7925, // 31013: Skill Cast Preparation Notice
        MsgGameSkillEndNtf          = 0x7926, // 31014: Skill Cast End / Cancel Notice
        MsgGameSkillHotkeyNtf       = 0x79FC, // 31228: Skill Hotkey Shortcut Sync
        MsgGameExNpcDialogReq       = 0x793B, // 31035: Interactive NPC Dialog Request
        MsgGameExNpcDialogSelectReq = 0x793C, // 31036: Interactive NPC Dialog Select Request
        MsgGameExNpcDialogNtf       = 0x5229, // 21033: Extended NPC Dialog Notice
        MsgGameExNpcDialogSelectNtf = 0x522C, // 21036: Extended NPC Dialog Selection Notice
        MsgGameNpcDialogEventNtf    = 0x793E, // 31038: NPC Dialog Event Notice (Delphi 0x005AC6C4)
        MsgGameNpcDialogSaleListNtf = 0x793F, // 31039: NPC Shop Catalog Notice (Delphi 0x005AC718)
        MsgGameNpcDialogEndNtf      = 0x7940, // 31040: NPC Action End / Dialog Close Notice
        MsgGameHairShopEnterNtf     = 0x526D, // 21101: Hair Salon Catalog Notice
        MsgGameHairShopChangeAns    = 0x526E, // 21102: Hair Salon Style Change Answer
        MsgGameHairChangeAns        = 0x5271, // 21105: Hair Change Answer
        MsgGameVMachineUseDanceItemNtf = 0x794D, // 31053: Dance / Event Consumables Broadcast
        MsgLockerEnterNtf           = 0xA029, // 41001: Locker Enter Notice
        MsgLockerOpenReq            = 0xA02B, // 41003: Locker Open Request
        MsgLockerOpenAns            = 0xA02C, // 41004: Locker Open Answer
        MsgLockerItemInfoNtf        = 0xA02D, // 41005: Locker Stored Items Info Notice
        MsgLockerMoveItemReq        = 0xA02F, // 41007: Locker Move Item Request
        MsgLockerMoveItemCompleteNtf = 0xA030, // 41008: Locker Move Item Complete Notice

        // ----------------------------------------------------
        // LOGIN SERVER (PORT 10000 - 30100 series)
        // ----------------------------------------------------
        MsgAuthTypeNtf              = 0x7595, // 30101: Auth type (MD5/Password)
        MsgLoginAuthReq             = 0x7596, // 30102: Login Credentials Request
        MsgLoginAuthAns             = 0x7597, // 30103: Login Authentication Result (458 bytes)
        MsgLoginJoinGameReq         = 0x759A, // 30106: Join Game / Select Server
        MsgLoginJoinGameAns         = 0x759B, // 30107: School & Comm IP/Port assignment
        MsgLoginKickOutAns          = 0x759D, // 30109: Kickout response
        MsgLoginKickOutNtf          = 0x759E, // 30110: Kickout notification
        MsgLoginWorldListReq        = 0x759F, // 30111: World List Request
        MsgLoginWorldListAns        = 0x75A0, // 30112: World List Status
        MsgLoginWorldListNtf        = 0x75A1, // 30113: World entries (Estiva, etc.)
        MsgLoginSelectWorldReq      = 0x75A2, // 30114: Select World
        MsgLoginSchoolListNtf       = 0x75A3, // 30115: School list entries (Estiva Academy, So-il)
        MsgLoginCheckNameReq        = 0x75A4, // 30116: Check Duplicate Name Request (Delphi 0x006BEFC0)
        MsgLoginCheckNameAns        = 0x75A5, // 30117: Check Duplicate Name Response (Delphi 0x005AAD20)
        MsgLoginCheckPhoneReq       = 0x75A6, // 30118: Check Phone Number Request (Delphi 0x006BF064)
        MsgLoginCheckPhoneAns       = 0x75A7, // 30119: Check Phone Number Response (Delphi 0x005AAD88)
        MsgLoginMakeCharReq         = 0x75A8, // 30120: Create Character Request (Delphi 0x006BF098)
        MsgLoginMakeCharAns         = 0x75A9, // 30121: Create Character Response (Delphi 0x005AADDC)
        MsgLoginDeleteCharReq       = 0x75AA, // 30122: Delete/Reset Character Request (Delphi 0x006BF1A0)
        MsgLoginDeleteCharAns       = 0x75AB, // 30123: Delete Character Response (Delphi 0x005AAE30)
        MsgLoginResumeNtf           = 0x75AE, // 30126: Session Resume token

        // ----------------------------------------------------
        // LOBBY & MATCHMAKING ROOMS (30300 series)
        // ----------------------------------------------------
        MsgLobbyEnterReq            = 0x765E, // 30302: Enter Lobby
        MsgLobbyLeaveReq            = 0x7660, // 30304: Leave Lobby
        MsgLobbyRoomListReq         = 0x7668, // 30312: Room list request
        MsgLobbyCreateRoomReq       = 0x766A, // 30314: Create Episode Room
        MsgLobbyPageInfoNtf         = 0x766C, // 30316: Lobby Room page status
        MsgLobbyReserveJoinRoomReq  = 0x766D, // 30317: Reserve join room
        MsgLobbyJoinRoomReq         = 0x766F, // 30319: Join room request
        MsgLobbyQuickJoinReq        = 0x7672, // 30322: Quick Join room
        MsgWaitRoomInfoReq          = 0x7678, // 30328: Wait room details
        MsgWaitRoomEditReq          = 0x767A, // 30330: Edit wait room settings
        MsgWaitRoomSelectTeamReq    = 0x7680, // 30336: Change team slot
        MsgWaitRoomInviteReq        = 0x7682, // 30338: Invite player to room
        MsgWaitRoomReadyStartReq    = 0x768C, // 30348: Player Ready / Host Start Episode
        MsgWaitRoomLeaveReq         = 0x768D, // 30349: Leave wait room

        // ----------------------------------------------------
        // COMM SERVER (PORT 10004 - 30500 series)
        // ----------------------------------------------------
        MsgTransJoinCmsAns          = 0x7604, // 30212: Comm Server Join Acknowledgment (friend list)
        MsgCommFriendProposeReq     = 0x7728, // 30504: Send Friend Request
        MsgCommEchoNtf              = 0x7759, // 30553: Heartbeat ping-pong

        // ----------------------------------------------------
        // IN-GAME / WORLD ACTIONS (31000 series)
        // ----------------------------------------------------
        MsgGameMoveNtf              = 0x7918, // 31000: Character Movement Sync Broadcast
        MsgGameAttackReq            = 0x7919, // 31001: Character Attack Request (Delphi TMsgGameAttackReq)
        MsgGameAttackAns            = 0x791A, // 31002: Character Attack Response / Damage Broadcast (Delphi TMsgGameAttackAns)
        MsgGameDieCharNtf           = 0x791B, // 31003: Player Death Notification (Delphi TMsgGameDieCharNtf)
        MsgGameDisplayCounterNtf    = 0x7959, // 31065: Display Counter Update (Delphi TMsgGameDisplayCounterNtf)
        MsgGameBeginCounterNtf      = 0x7990, // 31120: Begin Counter / Mission Objective (Delphi TMsgGameBeginCounterNtf)
        MsgGameShowCounterNtf       = 0x7993, // 31123: Show Counter UI (Delphi TMsgGameShowCounterNtf)
        MsgGameMoveStopReq          = 0x791E, // 31006: Character Stop Move
        MsgGamePosSyncReq           = 0x7921, // 31009: Position Sync packet
        MsgGameJumpReq              = 0x7922, // 31010: Jump action
        MsgGameUseCoItemReq         = 0x7928, // 31016: Consumable Item Use Request (31016.dms: 消費アイテム使用要求)
        MsgGameUseCoItemAns         = 0x7929, // 31017: Consumable Item Use Answer (31017.dms: COITEM使用返答)
        MsgGameSkillHitReq          = 0x7928, // 31016: Legacy alias
        MsgGameMonMoveNtf           = 0x7969, // 31081: Monster Move Notification (Delphi TMsgGameMonMoveNtf)
        MsgGameMonAttackNtf         = 0x796A, // 31082: Monster Attack Notification (Delphi TMsgGameMonAttackNtf)
        MsgGameMonInfoNtf           = 0x796E, // 31086: Monster Spawn / Info Notification (Delphi TMsgGameMonInfoNtf)
        MsgGameCharLvUpNtf          = 0x7970, // 31088: Player Level-Up Notification (Delphi TMsgGameCharLvUpNtf)
        MsgGameCharDexLvUpNtf       = 0x79EE, // 31214: Weapon Mastery / Dex Level-Up Notification (Delphi TMsgGameCharDexLvUpNtf)
        MsgGameTradeProposeReq      = 0x792B, // 31019: Trade Propose
        MsgGameTradeAcceptReq       = 0x792D, // 31021: Trade Accept
        MsgGameTradeAddItemReq      = 0x7930, // 31024: Trade Add Item
        MsgGameTradeCancelReq       = 0x7931, // 31025: Trade Cancel
        MsgGameTradeLockReq         = 0x7932, // 31026: Trade Lock
        MsgGameTradeConfirmReq      = 0x7933, // 31027: Trade Final Confirm
        MsgGameInventoryMoveReq     = 0x7935, // 31029: Move / Equip item slot
        MsgGameItemDropReq          = 0x7937, // 31031: Drop item to floor
        MsgGameItemPickUpReq        = 0x7939, // 31033: Pick up item from floor
        MsgGameItemUseReq           = 0x793B, // 31035: Use inventory consumable
        MsgGameQuickSlotSetReq      = 0x7940, // 31040: Set quick slot mapping
        MsgGameStarEquipReq         = 0x7944, // 31044: Equip Star item
        MsgGameStarUnequipReq       = 0x7946, // 31046: Unequip Star item
        MsgGameReinforceItemReq     = 0x794A, // 31050: Reinforce item
        MsgGameRevival119Req        = 0x794D, // 31053: Emergency 119 Respawn
        MsgGameRevival119Ans        = 0x794E, // 31054: Emergency 119 Respawn Response
        MsgGameRevivalSchoolReq     = 0x794F, // 31055: School Respawn
        MsgGameEmoteReq             = 0x795B, // 31067: Emote social animation
        MsgGameChatReq              = 0x7963, // 31075: Send Chat Message
        MsgGameChatNtf              = 0x7963, // 31075: Broadcast Chat Message
        MsgGameChannelSwitchReq     = 0x7965, // 31077: Switch school channel
        MsgGameFieldDropBoxNtf      = 0x796C, // 31084: Physical Cardboard Loot Drop Box on Ground (Delphi TMsgGameMonDeadNtf)
        MsgGameEpisodeResultNtf     = 0x7972, // 31090: Episode / Stage Result Evaluation & 3-Booty Box Roulette (Delphi TMsgGameEpisodeResultNtf)
        MsgGameBootyBoxDoneReq      = 0x7974, // 31092: Booty Box opened / Selected by Player (Delphi TMsgGameBootyBoxDoneReq)
        MsgGameBootyBoxDoneAns      = 0x7975, // 31093: Booty Box Unbox Answer & Particle Trigger (Delphi TMsgGameBootyBoxDoneAns)
        MsgGameEpisodePlayResumeNtf = 0x7957, // 31063: Episode Play Resume / Unpause / Unlock Controls (Delphi _Unit47.pas:53292)
        MsgGameEpisodeInfoNtf       = 0x79B5, // 31157: Episode Info / Hunt Stage Context & Booty Box Init (Delphi _Unit47.pas:005AF11D)
        MsgGameBootyBoxAssignNtf    = 0x79E3, // 31203: Booty Box HUD Assignment in Top-Right below minimap (Delphi 31203.dms)
        MsgGameTakeUpObjectReq      = 0x7984, // 31108: Take up interactive object
        MsgGamePushObjectReq        = 0x7996, // 31126: Push interactive object
        MsgGameLockerOpenReq        = 0x79A2, // 31138: Open Storage Locker
        MsgGameLockerCloseReq       = 0x79A4, // 31140: Close Storage Locker
        MsgGameEnchantCrystalReq    = 0x79A8, // 31144: Crystal Enchant level
        MsgGameCrystallizeReq       = 0x79AA, // 31146: Crystallize item
        MsgGameRaceEpisodeResultReq = 0x79BE, // 31166: Race Episode completion
        MsgGameSpecialPhoneCallReq  = 0x79C2, // 31170: Special Phone call action
        MsgGameGuildChangeNameReq   = 0x79CC, // 31180: Change Guild Name
        MsgGameBroadcastAOINtf      = 0x79D3, // 31187: Broadcast Area-of-Interest data
        MsgGameCharDirectNtf        = 0x798F, // 31119: Character facing direction sync

        // ----------------------------------------------------
        // CAPSULE VENDING MACHINE (GACHA) (42000 series)
        // ----------------------------------------------------
        MsgGameCapsuleBuyReq        = 0xA413, // 42003: Capsule Vending Machine Purchase Request
        MsgGameCapsuleBuyAns        = 0xA414, // 42004: Capsule Vending Machine Purchase Response
        MsgGameCapsuleExitNtf       = 0xA415, // 42005: Capsule Vending Machine Exit Notification

        // ----------------------------------------------------
        // ADMIN SERVER (PORT 10010 - 10000 series)
        // ----------------------------------------------------
        MsgAdminLoginReq            = 0x2711, // 10001: Admin login
        MsgAdminStatsReq            = 0x271B, // 10011: Server stats
        MsgAdminQueryUserReq        = 0x2725, // 10021: Query user
        MsgAdminKickUserReq         = 0x272F, // 10031: Kick user
        MsgAdminBanUserReq          = 0x2739, // 10041: Ban user
        MsgAdminModifyDataReq       = 0x2743, // 10051: Modify data
        MsgAdminReloadReq           = 0x2EE1, // 12001: Reload DB
        MsgAdminAnnounceReq         = 0x2EEB, // 12011: Global Announcement
        MsgAdminShutdownReq         = 0x4A39  // 19001: Server Shutdown
    }
}
