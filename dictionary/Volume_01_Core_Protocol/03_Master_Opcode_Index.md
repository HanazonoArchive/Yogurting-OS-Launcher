# Volume 1, Chapter 3: Master Opcode Index Directory

This master catalog documents all **268 opcodes** of the Yogurting Online network protocol, cross-referencing raw AnaYg DMS scripts, Modern C# enum identifiers, legacy Delphi classes, target server ports, and packet directions.

---

## Master Index Table (268 Opcodes)

| Opcode (Hex) | Opcode (Dec) | Japanese Title (DMS) | Canonical Name (C#) | Direction | Target Port | Raw DMS Script | Legacy Delphi Class |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `0x4E21` | `20001` | **バージョンチェック通知** | `MsgCheckVersionNtf` | `Client -> Server` | `Universal` | [`20001.dms`](../dms/20001.dms) | `TMsgCheckVersionNtf` |
| `0x4E22` | `20002` | **エラーメッセージ通知** | `MsgUnknown_0x4E22` | `Server -> Client` | `Universal` | [`20002.dms`](../dms/20002.dms) | `TMsgUnknown_0x4E22` |
| `0x4E24` | `20004` | **アラートメッセージ通知** | `MsgUnknown_0x4E24` | `Server -> Client` | `Universal` | [`20004.dms`](../dms/20004.dms) | `TMsgUnknown_0x4E24` |
| `0x4E25` | `20005` | **ワールド時間通知** | `MsgWorldTimeNtf` | `Server -> Client` | `Universal` | [`20005.dms`](../dms/20005.dms) | `TMsgWorldTimeNtf` |
| `0x4E26` | `20006` | **時間通知** | `MsgTimeNtf` | `Server -> Client` | `Universal` | [`20006.dms`](../dms/20006.dms) | `TMsgTimeNtf` |
| `0x520B` | `21003` | **自動回復開始通知** | `MsgGameFieldEnterStatReadyNtf` | `Server -> Client` | `10002 (Field)` | [`21003.dms`](../dms/21003.dms) | `TMsgGameFieldEnterStatReadyNtf` |
| `0x520C` | `21004` | **回復停止通知** | `MsgGameFadeOutNtf` | `Server -> Client` | `10002 (Field)` | [`21004.dms`](../dms/21004.dms) | `TMsgGameFadeOutNtf` |
| `0x520D` | `21005` | **HP設定通知** | `MsgGameStatDeltaNtf` | `Server -> Client` | `10002 (Field)` | [`21005.dms`](../dms/21005.dms) | `TMsgGameStatDeltaNtf` |
| `0x520F` | `21007` | **ステータス通知** | `MsgGameSetStateNtf` | `Server -> Client` | `10002 (Field)` | [`21007.dms`](../dms/21007.dms) | `TMsgGameSetStateNtf` |
| `0x5210` | `21008` | **サーバー移動通知** | `MsgGotoSvrNtf` | `Server -> Client` | `10002 (Field)` | [`21008.dms`](../dms/21008.dms) | `TMsgGotoSvrNtf` |
| `0x5211` | `21009` | **サーバー参加通知** | `MsgPingTimeReq` | `Client -> Server` | `10002 (Field)` | [`21009.dms`](../dms/21009.dms) | `TMsgPingTimeReq` |
| `0x5212` | `21010` | **スクールサーバー参加通知** | `MsgEnterScsNtf` | `Server -> Client` | `10002 (Field)` | [`21010.dms`](../dms/21010.dms) | `TMsgEnterScsNtf` |
| `0x5213` | `21011` | **スクールサーバーログアウト通知** | `MsgEnterScsReq` | `Client -> Server` | `10002 (Field)` | [`21011.dms`](../dms/21011.dms) | `TMsgEnterScsReq` |
| `0x5214` | `21012` | **アトラクションサーバーログイン通知** | `MsgLeaveScsNtf` | `Server -> Client` | `10002 (Field)` | [`21012.dms`](../dms/21012.dms) | `TMsgLeaveScsNtf` |
| `0x5216` | `21014` | **エスケープ要求通知** | `MsgUnknown_0x5216` | `Client -> Server` | `10002 (Field)` | [`21014.dms`](../dms/21014.dms) | `TMsgUnknown_0x5216` |
| `0x5217` | `21015` | **エスケープ承認通知** | `MsgUnknown_0x5217` | `Server -> Client` | `10002 (Field)` | [`21015.dms`](../dms/21015.dms) | `TMsgUnknown_0x5217` |
| `0x5218` | `21016` | **エスケープキャンセル通知** | `MsgUnknown_0x5218` | `Server -> Client` | `10002 (Field)` | [`21016.dms`](../dms/21016.dms) | `TMsgUnknown_0x5218` |
| `0x521B` | `21019` | **オブジェクト生成通知** | `MsgObjectCreateNtf` | `Server -> Client` | `10002 (Field)` | [`21019.dms`](../dms/21019.dms) | `TMsgObjectCreateNtf` |
| `0x521F` | `21023` | **オブジェクト使用要求** | `MsgObjectClickReq` | `Client -> Server` | `10002 (Field)` | [`21023.dms`](../dms/21023.dms) | `TMsgObjectClickReq` |
| `0x5220` | `21024` | **オブジェクト使用返答** | `MsgObjectUseAns` | `Server -> Client` | `10002 (Field)` | [`21024.dms`](../dms/21024.dms) | `TMsgObjectUseAns` |
| `0x5221` | `21025` | **ガイド掲示板ENTER通知** | `MsgGameShopEnterReq` | `Client -> Server` | `10002 (Field)` | [`21025.dms`](../dms/21025.dms) | `TMsgGameShopEnterReq` |
| `0x5222` | `21026` | **ガイド掲示板LEAVE通知** | `MsgGameShopLeaveReq` | `Client -> Server` | `10002 (Field)` | [`21026.dms`](../dms/21026.dms) | `TMsgGameShopLeaveReq` |
| `0x5223` | `21027` | **ガイド情報要求** | `MsgGameShopListReq` | `Client -> Server` | `10002 (Field)` | [`21027.dms`](../dms/21027.dms) | `TMsgGameShopListReq` |
| `0x5224` | `21028` | **ガイド情報返答** | `MsgGameShopBuyAns` | `Server -> Client` | `10002 (Field)` | [`21028.dms`](../dms/21028.dms) | `TMsgGameShopBuyAns` |
| `0x5225` | `21029` | **エピソードガイド情報通知** | `MsgGameShopListNtf` | `Server -> Client` | `10002 (Field)` | [`21029.dms`](../dms/21029.dms) | `TMsgGameShopListNtf` |
| `0x5227` | `21031` | **ロビー状態通知** | `MsgObjectStateNtf` | `Server -> Client` | `10002 (Field)` | [`21031.dms`](../dms/21031.dms) | `TMsgObjectStateNtf` |
| `0x5229` | `21033` | **拡張NPCダイアログ返答通知** | `MsgGameNpcDialogNtf` | `Server -> Client` | `10002 (Field)` | [`21033.dms`](../dms/21033.dms) | `TMsgGameNpcDialogNtf` |
| `0x522A` | `21034` | **拡張NPCダイアログクエストリスト返答通知** | `MsgUnknown_0x522A` | `Server -> Client` | `10002 (Field)` | [`21034.dms`](../dms/21034.dms) | `TMsgUnknown_0x522A` |
| `0x522B` | `21035` | **NPCダイアログクエスト情報返答通知** | `MsgUnknown_0x522B` | `Server -> Client` | `10002 (Field)` | [`21035.dms`](../dms/21035.dms) | `TMsgUnknown_0x522B` |
| `0x522C` | `21036` | **拡張NPCダイアログ選択通知** | `MsgNpcDialogSelectReq` | `Client -> Server` | `10002 (Field)` | [`21036.dms`](../dms/21036.dms) | `TMsgNpcDialogSelectReq` |
| `0x5233` | `21043` | **アイテムショップ開始要求** | `MsgGameByulShopBeginReq` | `Client -> Server` | `10002 (Field)` | [`21043.dms`](../dms/21043.dms) | `TMsgGameByulShopBeginReq` |
| `0x5234` | `21044` | **アイテムショップ開始返答** | `MsgGameByulShopBeginAns` | `Server -> Client` | `10002 (Field)` | [`21044.dms`](../dms/21044.dms) | `TMsgGameByulShopBeginAns` |
| `0x5235` | `21045` | **アイテムショップ終了要求** | `MsgGameByulShopEndReq` | `Client -> Server` | `10002 (Field)` | [`21045.dms`](../dms/21045.dms) | `TMsgGameByulShopEndReq` |
| `0x5236` | `21046` | **アイテムショップ終了返答** | `MsgGameByulShopEndAns` | `Server -> Client` | `10002 (Field)` | [`21046.dms`](../dms/21046.dms) | `TMsgGameByulShopEndAns` |
| `0x523A` | `21050` | **MSG_GAME_BYUL_CHARGE_REQ** | `MsgGameByulChargeReq` | `Client -> Server` | `10002 (Field)` | [`21050.dms`](../dms/21050.dms) | `TMsgGameByulChargeReq` |
| `0x523B` | `21051` | **MSG_GAME_BYUL_CHARGE_ANS** | `MsgGameByulChargeAns` | `Server -> Client` | `10002 (Field)` | [`21051.dms`](../dms/21051.dms) | `TMsgGameByulChargeAns` |
| `0x523C` | `21052` | **MSG_GAME_BYUL_PRODUCT_LIST_REQ** | `MsgGameByulProductListReq` | `Client -> Server` | `10002 (Field)` | [`21052.dms`](../dms/21052.dms) | `TMsgGameByulProductListReq` |
| `0x523D` | `21053` | **MSG_GAME_BYUL_PRODUCT_LIST_ANS** | `MsgGameByulProductListAns` | `Server -> Client` | `10002 (Field)` | [`21053.dms`](../dms/21053.dms) | `TMsgGameByulProductListAns` |
| `0x523E` | `21054` | **スターアイテム購入要求** | `MsgGameByulProductBuyReq` | `Client -> Server` | `10002 (Field)` | [`21054.dms`](../dms/21054.dms) | `TMsgGameByulProductBuyReq` |
| `0x523F` | `21055` | **スターアイテム購入返答** | `MsgGameByulProductBuyAns` | `Server -> Client` | `10002 (Field)` | [`21055.dms`](../dms/21055.dms) | `TMsgGameByulProductBuyAns` |
| `0x524B` | `21067` | **装備系スターアイテム使用要求** | `MsgGameUseByulBeItemReq` | `Client -> Server` | `10002 (Field)` | [`21067.dms`](../dms/21067.dms) | `TMsgGameUseByulBeItemReq` |
| `0x524C` | `21068` | **装備系スターアイテム使用応答** | `MsgGameUseByulBeItemAns` | `Server -> Client` | `10002 (Field)` | [`21068.dms`](../dms/21068.dms) | `TMsgGameUseByulBeItemAns` |
| `0x524F` | `21071` | **未対応** | `MsgUnknown_0x524F` | `Bidirectional` | `10002 (Field)` | [`21071.dms`](../dms/21071.dms) | `TMsgUnknown_0x524F` |
| `0x5250` | `21072` | **プレゼントされたスターアイテム返答** | `MsgUnknown_0x5250` | `Server -> Client` | `10002 (Field)` | [`21072.dms`](../dms/21072.dms) | `TMsgUnknown_0x5250` |
| `0x5257` | `21079` | **MSG_GAME_BYUL_BYUL_HISTORY_REQ** | `MsgUnknown_0x5257` | `Bidirectional` | `10002 (Field)` | [`21079.dms`](../dms/21079.dms) | `TMsgUnknown_0x5257` |
| `0x5258` | `21080` | **MSG_GAME_BYUL_BYUL_HISTORY_ANS(未完成)** | `MsgUnknown_0x5258` | `Bidirectional` | `10002 (Field)` | [`21080.dms`](../dms/21080.dms) | `TMsgUnknown_0x5258` |
| `0x525D` | `21085` | **台紙色変更通知(1:r 2:o 3:y 4:g 5:c 6:b 7:m)** | `MsgUnknown_0x525D` | `Server -> Client` | `10002 (Field)` | [`21085.dms`](../dms/21085.dms) | `TMsgUnknown_0x525D` |
| `0x525F` | `21087` | **プラカード状態変更返答** | `MsgUnknown_0x525F` | `Server -> Client` | `10002 (Field)` | [`21087.dms`](../dms/21087.dms) | `TMsgUnknown_0x525F` |
| `0x5261` | `21089` | **プラカード内容変更返答** | `MsgUnknown_0x5261` | `Server -> Client` | `10002 (Field)` | [`21089.dms`](../dms/21089.dms) | `TMsgUnknown_0x5261` |
| `0x5264` | `21092` | **学校情報通知** | `MsgGameSchoolInfoNtf` | `Server -> Client` | `10002 (Field)` | [`21092.dms`](../dms/21092.dms) | `TMsgGameSchoolInfoNtf` |
| `0x526D` | `21101` | **ヘアショップ参加通知** | `MsgGameEnterHairShopNtf` | `Server -> Client` | `10002 (Field)` | [`21101.dms`](../dms/21101.dms) | `TMsgGameEnterHairShopNtf` |
| `0x5271` | `21105` | **髪型変更返答** | `MsgUnknown_0x5271` | `Server -> Client` | `10002 (Field)` | [`21105.dms`](../dms/21105.dms) | `TMsgUnknown_0x5271` |
| `0x5272` | `21106` | **ヘアショップ退店通知** | `MsgGameLeaveHairShopNtf` | `Server -> Client` | `10002 (Field)` | [`21106.dms`](../dms/21106.dms) | `TMsgGameLeaveHairShopNtf` |
| `0x5273` | `21107` | **武器フレーム情報要求** | `MsgGameWeaponFrameReq` | `Client -> Server` | `10002 (Field)` | [`21107.dms`](../dms/21107.dms) | `TMsgGameWeaponFrameReq` |
| `0x5274` | `21108` | **武器フレーム情報返答** | `MsgGameWeaponFrameAns` | `Server -> Client` | `10002 (Field)` | [`21108.dms`](../dms/21108.dms) | `TMsgGameWeaponFrameAns` |
| `0x5275` | `21109` | **戦闘フィールドキャラレベルアップ通知** | `MsgGameHuntCharLvUpNtf` | `Server -> Client` | `10002 (Field)` | [`21109.dms`](../dms/21109.dms) | `TMsgGameHuntCharLvUpNtf` |
| `0x5276` | `21110` | **ハントモンスター死亡通知** | `MsgGameHuntMonDeadNtf` | `Server -> Client` | `10002 (Field)` | [`21110.dms`](../dms/21110.dms) | `TMsgGameHuntMonDeadNtf` |
| `0x5277` | `21111` | **キャラEXPアップ通知** | `MsgGameHuntCharExpUpNtf` | `Server -> Client` | `10002 (Field)` | [`21111.dms`](../dms/21111.dms) | `TMsgGameHuntCharExpUpNtf` |
| `0x7595` | `30101` | **認証タイプ通知** | `MsgAuthTypeNtf` | `Server -> Client` | `10000 (Login)` | [`30101.dms`](../dms/30101.dms) | `TMsgAuthTypeNtf` |
| `0x7596` | `30102` | **アカウント認証要求** | `MsgLoginAuthReq` | `Client -> Server` | `10000 (Login)` | [`30102.dms`](../dms/30102.dms) | `TMsgLoginAuthReq` |
| `0x7597` | `30103` | **ログイン認証返答** | `MsgLoginAuthAns` | `Server -> Client` | `10000 (Login)` | [`30103.dms`](../dms/30103.dms) | `TMsgLoginAuthAns` |
| `0x759A` | `30106` | **ゲーム参加要求** | `MsgLoginJoinGameReq` | `Client -> Server` | `10000 (Login)` | [`30106.dms`](../dms/30106.dms) | `TMsgLoginJoinGameReq` |
| `0x759B` | `30107` | **ゲーム参加返答** | `MsgLoginJoinGameAns` | `Server -> Client` | `10000 (Login)` | [`30107.dms`](../dms/30107.dms) | `TMsgLoginJoinGameAns` |
| `0x759F` | `30111` | **ワールドリスト要求** | `MsgLoginWorldListReq` | `Client -> Server` | `10000 (Login)` | [`30111.dms`](../dms/30111.dms) | `TMsgLoginWorldListReq` |
| `0x75A0` | `30112` | **ワールドリスト返答** | `MsgLoginWorldListAns` | `Server -> Client` | `10000 (Login)` | [`30112.dms`](../dms/30112.dms) | `TMsgLoginWorldListAns` |
| `0x75A1` | `30113` | **ワールドリスト通知** | `MsgLoginWorldListNtf` | `Server -> Client` | `10000 (Login)` | [`30113.dms`](../dms/30113.dms) | `TMsgLoginWorldListNtf` |
| `0x75A2` | `30114` | **ワールド選択通知** | `MsgLoginSelectWorldReq` | `Client -> Server` | `10000 (Login)` | [`30114.dms`](../dms/30114.dms) | `TMsgLoginSelectWorldReq` |
| `0x75A3` | `30115` | **学校リスト通知** | `MsgLoginSchoolListNtf` | `Server -> Client` | `10000 (Login)` | [`30115.dms`](../dms/30115.dms) | `TMsgLoginSchoolListNtf` |
| `0x75A4` | `30116` | **名前重複チェック要求** | `MsgLoginCheckNameReq` | `Client -> Server` | `10000 (Login)` | [`30116.dms`](../dms/30116.dms) | `TMsgLoginCheckNameReq` |
| `0x75A5` | `30117` | **名前重複チェック返答** | `MsgLoginCheckNameAns` | `Server -> Client` | `10000 (Login)` | [`30117.dms`](../dms/30117.dms) | `TMsgLoginCheckNameAns` |
| `0x75A6` | `30118` | **電話番号重複確認** | `MsgLoginCheckPhoneReq` | `Client -> Server` | `10000 (Login)` | [`30118.dms`](../dms/30118.dms) | `TMsgLoginCheckPhoneReq` |
| `0x75A7` | `30119` | **電話番号重複確認返答** | `MsgLoginCheckPhoneAns` | `Server -> Client` | `10000 (Login)` | [`30119.dms`](../dms/30119.dms) | `TMsgLoginCheckPhoneAns` |
| `0x75A8` | `30120` | **キャラ作成要求** | `MsgLoginMakeCharReq` | `Client -> Server` | `10000 (Login)` | [`30120.dms`](../dms/30120.dms) | `TMsgLoginMakeCharReq` |
| `0x75A9` | `30121` | **キャラ作成返答** | `MsgLoginMakeCharAns` | `Server -> Client` | `10000 (Login)` | [`30121.dms`](../dms/30121.dms) | `TMsgLoginMakeCharAns` |
| `0x75AA` | `30122` | **キャラ削除要求** | `MsgLoginDeleteCharReq` | `Client -> Server` | `10000 (Login)` | [`30122.dms`](../dms/30122.dms) | `TMsgLoginDeleteCharReq` |
| `0x75AB` | `30123` | **キャラ削除返答** | `MsgLoginDeleteCharAns` | `Server -> Client` | `10000 (Login)` | [`30123.dms`](../dms/30123.dms) | `TMsgLoginDeleteCharAns` |
| `0x75AE` | `30126` | **リジューム通知** | `MsgLoginResumeNtf` | `Server -> Client` | `10000 (Login)` | [`30126.dms`](../dms/30126.dms) | `TMsgLoginResumeNtf` |
| `0x75AF` | `30127` | **タイムアウト通知** | `MsgUnknown_0x75AF` | `Server -> Client` | `10000 (Login)` | [`30127.dms`](../dms/30127.dms) | `TMsgUnknown_0x75AF` |
| `0x7604` | `30212` | **チャット鯖参加返答** | `MsgTransJoinCmsAns` | `Server -> Client` | `10004 (Comm)` | [`30212.dms`](../dms/30212.dms) | `TMsgTransJoinCmsAns` |
| `0x765D` | `30301` | **ロビー参加通知** | `MsgUnknown_0x765D` | `Server -> Client` | `10003 (Episode)` | [`30301.dms`](../dms/30301.dms) | `TMsgUnknown_0x765D` |
| `0x765E` | `30302` | **ロビー退出通知** | `MsgLobbyEnterReq` | `Client -> Server` | `10003 (Episode)` | [`30302.dms`](../dms/30302.dms) | `TMsgLobbyEnterReq` |
| `0x765F` | `30303` | **ロビー部屋情報通知** | `MsgUnknown_0x765F` | `Server -> Client` | `10003 (Episode)` | [`30303.dms`](../dms/30303.dms) | `TMsgUnknown_0x765F` |
| `0x7660` | `30304` | **ロビーページ選択要求** | `MsgLobbyLeaveReq` | `Client -> Server` | `10003 (Episode)` | [`30304.dms`](../dms/30304.dms) | `TMsgLobbyLeaveReq` |
| `0x7662` | `30306` | **ロビーページ情報通知** | `MsgUnknown_0x7662` | `Server -> Client` | `10003 (Episode)` | [`30306.dms`](../dms/30306.dms) | `TMsgUnknown_0x7662` |
| `0x7663` | `30307` | **エピソードページ状態通知** | `MsgUnknown_0x7663` | `Server -> Client` | `10003 (Episode)` | [`30307.dms`](../dms/30307.dms) | `TMsgUnknown_0x7663` |
| `0x7664` | `30308` | **ロビー・エピソード選択要求** | `MsgUnknown_0x7664` | `Client -> Server` | `10003 (Episode)` | [`30308.dms`](../dms/30308.dms) | `TMsgUnknown_0x7664` |
| `0x7665` | `30309` | **ロビー・エピソード選択返答** | `MsgUnknown_0x7665` | `Server -> Client` | `10003 (Episode)` | [`30309.dms`](../dms/30309.dms) | `TMsgUnknown_0x7665` |
| `0x7666` | `30310` | **ロビー参加キャラ通知** | `MsgUnknown_0x7666` | `Server -> Client` | `10003 (Episode)` | [`30310.dms`](../dms/30310.dms) | `TMsgUnknown_0x7666` |
| `0x7667` | `30311` | **ロビー退出キャラ通知** | `MsgUnknown_0x7667` | `Server -> Client` | `10003 (Episode)` | [`30311.dms`](../dms/30311.dms) | `TMsgUnknown_0x7667` |
| `0x7668` | `30312` | **ルーム作成予約要求** | `MsgLobbyRoomListReq` | `Client -> Server` | `10003 (Episode)` | [`30312.dms`](../dms/30312.dms) | `TMsgLobbyRoomListReq` |
| `0x7669` | `30313` | **ルーム作成予約返答** | `MsgUnknown_0x7669` | `Server -> Client` | `10003 (Episode)` | [`30313.dms`](../dms/30313.dms) | `TMsgUnknown_0x7669` |
| `0x766A` | `30314` | **ルーム作成要求** | `MsgLobbyCreateRoomReq` | `Client -> Server` | `10003 (Episode)` | [`30314.dms`](../dms/30314.dms) | `TMsgLobbyCreateRoomReq` |
| `0x766B` | `30315` | **ルーム作成返答** | `MsgUnknown_0x766B` | `Server -> Client` | `10003 (Episode)` | [`30315.dms`](../dms/30315.dms) | `TMsgUnknown_0x766B` |
| `0x766C` | `30316` | **ロビー・部屋作成キャンセル通知** | `MsgLobbyPageInfoNtf` | `Server -> Client` | `10003 (Episode)` | [`30316.dms`](../dms/30316.dms) | `TMsgLobbyPageInfoNtf` |
| `0x766D` | `30317` | **ロビー・待機室参加予約要求** | `MsgLobbyReserveJoinRoomReq` | `Client -> Server` | `10003 (Episode)` | [`30317.dms`](../dms/30317.dms) | `TMsgLobbyReserveJoinRoomReq` |
| `0x766E` | `30318` | **ロビー・待機室参加予約返答** | `MsgUnknown_0x766E` | `Server -> Client` | `10003 (Episode)` | [`30318.dms`](../dms/30318.dms) | `TMsgUnknown_0x766E` |
| `0x766F` | `30319` | **ロビー・待機室参加要求** | `MsgLobbyJoinRoomReq` | `Client -> Server` | `10003 (Episode)` | [`30319.dms`](../dms/30319.dms) | `TMsgLobbyJoinRoomReq` |
| `0x7670` | `30320` | **ロビー・待機室参加返答** | `MsgUnknown_0x7670` | `Server -> Client` | `10003 (Episode)` | [`30320.dms`](../dms/30320.dms) | `TMsgUnknown_0x7670` |
| `0x7671` | `30321` | **ロビー・待機室参加キャンセル通知** | `MsgUnknown_0x7671` | `Server -> Client` | `10003 (Episode)` | [`30321.dms`](../dms/30321.dms) | `TMsgUnknown_0x7671` |
| `0x7672` | `30322` | **即時ルーム参加要求** | `MsgLobbyQuickJoinReq` | `Client -> Server` | `10003 (Episode)` | [`30322.dms`](../dms/30322.dms) | `TMsgLobbyQuickJoinReq` |
| `0x7673` | `30323` | **即時ルーム参加返答** | `MsgUnknown_0x7673` | `Server -> Client` | `10003 (Episode)` | [`30323.dms`](../dms/30323.dms) | `TMsgUnknown_0x7673` |
| `0x7676` | `30326` | **利用可能エピソード情報通知** | `MsgUnknown_0x7676` | `Server -> Client` | `10003 (Episode)` | [`30326.dms`](../dms/30326.dms) | `TMsgUnknown_0x7676` |
| `0x7677` | `30327` | **待機室参加通知** | `MsgUnknown_0x7677` | `Server -> Client` | `10003 (Episode)` | [`30327.dms`](../dms/30327.dms) | `TMsgUnknown_0x7677` |
| `0x7678` | `30328` | **待機室退出通知** | `MsgWaitRoomInfoReq` | `Client -> Server` | `10003 (Episode)` | [`30328.dms`](../dms/30328.dms) | `TMsgWaitRoomInfoReq` |
| `0x7679` | `30329` | **待機室情報通知** | `MsgUnknown_0x7679` | `Server -> Client` | `10003 (Episode)` | [`30329.dms`](../dms/30329.dms) | `TMsgUnknown_0x7679` |
| `0x767A` | `30330` | **待機室・エディット要求** | `MsgWaitRoomEditReq` | `Client -> Server` | `10003 (Episode)` | [`30330.dms`](../dms/30330.dms) | `TMsgWaitRoomEditReq` |
| `0x767B` | `30331` | **待機室・エディット返答** | `MsgUnknown_0x767B` | `Server -> Client` | `10003 (Episode)` | [`30331.dms`](../dms/30331.dms) | `TMsgUnknown_0x767B` |
| `0x767C` | `30332` | **ルームキャラ情報** | `MsgUnknown_0x767C` | `Bidirectional` | `10003 (Episode)` | [`30332.dms`](../dms/30332.dms) | `TMsgUnknown_0x767C` |
| `0x767D` | `30333` | **待機室・キャラ退出通知** | `MsgUnknown_0x767D` | `Server -> Client` | `10003 (Episode)` | [`30333.dms`](../dms/30333.dms) | `TMsgUnknown_0x767D` |
| `0x767E` | `30334` | **待機室キャラ状態通知** | `MsgUnknown_0x767E` | `Server -> Client` | `10003 (Episode)` | [`30334.dms`](../dms/30334.dms) | `TMsgUnknown_0x767E` |
| `0x767F` | `30335` | **待機室長通知** | `MsgUnknown_0x767F` | `Server -> Client` | `10003 (Episode)` | [`30335.dms`](../dms/30335.dms) | `TMsgUnknown_0x767F` |
| `0x7680` | `30336` | **待機室・チーム選択** | `MsgWaitRoomSelectTeamReq` | `Client -> Server` | `10003 (Episode)` | [`30336.dms`](../dms/30336.dms) | `TMsgWaitRoomSelectTeamReq` |
| `0x7681` | `30337` | **待機室・アイテム変更通知** | `MsgUnknown_0x7681` | `Server -> Client` | `10003 (Episode)` | [`30337.dms`](../dms/30337.dms) | `TMsgUnknown_0x7681` |
| `0x7682` | `30338` | **待機室・招待検証通知** | `MsgWaitRoomInviteReq` | `Client -> Server` | `10003 (Episode)` | [`30338.dms`](../dms/30338.dms) | `TMsgWaitRoomInviteReq` |
| `0x7683` | `30339` | **待機室・招待検証返答** | `MsgUnknown_0x7683` | `Server -> Client` | `10003 (Episode)` | [`30339.dms`](../dms/30339.dms) | `TMsgUnknown_0x7683` |
| `0x7684` | `30340` | **待機室・招待通知** | `MsgUnknown_0x7684` | `Server -> Client` | `10003 (Episode)` | [`30340.dms`](../dms/30340.dms) | `TMsgUnknown_0x7684` |
| `0x7686` | `30342` | **待機室・招待申請通知** | `MsgUnknown_0x7686` | `Server -> Client` | `10003 (Episode)` | [`30342.dms`](../dms/30342.dms) | `TMsgUnknown_0x7686` |
| `0x7687` | `30343` | **待機室・招待申請通知** | `MsgUnknown_0x7687` | `Server -> Client` | `10003 (Episode)` | [`30343.dms`](../dms/30343.dms) | `TMsgUnknown_0x7687` |
| `0x768C` | `30348` | **待機室・準備通知** | `MsgWaitRoomReadyStartReq` | `Client -> Server` | `10003 (Episode)` | [`30348.dms`](../dms/30348.dms) | `TMsgWaitRoomReadyStartReq` |
| `0x768D` | `30349` | **待機室・スタート通知** | `MsgWaitRoomLeaveReq` | `Client -> Server` | `10003 (Episode)` | [`30349.dms`](../dms/30349.dms) | `TMsgWaitRoomLeaveReq` |
| `0x76D4` | `30420` | **アトラクションプレイ開始通知** | `MsgUnknown_0x76D4` | `Server -> Client` | `10002 (Field)` | [`30420.dms`](../dms/30420.dms) | `TMsgUnknown_0x76D4` |
| `0x772B` | `30507` | **友達登録申請要求** | `MsgUnknown_0x772B` | `Client -> Server` | `10004 (Comm)` | [`30507.dms`](../dms/30507.dms) | `TMsgUnknown_0x772B` |
| `0x772C` | `30508` | **友達登録申請返答** | `MsgUnknown_0x772C` | `Server -> Client` | `10004 (Comm)` | [`30508.dms`](../dms/30508.dms) | `TMsgUnknown_0x772C` |
| `0x772D` | `30509` | **友達登録通知** | `MsgUnknown_0x772D` | `Server -> Client` | `10004 (Comm)` | [`30509.dms`](../dms/30509.dms) | `TMsgUnknown_0x772D` |
| `0x7732` | `30514` | **友達登校通知** | `MsgUnknown_0x7732` | `Server -> Client` | `10004 (Comm)` | [`30514.dms`](../dms/30514.dms) | `TMsgUnknown_0x7732` |
| `0x7733` | `30515` | **友達下校通知** | `MsgUnknown_0x7733` | `Server -> Client` | `10004 (Comm)` | [`30515.dms`](../dms/30515.dms) | `TMsgUnknown_0x7733` |
| `0x7735` | `30517` | **メール(memo)送信** | `MsgUnknown_0x7735` | `Bidirectional` | `10004 (Comm)` | [`30517.dms`](../dms/30517.dms) | `TMsgUnknown_0x7735` |
| `0x7736` | `30518` | **メール(memo)受信** | `MsgUnknown_0x7736` | `Bidirectional` | `10004 (Comm)` | [`30518.dms`](../dms/30518.dms) | `TMsgUnknown_0x7736` |
| `0x7740` | `30528` | **電話発信** | `MsgUnknown_0x7740` | `Bidirectional` | `10004 (Comm)` | [`30528.dms`](../dms/30528.dms) | `TMsgUnknown_0x7740` |
| `0x7741` | `30529` | **電話発信返答** | `MsgUnknown_0x7741` | `Server -> Client` | `10004 (Comm)` | [`30529.dms`](../dms/30529.dms) | `TMsgUnknown_0x7741` |
| `0x7742` | `30530` | **電話着信** | `MsgUnknown_0x7742` | `Bidirectional` | `10004 (Comm)` | [`30530.dms`](../dms/30530.dms) | `TMsgUnknown_0x7742` |
| `0x7743` | `30531` | **通話応答** | `MsgUnknown_0x7743` | `Bidirectional` | `10004 (Comm)` | [`30531.dms`](../dms/30531.dms) | `TMsgUnknown_0x7743` |
| `0x7744` | `30532` | **電話通話開始** | `MsgUnknown_0x7744` | `Bidirectional` | `10004 (Comm)` | [`30532.dms`](../dms/30532.dms) | `TMsgUnknown_0x7744` |
| `0x7746` | `30534` | **電話メッセージ送信** | `MsgUnknown_0x7746` | `Bidirectional` | `10004 (Comm)` | [`30534.dms`](../dms/30534.dms) | `TMsgUnknown_0x7746` |
| `0x7747` | `30535` | **電話切断** | `MsgUnknown_0x7747` | `Bidirectional` | `10004 (Comm)` | [`30535.dms`](../dms/30535.dms) | `TMsgUnknown_0x7747` |
| `0x7759` | `30553` | **チャットサーバーエコー通知** | `MsgCommEchoNtf` | `Server -> Client` | `10004 (Comm)` | [`30553.dms`](../dms/30553.dms) | `TMsgCommEchoNtf` |
| `0x77A4` | `30628` | **チャットサーバー同好会情報要求** | `MsgUnknown_0x77A4` | `Client -> Server` | `10002 (Field)` | [`30628.dms`](../dms/30628.dms) | `TMsgUnknown_0x77A4` |
| `0x77A5` | `30629` | **チャットサーバー同好会情報返答** | `MsgUnknown_0x77A5` | `Server -> Client` | `10002 (Field)` | [`30629.dms`](../dms/30629.dms) | `TMsgUnknown_0x77A5` |
| `0x7919` | `31001` | **攻撃要求** | `MsgGameAttackReq` | `Client -> Server` | `10002 (Field)` | [`31001.dms`](../dms/31001.dms) | `TMsgGameAttackReq` |
| `0x791A` | `31002` | **攻撃返答** | `MsgGameAttackAns` | `Server -> Client` | `10002 (Field)` | [`31002.dms`](../dms/31002.dms) | `TMsgGameAttackAns` |
| `0x791B` | `31003` | **キャラ死亡通知** | `MsgGameDieCharNtf` | `Server -> Client` | `10002 (Field)` | [`31003.dms`](../dms/31003.dms) | `TMsgGameDieCharNtf` |
| `0x791C` | `31004` | **チャージポイント更新通知** | `MsgGameChargePointUpdateNtf` | `Server -> Client` | `10002 (Field)` | [`31004.dms`](../dms/31004.dms) | `TMsgGameChargePointUpdateNtf` |
| `0x791E` | `31006` | **停止通知** | `MsgGameMoveStopReq` | `Client -> Server` | `10002 (Field)` | [`31006.dms`](../dms/31006.dms) | `TMsgGameMoveStopReq` |
| `0x791F` | `31007` | **正規位置通知** | `MsgUnknown_0x791F` | `Server -> Client` | `10002 (Field)` | [`31007.dms`](../dms/31007.dms) | `TMsgUnknown_0x791F` |
| `0x7921` | `31009` | **キャラ位置通知** | `MsgGamePosSyncReq` | `Client -> Server` | `10002 (Field)` | [`31009.dms`](../dms/31009.dms) | `TMsgGamePosSyncReq` |
| `0x7922` | `31010` | **AOI=Area Of Interest(有効領域)ブロック通知** | `MsgGameJumpReq` | `Client -> Server` | `10002 (Field)` | [`31010.dms`](../dms/31010.dms) | `TMsgGameJumpReq` |
| `0x7923` | `31011` | **スキル発動要求** | `MsgGameSkillActiveReq` | `Client -> Server` | `10002 (Field)` | [`31011.dms`](../dms/31011.dms) | `TMsgGameSkillActiveReq` |
| `0x7924` | `31012` | **スキル発動返答** | `MsgGameSkillActiveAns` | `Server -> Client` | `10002 (Field)` | [`31012.dms`](../dms/31012.dms) | `TMsgGameSkillActiveAns` |
| `0x7925` | `31013` | **スキル準備通知** | `MsgGameSkillCastReq` | `Client -> Server` | `10002 (Field)` | [`31013.dms`](../dms/31013.dms) | `TMsgGameSkillCastReq` |
| `0x7928` | `31016` | **消費アイテム使用要求** | `MsgGameSkillHitReq` | `Client -> Server` | `10002 (Field)` | [`31016.dms`](../dms/31016.dms) | `TMsgGameSkillHitReq` |
| `0x7929` | `31017` | **COITEM使用返答** | `MsgGameUseCoItemAns` | `Server -> Client` | `10002 (Field)` | [`31017.dms`](../dms/31017.dms) | `TMsgGameUseCoItemAns` |
| `0x792A` | `31018` | **交換失敗通知** | `MsgUnknown_0x792A` | `Server -> Client` | `10002 (Field)` | [`31018.dms`](../dms/31018.dms) | `TMsgUnknown_0x792A` |
| `0x792B` | `31019` | **交換申請通知** | `MsgGameTradeProposeReq` | `Client -> Server` | `10002 (Field)` | [`31019.dms`](../dms/31019.dms) | `TMsgGameTradeProposeReq` |
| `0x792C` | `31020` | **交換応答要求** | `MsgUnknown_0x792C` | `Client -> Server` | `10002 (Field)` | [`31020.dms`](../dms/31020.dms) | `TMsgUnknown_0x792C` |
| `0x792D` | `31021` | **交換応答返答** | `MsgGameTradeAcceptReq` | `Client -> Server` | `10002 (Field)` | [`31021.dms`](../dms/31021.dms) | `TMsgGameTradeAcceptReq` |
| `0x792E` | `31022` | **交換相手了承通知** | `MsgUnknown_0x792E` | `Server -> Client` | `10002 (Field)` | [`31022.dms`](../dms/31022.dms) | `TMsgUnknown_0x792E` |
| `0x792F` | `31023` | **交換相手アイテム情報通知** | `MsgUnknown_0x792F` | `Server -> Client` | `10002 (Field)` | [`31023.dms`](../dms/31023.dms) | `TMsgUnknown_0x792F` |
| `0x7930` | `31024` | **交換アイテム追加通知** | `MsgGameTradeAddItemReq` | `Client -> Server` | `10002 (Field)` | [`31024.dms`](../dms/31024.dms) | `TMsgGameTradeAddItemReq` |
| `0x7931` | `31025` | **交換キャンセル通知** | `MsgGameTradeCancelReq` | `Client -> Server` | `10002 (Field)` | [`31025.dms`](../dms/31025.dms) | `TMsgGameTradeCancelReq` |
| `0x7932` | `31026` | **交換OK通知** | `MsgGameTradeLockReq` | `Client -> Server` | `10002 (Field)` | [`31026.dms`](../dms/31026.dms) | `TMsgGameTradeLockReq` |
| `0x7933` | `31027` | **交換終了通知** | `MsgGameTradeConfirmReq` | `Client -> Server` | `10002 (Field)` | [`31027.dms`](../dms/31027.dms) | `TMsgGameTradeConfirmReq` |
| `0x7934` | `31028` | **交換完了通知** | `MsgUnknown_0x7934` | `Server -> Client` | `10002 (Field)` | [`31028.dms`](../dms/31028.dms) | `TMsgUnknown_0x7934` |
| `0x7935` | `31029` | **NPCからアイテム購入要求** | `MsgGameInventoryMoveReq` | `Client -> Server` | `10002 (Field)` | [`31029.dms`](../dms/31029.dms) | `TMsgGameInventoryMoveReq` |
| `0x7936` | `31030` | **NPCからアイテム購入返答** | `MsgUnknown_0x7936` | `Server -> Client` | `10002 (Field)` | [`31030.dms`](../dms/31030.dms) | `TMsgUnknown_0x7936` |
| `0x7937` | `31031` | **アイテム売値要求** | `MsgGameItemDropReq` | `Client -> Server` | `10002 (Field)` | [`31031.dms`](../dms/31031.dms) | `TMsgGameItemDropReq` |
| `0x7938` | `31032` | **アイテム売値返答** | `MsgUnknown_0x7938` | `Server -> Client` | `10002 (Field)` | [`31032.dms`](../dms/31032.dms) | `TMsgUnknown_0x7938` |
| `0x7939` | `31033` | **アイテム売却要求** | `MsgGameItemPickUpReq` | `Client -> Server` | `10002 (Field)` | [`31033.dms`](../dms/31033.dms) | `TMsgGameItemPickUpReq` |
| `0x793A` | `31034` | **アイテム売却返答** | `MsgUnknown_0x793A` | `Server -> Client` | `10002 (Field)` | [`31034.dms`](../dms/31034.dms) | `TMsgUnknown_0x793A` |
| `0x793B` | `31035` | **NPCダイアログ開始通知** | `MsgGameItemUseReq` | `Client -> Server` | `10002 (Field)` | [`31035.dms`](../dms/31035.dms) | `TMsgGameItemUseReq` |
| `0x793E` | `31038` | **NPCダイアログイベント通知** | `MsgUnknown_0x793E` | `Server -> Client` | `10002 (Field)` | [`31038.dms`](../dms/31038.dms) | `TMsgUnknown_0x793E` |
| `0x793F` | `31039` | **NPCダイアログ商品リスト通知** | `MsgUnknown_0x793F` | `Server -> Client` | `10002 (Field)` | [`31039.dms`](../dms/31039.dms) | `TMsgUnknown_0x793F` |
| `0x7940` | `31040` | **NPCアクション終了通知** | `MsgGameQuickSlotSetReq` | `Client -> Server` | `10002 (Field)` | [`31040.dms`](../dms/31040.dms) | `TMsgGameQuickSlotSetReq` |
| `0x7942` | `31042` | **NPC生成通知** | `MsgGameVisualAttachNtf` | `Server -> Client` | `10002 (Field)` | [`31042.dms`](../dms/31042.dms) | `TMsgGameVisualAttachNtf` |
| `0x7944` | `31044` | **BEITEM装備要求** | `MsgGameStarEquipReq` | `Client -> Server` | `10002 (Field)` | [`31044.dms`](../dms/31044.dms) | `TMsgGameStarEquipReq` |
| `0x7945` | `31045` | **BEITEM装備返答** | `MsgGameEquipAns` | `Server -> Client` | `10002 (Field)` | [`31045.dms`](../dms/31045.dms) | `TMsgGameEquipAns` |
| `0x7946` | `31046` | **BEITEM装備解除要求** | `MsgGameStarUnequipReq` | `Client -> Server` | `10002 (Field)` | [`31046.dms`](../dms/31046.dms) | `TMsgGameStarUnequipReq` |
| `0x7947` | `31047` | **BEITEM装備解除返答** | `MsgGameUnequipAns` | `Server -> Client` | `10002 (Field)` | [`31047.dms`](../dms/31047.dms) | `TMsgGameUnequipAns` |
| `0x7948` | `31048` | **他キャラ情報通知** | `MsgUnknown_0x7948` | `Server -> Client` | `10002 (Field)` | [`31048.dms`](../dms/31048.dms) | `TMsgUnknown_0x7948` |
| `0x7949` | `31049` | **他キャラ消滅通知** | `MsgUnknown_0x7949` | `Server -> Client` | `10002 (Field)` | [`31049.dms`](../dms/31049.dms) | `TMsgUnknown_0x7949` |
| `0x794A` | `31050` | **ホットキー設定通知** | `MsgGameReinforceItemReq` | `Client -> Server` | `10002 (Field)` | [`31050.dms`](../dms/31050.dms) | `TMsgGameReinforceItemReq` |
| `0x7952` | `31058` | **キャラ情報通知** | `MsgGameCharInfoNtf` | `Server -> Client` | `10002 (Field)` | [`31058.dms`](../dms/31058.dms) | `TMsgGameCharInfoNtf` |
| `0x7956` | `31062` | **フィールド情報完了通知** | `MsgGameFieldInfoDoneNtf` | `Server -> Client` | `10002 (Field)` | [`31062.dms`](../dms/31062.dms) | `TMsgGameFieldInfoDoneNtf` |
| `0x7957` | `31063` | **エピソードプレイ再開通知** | `MsgUnknown_0x7957` | `Server -> Client` | `10002 (Field)` | [`31063.dms`](../dms/31063.dms) | `TMsgUnknown_0x7957` |
| `0x795A` | `31066` | **フィールド読み込み開始通知** | `MsgGameFieldLoadingStartNtf` | `Server -> Client` | `10002 (Field)` | [`31066.dms`](../dms/31066.dms) | `TMsgGameFieldLoadingStartNtf` |
| `0x795B` | `31067` | **フィールド読み込み完了通知** | `MsgGameEmoteReq` | `Client -> Server` | `10002 (Field)` | [`31067.dms`](../dms/31067.dms) | `TMsgGameEmoteReq` |
| `0x795C` | `31068` | **トリガ作用通知(モンスター消去)** | `MsgGameFieldEntitySpawnNtf` | `Server -> Client` | `10002 (Field)` | [`31068.dms`](../dms/31068.dms) | `TMsgGameFieldEntitySpawnNtf` |
| `0x795D` | `31069` | **レバー引き要求** | `MsgUnknown_0x795D` | `Client -> Server` | `10002 (Field)` | [`31069.dms`](../dms/31069.dms) | `TMsgUnknown_0x795D` |
| `0x795E` | `31070` | **レバー引き返答** | `MsgUnknown_0x795E` | `Server -> Client` | `10002 (Field)` | [`31070.dms`](../dms/31070.dms) | `TMsgUnknown_0x795E` |
| `0x7963` | `31075` | **チャット通知** | `MsgGameChatNtf` | `Server -> Client` | `10002 (Field)` | [`31075.dms`](../dms/31075.dms) | `TMsgGameChatNtf` |
| `0x7964` | `31076` | **メッセージ通知** | `MsgUnknown_0x7964` | `Server -> Client` | `10002 (Field)` | [`31076.dms`](../dms/31076.dms) | `TMsgUnknown_0x7964` |
| `0x7965` | `31077` | **ワープ要求** | `MsgGameChannelSwitchReq` | `Client -> Server` | `10002 (Field)` | [`31077.dms`](../dms/31077.dms) | `TMsgGameChannelSwitchReq` |
| `0x7966` | `31078` | **ワープ返答** | `MsgGameWarpStartNtf` | `Server -> Client` | `10002 (Field)` | [`31078.dms`](../dms/31078.dms) | `TMsgGameWarpStartNtf` |
| `0x7967` | `31079` | **ワープ完了通知** | `MsgGameWarpGateReq` | `Client -> Server` | `10002 (Field)` | [`31079.dms`](../dms/31079.dms) | `TMsgGameWarpGateReq` |
| `0x7968` | `31080` | **ワープ結果通知** | `MsgGameWarpResultNtf` | `Server -> Client` | `10002 (Field)` | [`31080.dms`](../dms/31080.dms) | `TMsgGameWarpResultNtf` |
| `0x7969` | `31081` | **モンスター移動通知** | `MsgGameMonMoveNtf` | `Server -> Client` | `10002 (Field)` | [`31081.dms`](../dms/31081.dms) | `TMsgGameMonMoveNtf` |
| `0x796A` | `31082` | **モンスター攻撃通知** | `MsgGameMonAttackNtf` | `Server -> Client` | `10002 (Field)` | [`31082.dms`](../dms/31082.dms) | `TMsgGameMonAttackNtf` |
| `0x796C` | `31084` | **モンスター死亡通知** | `MsgUnknown_0x796C` | `Server -> Client` | `10002 (Field)` | [`31084.dms`](../dms/31084.dms) | `TMsgUnknown_0x796C` |
| `0x796D` | `31085` | **モンスター生成通知** | `MsgGameMonStatusNtf` | `Server -> Client` | `10002 (Field)` | [`31085.dms`](../dms/31085.dms) | `TMsgGameMonStatusNtf` |
| `0x796E` | `31086` | **モンスター情報通知** | `MsgGameMonInfoNtf` | `Server -> Client` | `10002 (Field)` | [`31086.dms`](../dms/31086.dms) | `TMsgGameMonInfoNtf` |
| `0x796F` | `31087` | **キャラEXP通知** | `MsgUnknown_0x796F` | `Server -> Client` | `10002 (Field)` | [`31087.dms`](../dms/31087.dms) | `TMsgUnknown_0x796F` |
| `0x7970` | `31088` | **キャラレベルアップ通知** | `MsgGameCharLvUpNtf` | `Server -> Client` | `10002 (Field)` | [`31088.dms`](../dms/31088.dms) | `TMsgGameCharLvUpNtf` |
| `0x7972` | `31090` | **エピソード結果通知** | `MsgUnknown_0x7972` | `Server -> Client` | `10002 (Field)` | [`31090.dms`](../dms/31090.dms) | `TMsgUnknown_0x7972` |
| `0x7974` | `31092` | **戦利品箱終了要求** | `MsgGameBootyBoxDoneReq` | `Client -> Server` | `10002 (Field)` | [`31092.dms`](../dms/31092.dms) | `TMsgGameBootyBoxDoneReq` |
| `0x7975` | `31093` | **戦利品箱終了返答** | `MsgUnknown_0x7975` | `Server -> Client` | `10002 (Field)` | [`31093.dms`](../dms/31093.dms) | `TMsgUnknown_0x7975` |
| `0x7977` | `31095` | **クエスト結果通知** | `MsgUnknown_0x7977` | `Server -> Client` | `10002 (Field)` | [`31095.dms`](../dms/31095.dms) | `TMsgUnknown_0x7977` |
| `0x7981` | `31105` | **アナウンス通知** | `MsgUnknown_0x7981` | `Server -> Client` | `10002 (Field)` | [`31105.dms`](../dms/31105.dms) | `TMsgUnknown_0x7981` |
| `0x7988` | `31112` | **水情報通知** | `MsgUnknown_0x7988` | `Server -> Client` | `10002 (Field)` | [`31112.dms`](../dms/31112.dms) | `TMsgUnknown_0x7988` |
| `0x798C` | `31116` | **キャラ着座要求** | `MsgGameSitDownReq` | `Client -> Server` | `10002 (Field)` | [`31116.dms`](../dms/31116.dms) | `TMsgGameSitDownReq` |
| `0x798D` | `31117` | **キャラ着座返答** | `MsgGameSitDownAns` | `Server -> Client` | `10002 (Field)` | [`31117.dms`](../dms/31117.dms) | `TMsgGameSitDownAns` |
| `0x798E` | `31118` | **キャラ起立通知** | `MsgGameStandUpNtf` | `Server -> Client` | `10002 (Field)` | [`31118.dms`](../dms/31118.dms) | `TMsgGameStandUpNtf` |
| `0x798F` | `31119` | **キャラ向き変更要求** | `MsgGameCharDirectNtf` | `Client -> Server` | `10002 (Field)` | [`31119.dms`](../dms/31119.dms) | `TMsgGameCharDirectNtf` |
| `0x7992` | `31122` | **タイマー開始通知** | `MsgUnknown_0x7992` | `Server -> Client` | `10002 (Field)` | [`31122.dms`](../dms/31122.dms) | `TMsgUnknown_0x7992` |
| `0x7996` | `31126` | **プッシュオブジェクト要求** | `MsgGamePushObjectReq` | `Client -> Server` | `10002 (Field)` | [`31126.dms`](../dms/31126.dms) | `TMsgGamePushObjectReq` |
| `0x7997` | `31127` | **プッシュオブジェクト返答** | `MsgUnknown_0x7997` | `Server -> Client` | `10002 (Field)` | [`31127.dms`](../dms/31127.dms) | `TMsgUnknown_0x7997` |
| `0x7998` | `31128` | **プッシュオブジェクト停止通知** | `MsgUnknown_0x7998` | `Server -> Client` | `10002 (Field)` | [`31128.dms`](../dms/31128.dms) | `TMsgUnknown_0x7998` |
| `0x799D` | `31133` | **スキルエフェクト開始通知** | `MsgUnknown_0x799D` | `Server -> Client` | `10002 (Field)` | [`31133.dms`](../dms/31133.dms) | `TMsgUnknown_0x799D` |
| `0x799E` | `31134` | **スキルエフェクト停止通知** | `MsgUnknown_0x799E` | `Server -> Client` | `10002 (Field)` | [`31134.dms`](../dms/31134.dms) | `TMsgUnknown_0x799E` |
| `0x799F` | `31135` | **攻撃・移動速度変更通知** | `MsgGameAtkMovChangeNtf` | `Server -> Client` | `10002 (Field)` | [`31135.dms`](../dms/31135.dms) | `TMsgGameAtkMovChangeNtf` |
| `0x79A0` | `31136` | **ミニマップ変更通知** | `MsgUnknown_0x79A0` | `Server -> Client` | `10002 (Field)` | [`31136.dms`](../dms/31136.dms) | `TMsgUnknown_0x79A0` |
| `0x79A1` | `31137` | **称号入手通知** | `MsgUnknown_0x79A1` | `Server -> Client` | `10002 (Field)` | [`31137.dms`](../dms/31137.dms) | `TMsgUnknown_0x79A1` |
| `0x79A2` | `31138` | **称号装備要求** | `MsgGameLockerOpenReq` | `Client -> Server` | `10002 (Field)` | [`31138.dms`](../dms/31138.dms) | `TMsgGameLockerOpenReq` |
| `0x79A3` | `31139` | **称号装備返答** | `MsgGameEquipTitleAns` | `Server -> Client` | `10002 (Field)` | [`31139.dms`](../dms/31139.dms) | `TMsgGameEquipTitleAns` |
| `0x79A4` | `31140` | **称号装備解除要求** | `MsgGameLockerCloseReq` | `Client -> Server` | `10002 (Field)` | [`31140.dms`](../dms/31140.dms) | `TMsgGameLockerCloseReq` |
| `0x79A5` | `31141` | **称号装備解除返答** | `MsgUnknown_0x79A5` | `Server -> Client` | `10002 (Field)` | [`31141.dms`](../dms/31141.dms) | `TMsgUnknown_0x79A5` |
| `0x79A8` | `31144` | **結晶レベル要求** | `MsgGameEnchantCrystalReq` | `Client -> Server` | `10002 (Field)` | [`31144.dms`](../dms/31144.dms) | `TMsgGameEnchantCrystalReq` |
| `0x79A9` | `31145` | **結晶レベル返答** | `MsgUnknown_0x79A9` | `Server -> Client` | `10002 (Field)` | [`31145.dms`](../dms/31145.dms) | `TMsgUnknown_0x79A9` |
| `0x79AA` | `31146` | **結晶精製要求** | `MsgGameCrystallizeReq` | `Client -> Server` | `10002 (Field)` | [`31146.dms`](../dms/31146.dms) | `TMsgGameCrystallizeReq` |
| `0x79AB` | `31147` | **結晶精製返答** | `MsgUnknown_0x79AB` | `Server -> Client` | `10002 (Field)` | [`31147.dms`](../dms/31147.dms) | `TMsgUnknown_0x79AB` |
| `0x79B2` | `31154` | **進級情報通知** | `MsgGamePromoteInfoNtf` | `Server -> Client` | `10002 (Field)` | [`31154.dms`](../dms/31154.dms) | `TMsgGamePromoteInfoNtf` |
| `0x79B5` | `31157` | **エピソード情報通知** | `MsgUnknown_0x79B5` | `Server -> Client` | `10002 (Field)` | [`31157.dms`](../dms/31157.dms) | `TMsgUnknown_0x79B5` |
| `0x79BA` | `31162` | **オブジェクト破壊通知** | `MsgUnknown_0x79BA` | `Server -> Client` | `10002 (Field)` | [`31162.dms`](../dms/31162.dms) | `TMsgUnknown_0x79BA` |
| `0x79BB` | `31163` | **オブジェクト消去通知** | `MsgUnknown_0x79BB` | `Server -> Client` | `10002 (Field)` | [`31163.dms`](../dms/31163.dms) | `TMsgUnknown_0x79BB` |
| `0x79C2` | `31170` | **特殊番号コール要求** | `MsgGameSpecialPhoneCallReq` | `Client -> Server` | `10002 (Field)` | [`31170.dms`](../dms/31170.dms) | `TMsgGameSpecialPhoneCallReq` |
| `0x79C3` | `31171` | **特殊番号コール返答** | `MsgUnknown_0x79C3` | `Server -> Client` | `10002 (Field)` | [`31171.dms`](../dms/31171.dms) | `TMsgUnknown_0x79C3` |
| `0x79C9` | `31177` | **ランダムボックス結果通知** | `MsgUnknown_0x79C9` | `Server -> Client` | `10002 (Field)` | [`31177.dms`](../dms/31177.dms) | `TMsgUnknown_0x79C9` |
| `0x79D3` | `31187` | **対象領域へアクション通知** | `MsgGameBroadcastAOINtf` | `Server -> Client` | `10002 (Field)` | [`31187.dms`](../dms/31187.dms) | `TMsgGameBroadcastAOINtf` |
| `0x79D4` | `31188` | **移動間隔通知** | `MsgGameZoneNameNtf` | `Server -> Client` | `10002 (Field)` | [`31188.dms`](../dms/31188.dms) | `TMsgGameZoneNameNtf` |
| `0x79D5` | `31189` | **拡張移動通知** | `MsgGameMoveExNtf` | `Server -> Client` | `10002 (Field)` | [`31189.dms`](../dms/31189.dms) | `TMsgGameMoveExNtf` |
| `0x79E3` | `31203` | **戦利品箱割り当て通知** | `MsgUnknown_0x79E3` | `Server -> Client` | `10002 (Field)` | [`31203.dms`](../dms/31203.dms) | `TMsgUnknown_0x79E3` |
| `0x79E4` | `31204` | **定期プレイヤーHP情報通知** | `MsgUnknown_0x79E4` | `Server -> Client` | `10002 (Field)` | [`31204.dms`](../dms/31204.dms) | `TMsgUnknown_0x79E4` |
| `0x79E5` | `31205` | **プレイヤーステータス通知** | `MsgUnknown_0x79E5` | `Server -> Client` | `10002 (Field)` | [`31205.dms`](../dms/31205.dms) | `TMsgUnknown_0x79E5` |
| `0x79E8` | `31208` | **モンスターHP情報通知** | `MsgUnknown_0x79E8` | `Server -> Client` | `10002 (Field)` | [`31208.dms`](../dms/31208.dms) | `TMsgUnknown_0x79E8` |
| `0x79E9` | `31209` | **イベントシステム参加通知** | `MsgUnknown_0x79E9` | `Server -> Client` | `10002 (Field)` | [`31209.dms`](../dms/31209.dms) | `TMsgUnknown_0x79E9` |
| `0x79EC` | `31212` | **クーポン結果通知** | `MsgUnknown_0x79EC` | `Server -> Client` | `10002 (Field)` | [`31212.dms`](../dms/31212.dms) | `TMsgUnknown_0x79EC` |
| `0x79ED` | `31213` | **プレゼントボックス結果通知** | `MsgUnknown_0x79ED` | `Server -> Client` | `10002 (Field)` | [`31213.dms`](../dms/31213.dms) | `TMsgUnknown_0x79ED` |
| `0x79EE` | `31214` | **武器熟練度アップ通知** | `MsgUnknown_0x79EE` | `Server -> Client` | `10002 (Field)` | [`31214.dms`](../dms/31214.dms) | `TMsgUnknown_0x79EE` |
| `0x79EF` | `31215` | **源石装着要求** | `MsgUnknown_0x79EF` | `Client -> Server` | `10002 (Field)` | [`31215.dms`](../dms/31215.dms) | `TMsgUnknown_0x79EF` |
| `0x79F0` | `31216` | **源石装着返答** | `MsgUnknown_0x79F0` | `Server -> Client` | `10002 (Field)` | [`31216.dms`](../dms/31216.dms) | `TMsgUnknown_0x79F0` |
| `0x79FB` | `31227` | **販売スキルリスト通知** | `MsgUnknown_0x79FB` | `Server -> Client` | `10002 (Field)` | [`31227.dms`](../dms/31227.dms) | `TMsgUnknown_0x79FB` |
| `0x79FC` | `31228` | **スキルホットキー設定通知** | `MsgUnknown_0x79FC` | `Server -> Client` | `10002 (Field)` | [`31228.dms`](../dms/31228.dms) | `TMsgUnknown_0x79FC` |
| `0x79FD` | `31229` | **MSG_GAME_PASSIVE_EFFECT_START_NTF(常時エフェクト表示開始？)** | `MsgUnknown_0x79FD` | `Bidirectional` | `10002 (Field)` | [`31229.dms`](../dms/31229.dms) | `TMsgUnknown_0x79FD` |
| `0x7A00` | `31232` | **モンスター所有権獲得通知** | `MsgGameMonDeadNtf` | `Server -> Client` | `10002 (Field)` | [`31232.dms`](../dms/31232.dms) | `TMsgGameMonDeadNtf` |
| `0x7A01` | `31233` | **モンスター所有権喪失通知** | `MsgUnknown_0x7A01` | `Server -> Client` | `10002 (Field)` | [`31233.dms`](../dms/31233.dms) | `TMsgUnknown_0x7A01` |
| `0x7A04` | `31236` | **アイテム廃棄通知** | `MsgUnknown_0x7A04` | `Server -> Client` | `10002 (Field)` | [`31236.dms`](../dms/31236.dms) | `TMsgUnknown_0x7A04` |
| `0xA029` | `41001` | **ロッカーENTER通知** | `MsgUnknown_0xA029` | `Server -> Client` | `10002 (Field)` | [`41001.dms`](../dms/41001.dms) | `TMsgUnknown_0xA029` |
| `0xA02A` | `41002` | **ロッカー終了通知** | `MsgUnknown_0xA02A` | `Server -> Client` | `10002 (Field)` | [`41002.dms`](../dms/41002.dms) | `TMsgUnknown_0xA02A` |
| `0xA02B` | `41003` | **ロッカーオープン要求** | `MsgUnknown_0xA02B` | `Client -> Server` | `10002 (Field)` | [`41003.dms`](../dms/41003.dms) | `TMsgUnknown_0xA02B` |
| `0xA02C` | `41004` | **ロッカーオープン返答** | `MsgUnknown_0xA02C` | `Server -> Client` | `10002 (Field)` | [`41004.dms`](../dms/41004.dms) | `TMsgUnknown_0xA02C` |
| `0xA02D` | `41005` | **ロッカーアイテム情報通知** | `MsgUnknown_0xA02D` | `Server -> Client` | `10002 (Field)` | [`41005.dms`](../dms/41005.dms) | `TMsgUnknown_0xA02D` |
| `0xA02F` | `41007` | **ロッカーアイテム移動通知** | `MsgUnknown_0xA02F` | `Server -> Client` | `10002 (Field)` | [`41007.dms`](../dms/41007.dms) | `TMsgUnknown_0xA02F` |
| `0xA030` | `41008` | **ロッカーアイテム移動完了通知** | `MsgUnknown_0xA030` | `Server -> Client` | `10002 (Field)` | [`41008.dms`](../dms/41008.dms) | `TMsgUnknown_0xA030` |
| `0xA411` | `42001` | **カプセル販売機参加通知** | `MsgUnknown_0xA411` | `Server -> Client` | `10002 (Field)` | [`42001.dms`](../dms/42001.dms) | `TMsgUnknown_0xA411` |
| `0xA412` | `42002` | **カプセル販売機商品情報通知** | `MsgUnknown_0xA412` | `Server -> Client` | `10002 (Field)` | [`42002.dms`](../dms/42002.dms) | `TMsgUnknown_0xA412` |
| `0xA413` | `42003` | **カプセル販売機購入要求** | `MsgGameCapsuleBuyReq` | `Client -> Server` | `10002 (Field)` | [`42003.dms`](../dms/42003.dms) | `TMsgGameCapsuleBuyReq` |
| `0xA414` | `42004` | **カプセル販売機購入返答** | `MsgGameCapsuleBuyAns` | `Server -> Client` | `10002 (Field)` | [`42004.dms`](../dms/42004.dms) | `TMsgGameCapsuleBuyAns` |
| `0xA415` | `42005` | **カプセル販売機退出通知** | `MsgGameCapsuleExitNtf` | `Server -> Client` | `10002 (Field)` | [`42005.dms`](../dms/42005.dms) | `TMsgGameCapsuleExitNtf` |
| `0xA416` | `42006` | **カプセル販売機退出返答** | `MsgUnknown_0xA416` | `Server -> Client` | `10002 (Field)` | [`42006.dms`](../dms/42006.dms) | `TMsgUnknown_0xA416` |
| `0xA419` | `42009` | **ダンスアイテム使用通知** | `MsgUnknown_0xA419` | `Server -> Client` | `10002 (Field)` | [`42009.dms`](../dms/42009.dms) | `TMsgUnknown_0xA419` |

---

## Shared Structure Scripts

Alongside individual opcode parsers, the protocol utilizes universal structured data models defined in:
* [`struct.dms`](../dms/struct.dms): Universal Character Display (Login 458B), Full Character State (1971B), Items, and Rooms.
* [`struct35.dms`](../dms/struct35.dms): Phone Call structures, Messenger buddy information, and Lobby PC entries.

---

## Numerical Opcode Series Guide

* **`20000` Series (`0x4E20` - `0x4E2F`)**: Core System, Version Check, Network Ping, and Heartbeat.
* **`21000` Series (`0x5208` - `0x5274`)**: Field Entities, Kiosks, Hair Shop, Star Cash Shop, Vending, Dialogs.
* **`30100` Series (`0x7595` - `0x75AE`)**: LoginServer, Authentication, Character Selection, Creation/Deletion.
* **`30200` & `30500` Series (`0x7604`, `0x7720` - `0x7759`)**: CommServer, Friends, Whispers, Club Chat.
* **`30300` & `30400` Series (`0x765E` - `0x7695`)**: EpisodeServer, Lobby Matchmaking, Waiting Rooms.
* **`31000` Series (`0x7900` - `0x79FF`)**: Field Gameplay, Movement, Combat, Monster HP, Inventory, Warp, Trades.
* **`42000` Series (`0xA410` - `0xA415`)**: Capsule Vending Machines (Gacha).
