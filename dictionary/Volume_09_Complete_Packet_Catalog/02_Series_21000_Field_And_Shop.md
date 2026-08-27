# Volume 9: Series 21000: Field Entities, Cash Shop, Hair, Kiosks & Vending

This chapter provides the complete, byte-for-byte specifications for all 53 opcodes in this range.

---

## Opcode 0x520B (21003): `MsgGameFieldEnterStatReadyNtf` [自動回復開始通知]

* **Original Japanese DMS Title**: `自動回復開始通知`
* **Raw DMS Script**: [`21003.dms`](../dms/21003.dms)
* **Legacy Delphi Class**: `TMsgGameStartRegainNtf` (Address: `005A925C`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21003.dms
function parse(packet){
	packet.setTitle("自動回復開始通知");
	packet.readSingle("basicRegain(時間単位回復量？)");
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameStartRegainNtf.Create(BasicRegain:Single)
005A925C    push        ebp
 005A925D    mov         ebp,esp
 005A925F    push        ebx
 005A9260    push        esi
 005A9261    test        dl,dl
>005A9263    je          005A926D
 005A9265    add         esp,0FFFFFFF0
 005A9268    call        @ClassCreate
 005A926D    mov         ebx,edx
 005A926F    mov         esi,eax
 005A9271    mov         dx,520B
 005A9275    mov         eax,esi
 005A9277    call        TYgPacket.WriteID
 005A927C    push        dword ptr [ebp+8]
 005A927F    mov         eax,esi
 005A9281    call        TYgPacket.WriteSingle
 005A9286    mov         eax,esi
 005A928
```

---

## Opcode 0x520C (21004): `MsgGameFadeOutNtf` [回復停止通知]

* **Original Japanese DMS Title**: `回復停止通知`
* **Raw DMS Script**: [`21004.dms`](../dms/21004.dms)
* **Legacy Delphi Class**: `TMsgGameFadeOutNtf` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21004.dms
function parse(packet){
	with(packet){
		setTitle("回復停止通知");
	}
}
```

---

## Opcode 0x520D (21005): `MsgGameStatDeltaNtf` [HP設定通知]

* **Original Japanese DMS Title**: `HP設定通知`
* **Raw DMS Script**: [`21005.dms`](../dms/21005.dms)
* **Legacy Delphi Class**: `TMsgGameSetHpNtf` (Address: `005A92DC`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21005.dms
function parse(packet){
	packet.setTitle("HP設定通知");
	packet.readWord("HP");
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameSetHpNtf.Create(Chara:TChara)
005A92DC    push        ebx
 005A92DD    push        esi
 005A92DE    push        edi
 005A92DF    test        dl,dl
>005A92E1    je          005A92EB
 005A92E3    add         esp,0FFFFFFF0
 005A92E6    call        @ClassCreate
 005A92EB    mov         esi,ecx
 005A92ED    mov         ebx,edx
 005A92EF    mov         edi,eax
 005A92F1    mov         dx,520D
 005A92F5    mov         eax,edi
 005A92F7    call        TYgPacket.WriteID
 005A92FC    mov         eax,esi
 005A92FE    call        0060FE00
 005A9303    mov         edx,eax
 005A9305    mov         eax,edi
 005A9307    call        TYgPac
```

---

## Opcode 0x520F (21007): `MsgGameSetStateNtf` [ステータス通知]

* **Original Japanese DMS Title**: `ステータス通知`
* **Raw DMS Script**: [`21007.dms`](../dms/21007.dms)
* **Legacy Delphi Class**: `TMsgGameSetStateNtf` (Address: `005A93A4`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21007.dms
function parse(packet){
	with(packet){
		setTitle("ステータス通知");
		readByte("学年");
		readBinary(1, "(padding)");
		readWord("レベル");
		readWord("最大HP");
		readWord("体力");
		readWord("瞬発力");
		readWord("技術");
		readWord("運");
		readBinary(2, "(padding)");
		readInt32("攻撃力");
		readInt32("防御力");
		readInt32("命中");
		readInt32("回避");
		readInt32("攻撃速度");
		readInt32("移動速度");
		readInt32("クールタイム");
		readInt32("クリティカル");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameSetStateNtf.Create(Chara:TChara)
005A93A4    push        ebx
 005A93A5    push        esi
 005A93A6    push        edi
 005A93A7    test        dl,dl
>005A93A9    je          005A93B3
 005A93AB    add         esp,0FFFFFFF0
 005A93AE    call        @ClassCreate
 005A93B3    mov         esi,ecx
 005A93B5    mov         ebx,edx
 005A93B7    mov         edi,eax
 005A93B9    mov         dx,520F
 005A93BD    mov         eax,edi
 005A93BF    call        TYgPacket.WriteID
 005A93C4    movzx       edx,byte ptr [esi+0E8];TChara.Grade:Integer
 005A93CB    mov         eax,edi
 005A93CD    call        TYgPacket.WriteByte
 005A93D2    mov 
```

---

## Opcode 0x5210 (21008): `MsgGotoSvrNtf` [サーバー移動通知]

* **Original Japanese DMS Title**: `サーバー移動通知`
* **Raw DMS Script**: [`21008.dms`](../dms/21008.dms)
* **Legacy Delphi Class**: `TMsgGotoSvrNtf` (Address: `005A94B0`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21008.dms
function parse(packet){
	with(packet){
		setTitle("サーバー移動通知");
		readIPPort("移動先");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGotoSvrNtf.Create(Address:Integer; Port:Word)
005A94B0    push        ebp
 005A94B1    mov         ebp,esp
 005A94B3    push        ebx
 005A94B4    push        esi
 005A94B5    push        edi
 005A94B6    test        dl,dl
>005A94B8    je          005A94C2
 005A94BA    add         esp,0FFFFFFF0
 005A94BD    call        @ClassCreate
 005A94C2    mov         esi,ecx
 005A94C4    mov         ebx,edx
 005A94C6    mov         edi,eax
 005A94C8    mov         dx,5210
 005A94CC    mov         eax,edi
 005A94CE    call        TYgPacket.WriteID
 005A94D3    mov         edx,esi
 005A94D5    mov         eax,edi
 005A94D7    call        TYgPacket.W
```

---

## Opcode 0x5211 (21009): `MsgPingTimeReq` [サーバー参加通知]

* **Original Japanese DMS Title**: `サーバー参加通知`
* **Raw DMS Script**: [`21009.dms`](../dms/21009.dms)
* **Legacy Delphi Class**: `TMsgPingTimeReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21009.dms
function parse(packet){
	packet.setTitle("サーバー参加通知");
	packet.readInt32("キャラID");
	packet.readInt32("accountSN");
	packet.readInt32("サーバー番号");
	packet.readWord("年");
	packet.readWord("月");
	packet.readWord("曜");
	packet.readWord("日");
	packet.readWord("時");
	packet.readWord("分");
	packet.readWord("秒");
	packet.readWord("ミリ秒");
	packet.readUInt32("sessionKey");
}
```

---

## Opcode 0x5212 (21010): `MsgEnterScsNtf` [スクールサーバー参加通知]

* **Original Japanese DMS Title**: `スクールサーバー参加通知`
* **Raw DMS Script**: [`21010.dms`](../dms/21010.dms)
* **Legacy Delphi Class**: `TMsgEnterScsNtf` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21010.dms
function parse(packet){
	packet.setTitle("スクールサーバー参加通知");
}
```

---

## Opcode 0x5213 (21011): `MsgEnterScsReq` [スクールサーバーログアウト通知]

* **Original Japanese DMS Title**: `スクールサーバーログアウト通知`
* **Raw DMS Script**: [`21011.dms`](../dms/21011.dms)
* **Legacy Delphi Class**: `TMsgEnterScsReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21011.dms
function parse(packet){
	with(packet){
		setTitle("スクールサーバーログアウト通知");
	}
}
```

---

## Opcode 0x5214 (21012): `MsgLeaveScsNtf` [アトラクションサーバーログイン通知]

* **Original Japanese DMS Title**: `アトラクションサーバーログイン通知`
* **Raw DMS Script**: [`21012.dms`](../dms/21012.dms)
* **Legacy Delphi Class**: `TMsgLeaveScsNtf` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21012.dms
function parse(packet){
	with(packet){
		setTitle("アトラクションサーバーログイン通知");
	}
}
```

---

## Opcode 0x5216 (21014): `MsgUnknown_0x5216` [エスケープ要求通知]

* **Original Japanese DMS Title**: `エスケープ要求通知`
* **Raw DMS Script**: [`21014.dms`](../dms/21014.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x5216` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21014.dms
function parse(packet){
	with(packet){
		setTitle("エスケープ要求通知");
		readByte("svrType");
	}
}
```

---

## Opcode 0x5217 (21015): `MsgUnknown_0x5217` [エスケープ承認通知]

* **Original Japanese DMS Title**: `エスケープ承認通知`
* **Raw DMS Script**: [`21015.dms`](../dms/21015.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x5217` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21015.dms
function parse(packet){
	with(packet){
		setTitle("エスケープ承認通知");
	}
}
```

---

## Opcode 0x5218 (21016): `MsgUnknown_0x5218` [エスケープキャンセル通知]

* **Original Japanese DMS Title**: `エスケープキャンセル通知`
* **Raw DMS Script**: [`21016.dms`](../dms/21016.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x5218` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21016.dms
function parse(packet){
	with(packet){
		setTitle("エスケープキャンセル通知");
	}
}
```

---

## Opcode 0x521B (21019): `MsgObjectCreateNtf` [オブジェクト生成通知]

* **Original Japanese DMS Title**: `オブジェクト生成通知`
* **Raw DMS Script**: [`21019.dms`](../dms/21019.dms)
* **Legacy Delphi Class**: `TMsgObjectCreateNtf` (Address: `005A95BC`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21019.dms
function parse(packet){
	packet.setTitle("オブジェクト生成通知");
	packet.readUInt32("id");
	packet.readUInt32("type");
	packet.readUInt32("subid");
	packet.readUInt32("idCli");
	packet.readUInt32("shell");
	packet.readSingle("posX");
	packet.readSingle("posY");
	packet.readByte("direction");
	packet.readByte("visible");
	packet.readByte("usable");
	packet.readBinary(1, "(padding)");
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgObjectCreateNtf.Create(Obj:TFieldObject)
005A95BC    push        ebx
 005A95BD    push        esi
 005A95BE    push        edi
 005A95BF    test        dl,dl
>005A95C1    je          005A95CB
 005A95C3    add         esp,0FFFFFFF0
 005A95C6    call        @ClassCreate
 005A95CB    mov         esi,ecx
 005A95CD    mov         ebx,edx
 005A95CF    mov         edi,eax
 005A95D1    mov         dx,521B
 005A95D5    mov         eax,edi
 005A95D7    call        TYgPacket.WriteID
 005A95DC    mov         edx,dword ptr [esi+4];TFieldObject.ID:Integer
 005A95DF    mov         eax,edi
 005A95E1    call        TYgPacket.WriteInt32
 005A95E6    m
```

---

## Opcode 0x521F (21023): `MsgObjectClickReq` [オブジェクト使用要求]

* **Original Japanese DMS Title**: `オブジェクト使用要求`
* **Raw DMS Script**: [`21023.dms`](../dms/21023.dms)
* **Legacy Delphi Class**: `TMsgObjectClickReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21023.dms
function parse(packet){
	with(packet){
		setTitle("オブジェクト使用要求");
		readInt32("ID");
	}
}
```

---

## Opcode 0x5220 (21024): `MsgObjectUseAns` [オブジェクト使用返答]

* **Original Japanese DMS Title**: `オブジェクト使用返答`
* **Raw DMS Script**: [`21024.dms`](../dms/21024.dms)
* **Legacy Delphi Class**: `TMsgObjectUseAns` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21024.dms
function parse(packet){
	with(packet){
		setTitle("オブジェクト使用返答");
		readInt32("戻り値");
		readInt32("ID");
	}
}
```

---

## Opcode 0x5221 (21025): `MsgGameShopEnterReq` [ガイド掲示板ENTER通知]

* **Original Japanese DMS Title**: `ガイド掲示板ENTER通知`
* **Raw DMS Script**: [`21025.dms`](../dms/21025.dms)
* **Legacy Delphi Class**: `TMsgGameShopEnterReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21025.dms
function parse(packet){
	with(packet){
		setTitle("ガイド掲示板ENTER通知");
		readInt32("ID");
	}
}
```

---

## Opcode 0x5222 (21026): `MsgGameShopLeaveReq` [ガイド掲示板LEAVE通知]

* **Original Japanese DMS Title**: `ガイド掲示板LEAVE通知`
* **Raw DMS Script**: [`21026.dms`](../dms/21026.dms)
* **Legacy Delphi Class**: `TMsgObjectUseAns` (Address: `005A9704`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21026.dms
function parse(packet){
	with(packet){
		setTitle("ガイド掲示板LEAVE通知");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgObjectUseAns.Create(?:?)
005A9704    push        ebx
 005A9705    push        esi
 005A9706    test        dl,dl
>005A9708    je          005A9712
 005A970A    add         esp,0FFFFFFF0
 005A970D    call        @ClassCreate
 005A9712    mov         ebx,edx
 005A9714    mov         esi,eax
 005A9716    mov         dx,5222
 005A971A    mov         eax,esi
 005A971C    call        TYgPacket.WriteID
 005A9721    mov         eax,esi
 005A9723    test        bl,bl
>005A9725    je          005A9736
 005A9727    call        @AfterConstruction
 005A972C    pop         dword ptr fs:[0]
 005A9733    add         esp,0C
 005A9736 
```

---

## Opcode 0x5223 (21027): `MsgGameShopListReq` [ガイド情報要求]

* **Original Japanese DMS Title**: `ガイド情報要求`
* **Raw DMS Script**: [`21027.dms`](../dms/21027.dms)
* **Legacy Delphi Class**: `TMsgGameShopListReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21027.dms
function parse(packet){
	with(packet){
		setTitle("ガイド情報要求");
		readByte("種類");
		readByte("学年");
	}
}
```

---

## Opcode 0x5224 (21028): `MsgGameShopBuyAns` [ガイド情報返答]

* **Original Japanese DMS Title**: `ガイド情報返答`
* **Raw DMS Script**: [`21028.dms`](../dms/21028.dms)
* **Legacy Delphi Class**: `TMsgGameShopBuyAns` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21028.dms
function parse(packet){
	with(packet){
		setTitle("ガイド情報返答");
	}
}
```

---

## Opcode 0x5225 (21029): `MsgGameShopListNtf` [エピソードガイド情報通知]

* **Original Japanese DMS Title**: `エピソードガイド情報通知`
* **Raw DMS Script**: [`21029.dms`](../dms/21029.dms)
* **Legacy Delphi Class**: `TMsgGameShopListNtf` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21029.dms
function parse(packet){
	with(packet){
		setTitle("エピソードガイド情報通知");
		readByte("学年");
		var cnt = readWord("countInfo");
		for(var i = 1; i <= cnt; i++){
			readByte("  学年" + i.toString());
			readBinary(1, "  (padding)");
			readWord("  エピソードID？" + i.toString());
			readWord("  エピソードID？" + i.toString());
		}
	}
}
```

---

## Opcode 0x5227 (21031): `MsgObjectStateNtf` [ロビー状態通知]

* **Original Japanese DMS Title**: `ロビー状態通知`
* **Raw DMS Script**: [`21031.dms`](../dms/21031.dms)
* **Legacy Delphi Class**: `TMsgLobbyStateNtf` (Address: `005A973C`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21031.dms
function parse(packet){
	packet.setTitle("ロビー状態通知");
	packet.readInt32("ID");
	packet.readByte("状態");
	packet.readBinary(3, "(padding)");
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgLobbyStateNtf.Create(FieldObjectID:Integer; State:Byte)
005A973C    push        ebp
 005A973D    mov         ebp,esp
 005A973F    push        ebx
 005A9740    push        esi
 005A9741    push        edi
 005A9742    test        dl,dl
>005A9744    je          005A974E
 005A9746    add         esp,0FFFFFFF0
 005A9749    call        @ClassCreate
 005A974E    mov         esi,ecx
 005A9750    mov         ebx,edx
 005A9752    mov         edi,eax
 005A9754    mov         dx,5227
 005A9758    mov         eax,edi
 005A975A    call        TYgPacket.WriteID
 005A975F    mov         edx,esi
 005A9761    mov         eax,edi
 005A9763    call        TYgPacket.W
```

---

## Opcode 0x5229 (21033): `MsgGameNpcDialogNtf` [拡張NPCダイアログ返答通知]

* **Original Japanese DMS Title**: `拡張NPCダイアログ返答通知`
* **Raw DMS Script**: [`21033.dms`](../dms/21033.dms)
* **Legacy Delphi Class**: `TMsgGameNpcDialogExResponseNtf` (Address: `005A97A0`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21033.dms
function parse(packet){
	packet.setTitle("拡張NPCダイアログ返答通知");
	packet.readInt32("NPDID");
	packet.readInt32("ダイアログID");
	packet.readInt32("カットイン種類");
	packet.readWStr(packet.readWord("テキスト長"), "テキスト");
	var cnt = packet.readWord("選択肢数");
	for(var i = 1; i <= cnt; i++){
		packet.readWStr( packet.readWord("選択肢長" + i.toString()), "選択肢" + i.toString());
	}
	packet.readInt32("nTimeOut");
	packet.readInt32("idChoiceOnTimeOut");
	packet.readInt32("bShowCloseButton");
	packet.readInt32("bEnableBgFrameClick");
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameNpcDialogExResponseNtf.Create(Chara:TChara; Dialog:TNpcDialog)
005A97A0    push        ebp
 005A97A1    mov         ebp,esp
 005A97A3    add         esp,0FFFFFFEC
 005A97A6    push        ebx
 005A97A7    push        esi
 005A97A8    xor         ebx,ebx
 005A97AA    mov         dword ptr [ebp-4],ebx
 005A97AD    test        dl,dl
>005A97AF    je          005A97B9
 005A97B1    add         esp,0FFFFFFF0
 005A97B4    call        @ClassCreate
 005A97B9    mov         dword ptr [ebp-0C],ecx
 005A97BC    mov         byte ptr [ebp-5],dl
 005A97BF    mov         ebx,eax
 005A97C1    xor         eax,eax
 005A97C3    push        ebp
 005A97C4    push        5A9920

```

---

## Opcode 0x522A (21034): `MsgUnknown_0x522A` [拡張NPCダイアログクエストリスト返答通知]

* **Original Japanese DMS Title**: `拡張NPCダイアログクエストリスト返答通知`
* **Raw DMS Script**: [`21034.dms`](../dms/21034.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x522A` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21034.dms
function parse(packet){
	with(packet){
		setTitle("拡張NPCダイアログクエストリスト返答通知");
		readInt32("idNpc");
		readInt32("idDialog");
		readInt32("cateCutIn");
		readWStr(readWord("sDialogTextLength"), "sDialogText");
		var cnt = readWord("vecQuestSelectionCount");
		for(var i = 1; i <= cnt; i++){
			readInt32("  idQuestSelection" + i.toString());
			readWStr(readWord("  sQuestSelectionLength" + i.toString()), "  sQuestSelection" + i.toString());
		}
		readInt32("bShowCloseButton");
		readInt32("bEnableBgFrameClick");
	}
}
```

---

## Opcode 0x522B (21035): `MsgUnknown_0x522B` [NPCダイアログクエスト情報返答通知]

* **Original Japanese DMS Title**: `NPCダイアログクエスト情報返答通知`
* **Raw DMS Script**: [`21035.dms`](../dms/21035.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x522B` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21035.dms
function parse(packet){
	with(packet){
		setTitle("NPCダイアログクエスト情報返答通知");
		readInt32("idNpc");
		readInt32("idDialog");
		readInt32("cateCutIn");
		readInt32("idQuest");
		readWStr(readWord("sDialogTextLen"), "sDialogText");
		var cnt = readWord("vecSelectionText");
		for(var i = 0; i < cnt; i++){
			readWStr(readWord("SelectionTextLen"), "SelectionText");
		}
		readInt32("bShowCloseButton");
		readInt32("bEnableBgFrameClick");
	}
}
```

---

## Opcode 0x522C (21036): `MsgNpcDialogSelectReq` [拡張NPCダイアログ選択通知]

* **Original Japanese DMS Title**: `拡張NPCダイアログ選択通知`
* **Raw DMS Script**: [`21036.dms`](../dms/21036.dms)
* **Legacy Delphi Class**: `TMsgNpcDialogSelectReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21036.dms
function parse(packet){
	packet.setTitle("拡張NPCダイアログ選択通知");
	packet.readInt32("ダイアログID");
	packet.readUInt32("選択肢番号+0x80000000");
	packet.readInt32("クエストID");
}
```

---

## Opcode 0x5233 (21043): `MsgGameByulShopBeginReq` [アイテムショップ開始要求]

* **Original Japanese DMS Title**: `アイテムショップ開始要求`
* **Raw DMS Script**: [`21043.dms`](../dms/21043.dms)
* **Legacy Delphi Class**: `TMsgGameByulShopBeginReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21043.dms
function parse(packet){
	with(packet){
		setTitle("アイテムショップ開始要求");
	}
}
```

---

## Opcode 0x5234 (21044): `MsgGameByulShopBeginAns` [アイテムショップ開始返答]

* **Original Japanese DMS Title**: `アイテムショップ開始返答`
* **Raw DMS Script**: [`21044.dms`](../dms/21044.dms)
* **Legacy Delphi Class**: `TMsgGameNpcDialogExResponseNtf` (Address: `005A9A04`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21044.dms
function parse(packet){
	with(packet){
		setTitle("アイテムショップ開始返答");
		readInt32("戻り値");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameNpcDialogExResponseNtf.Create(NpcID:Integer; ?:?; Selections:string; Text:string; CutIn:Integer; DialogID:Integer)
005A9A04    push        ebx
 005A9A05    push        esi
 005A9A06    test        dl,dl
>005A9A08    je          005A9A12
 005A9A0A    add         esp,0FFFFFFF0
 005A9A0D    call        @ClassCreate
 005A9A12    mov         ebx,edx
 005A9A14    mov         esi,eax
 005A9A16    mov         dx,5234
 005A9A1A    mov         eax,esi
 005A9A1C    call        TYgPacket.WriteID
 005A9A21    xor         edx,edx
 005A9A23    mov         eax,esi
 005A9A25    call        TYgPacket.WriteEC
 005A9A2A    mov         eax,esi
 005A9A2C    test        bl,bl
>005A9A2E    je          005A9A3F
 005A9A30    call  
```

---

## Opcode 0x5235 (21045): `MsgGameByulShopEndReq` [アイテムショップ終了要求]

* **Original Japanese DMS Title**: `アイテムショップ終了要求`
* **Raw DMS Script**: [`21045.dms`](../dms/21045.dms)
* **Legacy Delphi Class**: `TMsgGameByulShopEndReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21045.dms
function parse(packet){
	with(packet){
		setTitle("アイテムショップ終了要求");
	}
}
```

---

## Opcode 0x5236 (21046): `MsgGameByulShopEndAns` [アイテムショップ終了返答]

* **Original Japanese DMS Title**: `アイテムショップ終了返答`
* **Raw DMS Script**: [`21046.dms`](../dms/21046.dms)
* **Legacy Delphi Class**: `TMsgGameByulShopEndAns` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21046.dms
function parse(packet){
	with(packet){
		setTitle("アイテムショップ終了返答");
		readInt32("戻り値");
	}
}
```

---

## Opcode 0x523A (21050): `MsgGameByulChargeReq` [MSG_GAME_BYUL_CHARGE_REQ]

* **Original Japanese DMS Title**: `MSG_GAME_BYUL_CHARGE_REQ`
* **Raw DMS Script**: [`21050.dms`](../dms/21050.dms)
* **Legacy Delphi Class**: `TMsgGameByulChargeReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21050.dms
function parse(packet){
	with(packet){
		setTitle("MSG_GAME_BYUL_CHARGE_REQ");
	}
}
```

---

## Opcode 0x523B (21051): `MsgGameByulChargeAns` [MSG_GAME_BYUL_CHARGE_ANS]

* **Original Japanese DMS Title**: `MSG_GAME_BYUL_CHARGE_ANS`
* **Raw DMS Script**: [`21051.dms`](../dms/21051.dms)
* **Legacy Delphi Class**: `TMsgGameByulChargeAns` (Address: `005A9A84`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21051.dms
function parse(packet){
	with(packet){
		setTitle("MSG_GAME_BYUL_CHARGE_ANS");
		readInt32("戻り値");
		readInt32("shopPoint");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameByulChargeAns.Create(Chara:TChara)
005A9A84    push        ebx
 005A9A85    push        esi
 005A9A86    push        edi
 005A9A87    test        dl,dl
>005A9A89    je          005A9A93
 005A9A8B    add         esp,0FFFFFFF0
 005A9A8E    call        @ClassCreate
 005A9A93    mov         esi,ecx
 005A9A95    mov         ebx,edx
 005A9A97    mov         edi,eax
 005A9A99    mov         dx,523B
 005A9A9D    mov         eax,edi
 005A9A9F    call        TYgPacket.WriteID
 005A9AA4    xor         edx,edx
 005A9AA6    mov         eax,edi
 005A9AA8    call        TYgPacket.WriteEC
 005A9AAD    mov         edx,dword ptr [esi+1BC];TChara
```

---

## Opcode 0x523C (21052): `MsgGameByulProductListReq` [MSG_GAME_BYUL_PRODUCT_LIST_REQ]

* **Original Japanese DMS Title**: `MSG_GAME_BYUL_PRODUCT_LIST_REQ`
* **Raw DMS Script**: [`21052.dms`](../dms/21052.dms)
* **Legacy Delphi Class**: `TMsgGameByulProductListReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21052.dms
function parse(packet){
	with(packet){
		setTitle("MSG_GAME_BYUL_PRODUCT_LIST_REQ");
	}
}
```

---

## Opcode 0x523D (21053): `MsgGameByulProductListAns` [MSG_GAME_BYUL_PRODUCT_LIST_ANS]

* **Original Japanese DMS Title**: `MSG_GAME_BYUL_PRODUCT_LIST_ANS`
* **Raw DMS Script**: [`21053.dms`](../dms/21053.dms)
* **Legacy Delphi Class**: `TMsgGameByulProductListAns` (Address: `005A9AD8`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21053.dms
import struct.*;
function parse(packet){
	with(packet){
		setTitle("MSG_GAME_BYUL_PRODUCT_LIST_ANS");
		readInt32("戻り値");
		var cnt = readWord("vecByulProductCount");
		byul_product(packet, cnt);
		/*
		for(var i = 1; i <= cnt; i++){
			readInt32("typeByulItem" + i.toString());
			readInt32("shopPoint" + i.toString());
			readInt32();
			readInt32();
			readInt32();
		}
		*/
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameByulProductListAns.Create(ProductList:TProductList)
005A9AD8    push        ebp
 005A9AD9    mov         ebp,esp
 005A9ADB    add         esp,0FFFFFFF4
 005A9ADE    push        ebx
 005A9ADF    test        dl,dl
>005A9AE1    je          005A9AEB
 005A9AE3    add         esp,0FFFFFFF0
 005A9AE6    call        @ClassCreate
 005A9AEB    mov         ebx,ecx
 005A9AED    mov         byte ptr [ebp-5],dl
 005A9AF0    mov         dword ptr [ebp-4],eax
 005A9AF3    mov         dx,523D
 005A9AF7    mov         eax,dword ptr [ebp-4]
 005A9AFA    call        TYgPacket.WriteID
 005A9AFF    cmp         dword ptr [ebx+8],0;TProductList.FListHelper:TListHelper
```

---

## Opcode 0x523E (21054): `MsgGameByulProductBuyReq` [スターアイテム購入要求]

* **Original Japanese DMS Title**: `スターアイテム購入要求`
* **Raw DMS Script**: [`21054.dms`](../dms/21054.dms)
* **Legacy Delphi Class**: `TMsgGameByulProductBuyReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21054.dms
function parse(packet){
	with(packet){
		setTitle("スターアイテム購入要求");
		readInt32("productID");
		readInt32("count");
	}
}
```

---

## Opcode 0x523F (21055): `MsgGameByulProductBuyAns` [スターアイテム購入返答]

* **Original Japanese DMS Title**: `スターアイテム購入返答`
* **Raw DMS Script**: [`21055.dms`](../dms/21055.dms)
* **Legacy Delphi Class**: `TMsgGameByulProductBuyAns` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21055.dms
function parse(packet){
	with(packet){
		setTitle("スターアイテム購入返答");
		readInt32("戻り値");
		readInt32("productID");
		readInt64("productPrice");
		readInt32("byul");
		readInt32("priceType");
		var cnt = readWord("vecByulItemCount");
		for(var i = 1; i <= cnt; i++){
			readInt32("  typeByulItem" + i.toString());
			readInt32("  idByulItem" + i.toString());
			readInt32("  idByulItem" + i.toString());
			readInt32("  countByulItem" + i.toString());
			readInt32("  bUse" + i.toString());
			readInt32("  remainTimeInSecond" + i.toString());
			readInt32("  reinforceslot[0]" + i.toString());
			readInt32("  reinforceslot[1]" + i.toString());
			readInt32("  reinforceslot[2]" + i.toString());
			readInt32("  reinforceslot[3]" + i.toString());
			readInt32("  reinforceslot[4]" + i.toString());
		}
		//history
		readInt32("eventDate");
		readWStr(12, "kind");
		readInt32("byul");
		readWStr(0xCA, "usage");
		readBinary(2, "(padding)");
	}
}
```

---

## Opcode 0x524B (21067): `MsgGameUseByulBeItemReq` [装備系スターアイテム使用要求]

* **Original Japanese DMS Title**: `装備系スターアイテム使用要求`
* **Raw DMS Script**: [`21067.dms`](../dms/21067.dms)
* **Legacy Delphi Class**: `TMsgGameUseByulBeItemReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21067.dms
function parse(packet){
	with(packet){
		setTitle("装備系スターアイテム使用要求");
		readUInt32("BeItemシリアルNo");
		readUInt32();
	}
}
```

---

## Opcode 0x524C (21068): `MsgGameUseByulBeItemAns` [装備系スターアイテム使用応答]

* **Original Japanese DMS Title**: `装備系スターアイテム使用応答`
* **Raw DMS Script**: [`21068.dms`](../dms/21068.dms)
* **Legacy Delphi Class**: `TMsgGameByulProductBuyAns` (Address: `005A9E54`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21068.dms
function parse(packet){
	with(packet){
		setTitle("装備系スターアイテム使用応答");
		readInt32("戻り値");
		readInt32("キャラID");
		readInt32("byulItemSN_1");
		readInt32("byulItemSN_2");
		readInt32("effectType");
		readInt32("resultValue");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameByulProductBuyAns.Create(?:?; ?:?; ?:?)
005A9E54    push        ebp
 005A9E55    mov         ebp,esp
 005A9E57    push        ecx
 005A9E58    push        ebx
 005A9E59    push        esi
 005A9E5A    push        edi
 005A9E5B    test        dl,dl
>005A9E5D    je          005A9E67
 005A9E5F    add         esp,0FFFFFFF0
 005A9E62    call        @ClassCreate
 005A9E67    mov         edi,ecx
 005A9E69    mov         byte ptr [ebp-1],dl
 005A9E6C    mov         ebx,eax
 005A9E6E    mov         esi,dword ptr [ebp+8]
 005A9E71    mov         dx,524C
 005A9E75    mov         eax,ebx
 005A9E77    call        TYgPacket.WriteID
 005A9E7C    x
```

---

## Opcode 0x524F (21071): `MsgUnknown_0x524F` [未対応]

* **Original Japanese DMS Title**: `未対応`
* **Raw DMS Script**: [`21071.dms`](../dms/21071.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x524F` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21071.dms
function parse(packet){
	with(packet){
		setTitle("未対応");
	}
}
```

---

## Opcode 0x5250 (21072): `MsgUnknown_0x5250` [プレゼントされたスターアイテム返答]

* **Original Japanese DMS Title**: `プレゼントされたスターアイテム返答`
* **Raw DMS Script**: [`21072.dms`](../dms/21072.dms)
* **Legacy Delphi Class**: `TMsgGameByulItemUseAns` (Address: `005A9F54`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21072.dms
function parse(packet){
	with(packet){
		setTitle("プレゼントされたスターアイテム返答");
		readInt32("戻り値");
		var cnt = readWord("vecByulProductPresentBoxCount");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameByulItemUseAns.Create(Chara:TChara; ItemID:Int64; ?:?)
005A9F54    push        ebx
 005A9F55    push        esi
 005A9F56    test        dl,dl
>005A9F58    je          005A9F62
 005A9F5A    add         esp,0FFFFFFF0
 005A9F5D    call        @ClassCreate
 005A9F62    mov         ebx,edx
 005A9F64    mov         esi,eax
 005A9F66    mov         dx,5250
 005A9F6A    mov         eax,esi
 005A9F6C    call        TYgPacket.WriteID
 005A9F71    xor         edx,edx
 005A9F73    mov         eax,esi
 005A9F75    call        TYgPacket.WriteEC
 005A9F7A    xor         edx,edx
 005A9F7C    mov         eax,esi
 005A9F7E    call        TYgPacket.WriteWord
 005A9
```

---

## Opcode 0x5257 (21079): `MsgUnknown_0x5257` [MSG_GAME_BYUL_BYUL_HISTORY_REQ]

* **Original Japanese DMS Title**: `MSG_GAME_BYUL_BYUL_HISTORY_REQ`
* **Raw DMS Script**: [`21079.dms`](../dms/21079.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x5257` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21079.dms
function parse(packet){
	with(packet){
		setTitle("MSG_GAME_BYUL_BYUL_HISTORY_REQ");
	}
}
```

---

## Opcode 0x5258 (21080): `MsgUnknown_0x5258` [MSG_GAME_BYUL_BYUL_HISTORY_ANS(未完成)]

* **Original Japanese DMS Title**: `MSG_GAME_BYUL_BYUL_HISTORY_ANS(未完成)`
* **Raw DMS Script**: [`21080.dms`](../dms/21080.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x5258` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21080.dms
function parse(packet){
	with(packet){
		setTitle("MSG_GAME_BYUL_BYUL_HISTORY_ANS(未完成)");
		readInt32("戻り値");
		var cnt = readWord("vecByulHistoryCount");
		for(var i = 1; i <= cnt; i++){
			//???
		}
	}
}
```

---

## Opcode 0x525D (21085): `MsgUnknown_0x525D` [台紙色変更通知(1:r 2:o 3:y 4:g 5:c 6:b 7:m)]

* **Original Japanese DMS Title**: `台紙色変更通知(1:r 2:o 3:y 4:g 5:c 6:b 7:m)`
* **Raw DMS Script**: [`21085.dms`](../dms/21085.dms)
* **Legacy Delphi Class**: `TMsgGameIdTagBackgroundChangeNtf` (Address: `005AA038`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21085.dms
function parse(packet){
	with(packet){
		setTitle("台紙色変更通知(1:r 2:o 3:y 4:g 5:c 6:b 7:m)");
		readUInt32("キャラID");
		readUInt32("背景色番号");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameIdTagBackgroundChangeNtf.Create(Chara:TChara)
005AA038    push        ebx
 005AA039    push        esi
 005AA03A    push        edi
 005AA03B    test        dl,dl
>005AA03D    je          005AA047
 005AA03F    add         esp,0FFFFFFF0
 005AA042    call        @ClassCreate
 005AA047    mov         esi,ecx
 005AA049    mov         ebx,edx
 005AA04B    mov         edi,eax
 005AA04D    mov         dx,525D
 005AA051    mov         eax,edi
 005AA053    call        TYgPacket.WriteID
 005AA058    mov         edx,dword ptr [esi+0E0];TChara.ID:Integer
 005AA05E    mov         eax,edi
 005AA060    call        TYgPacket.WriteInt32
 005AA065    mov  
```

---

## Opcode 0x525F (21087): `MsgUnknown_0x525F` [プラカード状態変更返答]

* **Original Japanese DMS Title**: `プラカード状態変更返答`
* **Raw DMS Script**: [`21087.dms`](../dms/21087.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x525F` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21087.dms
function parse(packet){
	with(packet){
		setTitle("プラカード状態変更返答");
		readInt32("err");
		readInt32("idChar");
		readInt32("bShow");
	}
}
```

---

## Opcode 0x5261 (21089): `MsgUnknown_0x5261` [プラカード内容変更返答]

* **Original Japanese DMS Title**: `プラカード内容変更返答`
* **Raw DMS Script**: [`21089.dms`](../dms/21089.dms)
* **Legacy Delphi Class**: `TMsgGamePicketStatusChangeAns` (Address: `005AA0F4`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21089.dms
function parse(packet){
	with(packet){
		setTitle("プラカード内容変更返答");
		readInt32("err");
		readInt32("idChar");
		readWStr(0x4A, "contents");
		readBinary(2, "(padding)");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGamePicketStatusChangeAns.Create(?:?)
constructor TMsgGamePicketContentsChangeAns.Create(?:?);
begin
 005AA0F4    push        ebp
 005AA0F5    mov         ebp,esp
 005AA0F7    push        ebx
 005AA0F8    push        esi
 005AA0F9    push        edi
 005AA0FA    test        dl,dl
>005AA0FC    je          005AA106
 005AA0FE    add         esp,0FFFFFFF0
 005AA101    call        @ClassCreate
 005AA106    mov         esi,ecx
 005AA108    mov         ebx,edx
 005AA10A    mov         edi,eax
 005AA10C    mov         dx,5261
 005AA110    mov         eax,edi
 005AA112    call        TYgPacket.WriteID
 005AA117    mov         edx,dword ptr
```

---

## Opcode 0x5264 (21092): `MsgGameSchoolInfoNtf` [学校情報通知]

* **Original Japanese DMS Title**: `学校情報通知`
* **Raw DMS Script**: [`21092.dms`](../dms/21092.dms)
* **Legacy Delphi Class**: `TMsgGameSchoolInfoNtf` (Address: `005AA16C`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21092.dms
function parse(packet){
	packet.setTitle("学校情報通知");
	packet.readUInt32("学校ID");
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameSchoolInfoNtf.Create(SchoolID:Integer)
005AA16C    push        ebx
 005AA16D    push        esi
 005AA16E    push        edi
 005AA16F    test        dl,dl
>005AA171    je          005AA17B
 005AA173    add         esp,0FFFFFFF0
 005AA176    call        @ClassCreate
 005AA17B    mov         esi,ecx
 005AA17D    mov         ebx,edx
 005AA17F    mov         edi,eax
 005AA181    mov         dx,5264
 005AA185    mov         eax,edi
 005AA187    call        TYgPacket.WriteID
 005AA18C    mov         edx,esi
 005AA18E    mov         eax,edi
 005AA190    call        TYgPacket.WriteInt32
 005AA195    mov         eax,edi
 005AA197    test  
```

---

## Opcode 0x526D (21101): `MsgGameEnterHairShopNtf` [ヘアショップ参加通知]

* **Original Japanese DMS Title**: `ヘアショップ参加通知`
* **Raw DMS Script**: [`21101.dms`](../dms/21101.dms)
* **Legacy Delphi Class**: `TMsgGameEnterHairShopNtf` (Address: `005AA35C`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21101.dms
import struct.*;
function parse(packet){
	with(packet){
		setTitle("ヘアショップ参加通知");
		var cnt = readWord("vecHairInfoCount");
		hair_info(packet, cnt);
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameEnterHairShopNtf.Create(Sex:Integer; Dresser:THairDresser)
005AA35C    push        ebp
 005AA35D    mov         ebp,esp
 005AA35F    push        ecx
 005AA360    push        ebx
 005AA361    push        esi
 005AA362    push        edi
 005AA363    test        dl,dl
>005AA365    je          005AA36F
 005AA367    add         esp,0FFFFFFF0
 005AA36A    call        @ClassCreate
 005AA36F    mov         ebx,ecx
 005AA371    mov         byte ptr [ebp-1],dl
 005AA374    mov         esi,eax
 005AA376    mov         dx,526D
 005AA37A    mov         eax,esi
 005AA37C    call        TYgPacket.WriteID
 005AA381    mov         dx,33
 005AA385    mov         eax,e
```

---

## Opcode 0x5271 (21105): `MsgUnknown_0x5271` [髪型変更返答]

* **Original Japanese DMS Title**: `髪型変更返答`
* **Raw DMS Script**: [`21105.dms`](../dms/21105.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x5271` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21105.dms
function parse(packet){
	with(packet){
		setTitle("髪型変更返答");
		readInt32("戻り値");
		readInt32("idChar");
		readInt32("gender");
		readInt32("idHair");
		readInt64("reduceTaff");
	}
}
```

---

## Opcode 0x5272 (21106): `MsgGameLeaveHairShopNtf` [ヘアショップ退店通知]

* **Original Japanese DMS Title**: `ヘアショップ退店通知`
* **Raw DMS Script**: [`21106.dms`](../dms/21106.dms)
* **Legacy Delphi Class**: `TMsgGameChangeHairAns` (Address: `005AA45C`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21106.dms
function parse(packet){
	with(packet){
		setTitle("ヘアショップ退店通知");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameChangeHairAns.Create(?:?; ?:?; ?:?)
005AA45C    push        ebx
 005AA45D    push        esi
 005AA45E    test        dl,dl
>005AA460    je          005AA46A
 005AA462    add         esp,0FFFFFFF0
 005AA465    call        @ClassCreate
 005AA46A    mov         ebx,edx
 005AA46C    mov         esi,eax
 005AA46E    mov         dx,5272
 005AA472    mov         eax,esi
 005AA474    call        TYgPacket.WriteID
 005AA479    mov         eax,esi
 005AA47B    test        bl,bl
>005AA47D    je          005AA48E
 005AA47F    call        @AfterConstruction
 005AA484    pop         dword ptr fs:[0]
 005AA48B    add         esp,0C
 005AA48E 
```

---

## Opcode 0x5273 (21107): `MsgGameWeaponFrameReq` [武器フレーム情報要求]

* **Original Japanese DMS Title**: `武器フレーム情報要求`
* **Raw DMS Script**: [`21107.dms`](../dms/21107.dms)
* **Legacy Delphi Class**: `TMsgGameWeaponFrameInfoReq` (Address: `005AA494`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21107.dms
function parse(packet){
	with(packet){
		setTitle("武器フレーム情報要求");
		readInt32("typeItem+typeNum");
		readWord("dim1Index");
		readWord("dim2Index");
		readInt32("idItem");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameWeaponFrameInfoReq.Create(Item:TEquippableItem)
005AA494    push        ebx
 005AA495    push        esi
 005AA496    push        edi
 005AA497    test        dl,dl
>005AA499    je          005AA4A3
 005AA49B    add         esp,0FFFFFFF0
 005AA49E    call        @ClassCreate
 005AA4A3    mov         esi,ecx
 005AA4A5    mov         ebx,edx
 005AA4A7    mov         edi,eax
 005AA4A9    mov         dx,5273
 005AA4AD    mov         eax,edi
 005AA4AF    call        TYgPacket.WriteID
 005AA4B4    mov         eax,esi
 005AA4B6    call        005BD978
 005AA4BB    test        al,al
>005AA4BD    je          005AA4D4
 005AA4BF    mov         eax,dwo
```

---

## Opcode 0x5274 (21108): `MsgGameWeaponFrameAns` [武器フレーム情報返答]

* **Original Japanese DMS Title**: `武器フレーム情報返答`
* **Raw DMS Script**: [`21108.dms`](../dms/21108.dms)
* **Legacy Delphi Class**: `TMsgGameWeaponFrameAns` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21108.dms
function parse(packet){
	with(packet){
		setTitle("武器フレーム情報返答");
		readInt32("武器ID");
		readWord("武器SN-ID-low");
		readWord("武器SN-ID-high");
		readInt32("武器SN-Type");
		var cnt = readWord("vecAttackFrameCount");
		for(var i = 1; i <= cnt; i++){
			readInt32("  AttackFrameID" + i.toString());
			readInt32("  AttackFrame???" + i.toString());
		}
		var cnt = readWord("vecSkillFrameCount");
		for(var i = 1; i <= cnt; i++){
			readInt32("  SkillFrameID" + i.toString());
			readInt32("  SkillFrame???" + i.toString());
		}
	}
}
```

---

## Opcode 0x5275 (21109): `MsgGameHuntCharLvUpNtf` [戦闘フィールドキャラレベルアップ通知]

* **Original Japanese DMS Title**: `戦闘フィールドキャラレベルアップ通知`
* **Raw DMS Script**: [`21109.dms`](../dms/21109.dms)
* **Legacy Delphi Class**: `TMsgGameHuntCharLvUpNtf` (Address: `005AA50C`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21109.dms
function parse(packet){
	with(packet){
		setTitle("戦闘フィールドキャラレベルアップ通知");
		readInt32("level");
		readInt32("exp");
		readInt32("expMax");
		readInt32("skillPoint");
		readInt32("idChar");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameHuntCharLvUpNtf.Create(Chara:TChara)
005AA50C    push        ebx
 005AA50D    push        esi
 005AA50E    push        edi
 005AA50F    test        dl,dl
>005AA511    je          005AA51B
 005AA513    add         esp,0FFFFFFF0
 005AA516    call        @ClassCreate
 005AA51B    mov         esi,ecx
 005AA51D    mov         ebx,edx
 005AA51F    mov         edi,eax
 005AA521    mov         dx,5275
 005AA525    mov         eax,edi
 005AA527    call        TYgPacket.WriteID
 005AA52C    mov         edx,dword ptr [esi+50];TChara.FLevel:Integer
 005AA52F    mov         eax,edi
 005AA531    call        TYgPacket.WriteInt32
 005AA536    mo
```

---

## Opcode 0x5276 (21110): `MsgGameHuntMonDeadNtf` [ハントモンスター死亡通知]

* **Original Japanese DMS Title**: `ハントモンスター死亡通知`
* **Raw DMS Script**: [`21110.dms`](../dms/21110.dms)
* **Legacy Delphi Class**: `TMsgGameHuntMonDeadNtf` (Address: `005AA588`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21110.dms
function parse(packet){
	with(packet){
		setTitle("ハントモンスター死亡通知");
		readInt32("idMonster");
		readWord("posX");
		readWord("posY");
		readInt32("idAtkChar");
		readInt32("expUp");
		readInt32("exp");
		var cnt = readWord("vecItemSilCount");
		for(var i = 1; i <= cnt; i++){
			readInt32("  ItemSil" + i.toString());
		}
		var cnt = readWord("vecItemCount");
		for(var i = 1; i <= cnt; i++){
			switch(readInt32("  typeItem+typeNum" + i.toString()) & 0xFF000000){
				case 0x02000000: //BeItem
					readWord("  dim1Index" + i.toString());
					readWord("  dim2Index" + i.toString());
					readInt32("  idItem" + i.toString());
					break;
				case 0x03000000: //CoItem
				case 0x05000000: //EnItem
					readInt32("  count" + i.toString());
					readInt32("  (invalid)" + i.toString());
					break;
			}
		}
		readInt32("戻り値");
		readInt32("type");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameHuntMonDeadNtf.Create(Monster:TMonster; Counts:TArray<System.Integer>; Items:TArray<UYgItem.TYgItem>; Exp:Integer; Chara:TChara)
005AA588    push        ebp
 005AA589    mov         ebp,esp
 005AA58B    add         esp,0FFFFFFF8
 005AA58E    push        ebx
 005AA58F    push        esi
 005AA590    push        edi
 005AA591    test        dl,dl
>005AA593    je          005AA59D
 005AA595    add         esp,0FFFFFFF0
 005AA598    call        @ClassCreate
 005AA59D    mov         esi,ecx
 005AA59F    mov         byte ptr [ebp-1],dl
 005AA5A2    mov         ebx,eax
 005AA5A4    mov         edi,dword ptr [ebp+0C]
 005AA5A7    mov         dx,5276
 005AA5AB    mov         eax,ebx
 005AA5AD    call        TYgPacket.WriteID
 00
```

---

## Opcode 0x5277 (21111): `MsgGameHuntCharExpUpNtf` [キャラEXPアップ通知]

* **Original Japanese DMS Title**: `キャラEXPアップ通知`
* **Raw DMS Script**: [`21111.dms`](../dms/21111.dms)
* **Legacy Delphi Class**: `TMsgGameHuntCharExpUpNtf` (Address: `005AA7BC`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/21111.dms
function parse(packet){
	with(packet){
		setTitle("キャラEXPアップ通知");
		readInt32("EXP");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameHuntCharExpUpNtf.Create(Chara:TChara)
005AA7BC    push        ebx
 005AA7BD    push        esi
 005AA7BE    push        edi
 005AA7BF    test        dl,dl
>005AA7C1    je          005AA7CB
 005AA7C3    add         esp,0FFFFFFF0
 005AA7C6    call        @ClassCreate
 005AA7CB    mov         esi,ecx
 005AA7CD    mov         ebx,edx
 005AA7CF    mov         edi,eax
 005AA7D1    mov         dx,5277
 005AA7D5    mov         eax,edi
 005AA7D7    call        TYgPacket.WriteID
 005AA7DC    mov         edx,dword ptr [esi+260];TChara.Exp:Integer
 005AA7E2    mov         eax,edi
 005AA7E4    call        TYgPacket.WriteInt32
 005AA7E9    mov 
```

---
