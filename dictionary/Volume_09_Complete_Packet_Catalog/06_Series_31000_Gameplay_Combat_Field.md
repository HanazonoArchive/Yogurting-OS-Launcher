# Volume 9: Series 31000: Field Gameplay, Movement, Combat, Spawns & Inventory

This chapter provides the complete, byte-for-byte specifications for all 125 opcodes in this range.

---

## Opcode 0x77A4 (30628): `MsgUnknown_0x77A4` [チャットサーバー同好会情報要求]

* **Original Japanese DMS Title**: `チャットサーバー同好会情報要求`
* **Raw DMS Script**: [`30628.dms`](../dms/30628.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x77A4` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30628.dms
function parse(packet){
	packet.setTitle("チャットサーバー同好会情報要求");
	packet.readUInt32("キャラID");
}
```

---

## Opcode 0x77A5 (30629): `MsgUnknown_0x77A5` [チャットサーバー同好会情報返答]

* **Original Japanese DMS Title**: `チャットサーバー同好会情報返答`
* **Raw DMS Script**: [`30629.dms`](../dms/30629.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x77A5` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30629.dms
function parse(packet){
	packet.setTitle("チャットサーバー同好会情報返答");
	packet.readUInt32("キャラID");
	packet.readWord();
	packet.readWord();
	packet.readUInt32();
	packet.readUInt32();
	packet.readUInt32("同好会ID？");
	packet.readWStr(0x1C, "同好会名");
	packet.readUInt32("設立日");
	packet.readUInt32();
	packet.readUInt32();
	packet.readUInt32();
	packet.readUInt32();
	packet.readUInt32();
	packet.readUInt32("会長キャラID");
	packet.readInt32();
	packet.readInt32();
	packet.readUInt32();
	var cnt = packet.readWord("メンバー数");
	for(var i = 1; i <= cnt; i++){
		packet.readWord();
		packet.readWord();
		packet.readUInt32();
		packet.readUInt32();
		packet.readUInt32();
		packet.readUInt32("加入日");
		packet.readUInt32();
		packet.readUInt32("メンバーキャラID");
		packet.readUInt32("電話番号");
		packet.readWStr(0x44, "メンバー名");
		packet.readUInt32();
		packet.readUInt32();
		packet.readUInt32();
		packet.readUInt32();
	}
}
```

---

## Opcode 0x7919 (31001): `MsgGameAttackReq` [攻撃要求]

* **Original Japanese DMS Title**: `攻撃要求`
* **Raw DMS Script**: [`31001.dms`](../dms/31001.dms)
* **Legacy Delphi Class**: `TMsgGameAttackReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31001.dms
function parse(packet){
	with(packet){
		setTitle("攻撃要求");
		readInt32("idChar");
		readByte("idxAtkSkill");
		readInt32("targetMainType");
		readInt32("targetMainId");
		readInt32("targetMainPosX");
		readInt32("targetMainPosY");
		readByte("cntTarget");
		var cnt = readWord("TargetsCount");
		for(var i = 1; i <= cnt; i++){
			if(i != cnt){
				readInt32("├┬target"+i+"Type");
				readInt32("│├target"+i+"Id");
				readInt32("│├target"+i+"PosX");
				readInt32("│└target"+i+"PosY");
			}else{
				readInt32("└┬target"+i+"Type");
				readInt32("　 ├target"+i+"Id");
				readInt32("　 ├target"+i+"PosX");
				readInt32("　 └target"+i+"PosY");
			}
		}
		readInt32("time");
	}
}
```

---

## Opcode 0x791A (31002): `MsgGameAttackAns` [攻撃返答]

* **Original Japanese DMS Title**: `攻撃返答`
* **Raw DMS Script**: [`31002.dms`](../dms/31002.dms)
* **Legacy Delphi Class**: `TMsgGameAttackAns` (Address: `005ABF00`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31002.dms
function parse(packet){
	with(packet){
		setTitle("攻撃返答");
		readInt32("キャラID");
		readInt32("戻り値");
		readInt32("メインターゲットタイプ");
		readInt32("メインターゲットID");
		readInt32("メインターゲットX");
		readInt32("メインターゲットY");
		readByte("cntTarget");
		var cnt = readWord("TargetsCount");
		for(var i = 1; i <= cnt; i++){
			readInt32("  ターゲットタイプ" + i.toString());
			readInt32("  ターゲットID" + i.toString());
			readInt32("  ターゲットX" + i.toString());
			readInt32("  ターゲットY" + i.toString());
		}
		var cnt = readWord("DamagesCount");
		for(var i = 1; i <= cnt; i++){
			readInt32("  ダメージ" + i.toString());
		}
		var cnt = readWord("typeHitsCount");
		for(var i = 1; i <= cnt; i++){
			readByte("  typeHit" + i.toString());
		}
		readInt32("スキルID");
		readInt32("moneyDelta");
		readInt32("bCritical");
		readBinary(1, "(padding)");
		readInt32("numCombo");
		readInt32("cateWeapon");
		readInt32("addDexExp");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameAttackAns.Create(Chara:TChara; DexExp:Integer; Targets:TAttackTargetDynArray; MainTarget:TAttackTarget; SkillID:Integer)
005ABF00    push        ebp
 005ABF01    mov         ebp,esp
 005ABF03    add         esp,0FFFFFFF4
 005ABF06    push        ebx
 005ABF07    push        esi
 005ABF08    push        edi
 005ABF09    test        dl,dl
>005ABF0B    je          005ABF15
 005ABF0D    add         esp,0FFFFFFF0
 005ABF10    call        @ClassCreate
 005ABF15    mov         dword ptr [ebp-8],ecx
 005ABF18    mov         byte ptr [ebp-1],dl
 005ABF1B    mov         ebx,eax
 005ABF1D    mov         edi,dword ptr [ebp+0C]
 005ABF20    mov         esi,dword ptr [ebp+10]
 005ABF23    mov         dx,791A
 005ABF27    mov 
```

---

## Opcode 0x791B (31003): `MsgGameDieCharNtf` [キャラ死亡通知]

* **Original Japanese DMS Title**: `キャラ死亡通知`
* **Raw DMS Script**: [`31003.dms`](../dms/31003.dms)
* **Legacy Delphi Class**: `TMsgGameDieCharNtf` (Address: `005AC108`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31003.dms
function parse(packet){
	with(packet){
		setTitle("キャラ死亡通知");
		readInt32("idChar");
		readWord("posX");
		readWord("posY");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameDieCharNtf.Create(Chara:TChara)
005AC108    push        ebx
 005AC109    push        esi
 005AC10A    push        edi
 005AC10B    test        dl,dl
>005AC10D    je          005AC117
 005AC10F    add         esp,0FFFFFFF0
 005AC112    call        @ClassCreate
 005AC117    mov         esi,ecx
 005AC119    mov         ebx,edx
 005AC11B    mov         edi,eax
 005AC11D    mov         dx,791B
 005AC121    mov         eax,edi
 005AC123    call        TYgPacket.WriteID
 005AC128    mov         edx,dword ptr [esi+0E0];TChara.ID:Integer
 005AC12E    mov         eax,edi
 005AC130    call        TYgPacket.WriteInt32
 005AC135    mov  
```

---

## Opcode 0x791C (31004): `MsgGameChargePointUpdateNtf` [チャージポイント更新通知]

* **Original Japanese DMS Title**: `チャージポイント更新通知`
* **Raw DMS Script**: [`31004.dms`](../dms/31004.dms)
* **Legacy Delphi Class**: `TMsgGameUpdateChargePointNtf` (Address: `005AC15C`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31004.dms
function parse(packet){
	with(packet){
		setTitle("チャージポイント更新通知");
		readByte("チャージポイント");
		readBinary(3, "(paddding)");
		readInt32("uguuToNextUpdate");
		readInt32("uguuCurrent");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameUpdateChargePointNtf.Create(Chara:TChara)
005AC15C    push        ebx
 005AC15D    push        esi
 005AC15E    push        edi
 005AC15F    test        dl,dl
>005AC161    je          005AC16B
 005AC163    add         esp,0FFFFFFF0
 005AC166    call        @ClassCreate
 005AC16B    mov         esi,ecx
 005AC16D    mov         ebx,edx
 005AC16F    mov         edi,eax
 005AC171    mov         dx,791C
 005AC175    mov         eax,edi
 005AC177    call        TYgPacket.WriteID
 005AC17C    movzx       edx,byte ptr [esi+25C];TChara.ChargePoint:byte
 005AC183    mov         eax,edi
 005AC185    call        TYgPacket.WriteByte
 005AC18A    m
```

---

## Opcode 0x791E (31006): `MsgGameMoveStopReq` [停止通知]

* **Original Japanese DMS Title**: `停止通知`
* **Raw DMS Script**: [`31006.dms`](../dms/31006.dms)
* **Legacy Delphi Class**: `TMsgGameMoveStopReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31006.dms
function parse(packet){
	packet.setTitle("停止通知");
	packet.readInt32("キャラID");
	packet.readWord("X");
	packet.readWord("Y");
}
```

---

## Opcode 0x791F (31007): `MsgUnknown_0x791F` [正規位置通知]

* **Original Japanese DMS Title**: `正規位置通知`
* **Raw DMS Script**: [`31007.dms`](../dms/31007.dms)
* **Legacy Delphi Class**: `TMsgGamePosCorrectNtf` (Address: `005AC1D0`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31007.dms
function parse(packet){
	with(packet){
		setTitle("正規位置通知");
		readInt32("idChar");
		readInt32("typeCorrect");
		readWord("posX");
		readWord("posY");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGamePosCorrectNtf.Create(Chara:TChara)
005AC1D0    push        ebx
 005AC1D1    push        esi
 005AC1D2    push        edi
 005AC1D3    test        dl,dl
>005AC1D5    je          005AC1DF
 005AC1D7    add         esp,0FFFFFFF0
 005AC1DA    call        @ClassCreate
 005AC1DF    mov         esi,ecx
 005AC1E1    mov         ebx,edx
 005AC1E3    mov         edi,eax
 005AC1E5    mov         dx,791F
 005AC1E9    mov         eax,edi
 005AC1EB    call        TYgPacket.WriteID
 005AC1F0    mov         edx,dword ptr [esi+0E0];TChara.ID:Integer
 005AC1F6    mov         eax,edi
 005AC1F8    call        TYgPacket.WriteInt32
 005AC1FD    mov  
```

---

## Opcode 0x7921 (31009): `MsgGamePosSyncReq` [キャラ位置通知]

* **Original Japanese DMS Title**: `キャラ位置通知`
* **Raw DMS Script**: [`31009.dms`](../dms/31009.dms)
* **Legacy Delphi Class**: `TMsgGamePosSyncReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31009.dms
function parse(packet){
	packet.setTitle("キャラ位置通知");
	packet.readWord("X");
	packet.readWord("Y");
}
```

---

## Opcode 0x7922 (31010): `MsgGameJumpReq` [AOI=Area Of Interest(有効領域)ブロック通知]

* **Original Japanese DMS Title**: `AOI=Area Of Interest(有効領域)ブロック通知`
* **Raw DMS Script**: [`31010.dms`](../dms/31010.dms)
* **Legacy Delphi Class**: `TMsgGameJumpReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31010.dms
function parse(packet){
	with(packet){
		setTitle("AOI=Area Of Interest(有効領域)ブロック通知");
		readWord("X");
		readWord("Y");
	}
}
```

---

## Opcode 0x7923 (31011): `MsgGameSkillActiveReq` [スキル発動要求]

* **Original Japanese DMS Title**: `スキル発動要求`
* **Raw DMS Script**: [`31011.dms`](../dms/31011.dms)
* **Legacy Delphi Class**: `TMsgGameSkillActiveReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31011.dms
function parse(packet){
	with(packet){
		setTitle("スキル発動要求");
		readInt32("idSkill");
		readInt32("seqNum");
		readInt32("targetMainType");
		readInt32("targetMainID");
		readInt32("targetMainX");
		readInt32("targetMainY");
		readByte("cntTarget");
		var cnt = readWord("cnttargets");
		for(var i = 1; i <= cnt; i++){
			readInt32("  targetType" + i.toString());
			readInt32("  targetID" + i.toString());
			readInt32("  targetX" + i.toString());
			readInt32("  targetY" + i.toString());
		}
		readInt32("time");
	}
}
```

---

## Opcode 0x7924 (31012): `MsgGameSkillActiveAns` [スキル発動返答]

* **Original Japanese DMS Title**: `スキル発動返答`
* **Raw DMS Script**: [`31012.dms`](../dms/31012.dms)
* **Legacy Delphi Class**: `TMsgGameSkillActiveAns` (Address: `005AC230`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31012.dms
function parse(packet){
	with(packet){
		setTitle("スキル発動返答");
		readInt32("idChar");
		readInt32("idSkill");
		readInt32("seqNum");
		readInt32("targetMainType");
		readInt32("targetMainID");
		readInt32("targetMainX");
		readInt32("targetMainY");
		readByte("cntTarget");
		var cnt = readWord("targetsCount");
		for(var i = 1; i <= cnt; i++){
			readInt32("  targetType" + i.toString());
			readInt32("  targetID" + i.toString());
			readInt32("  targetX" + i.toString());
			readInt32("  targetY" + i.toString());
		}
		var cnt = readWord("damagesCount");
		for(var i = 1; i <= cnt; i++){
			readInt32("  damage" + i.toString());
		}
		var cnt = readWord("typeHitsCount");
		for(var i = 1; i <= cnt; i++){
			readByte("  typeHit" + i.toString());
		}
		readInt32("bSkill");
		readInt32("cateWeapon");
		readInt32("addDexExp");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameSkillActiveAns.Create(Chara:TChara; DexExp:Integer; Targets:TAttackTargetDynArray; MainTarget:TAttackTarget; SeqNum:Integer; SkillID:Integer)
005AC230    push        ebp
 005AC231    mov         ebp,esp
 005AC233    add         esp,0FFFFFFF4
 005AC236    push        ebx
 005AC237    push        esi
 005AC238    push        edi
 005AC239    test        dl,dl
>005AC23B    je          005AC245
 005AC23D    add         esp,0FFFFFFF0
 005AC240    call        @ClassCreate
 005AC245    mov         dword ptr [ebp-8],ecx
 005AC248    mov         byte ptr [ebp-1],dl
 005AC24B    mov         ebx,eax
 005AC24D    mov         edi,dword ptr [ebp+0C]
 005AC250    mov         esi,dword ptr [ebp+10]
 005AC253    mov         dx,7924
 005AC257    mov 
```

---

## Opcode 0x7925 (31013): `MsgGameSkillCastReq` [スキル準備通知]

* **Original Japanese DMS Title**: `スキル準備通知`
* **Raw DMS Script**: [`31013.dms`](../dms/31013.dms)
* **Legacy Delphi Class**: `TMsgGameSkillCastReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31013.dms
function parse(packet){
	with(packet){
		setTitle("スキル準備通知");
		readInt32("idChar");
		readInt32("idSkill");
		readInt32("seqNum");
		readInt32("targetMainType");
		readInt32("targetMainID");
		readInt32("targetMainX");
		readInt32("targetMainY");
		readInt32("time");
		readInt32("bSkill");
	}
}
```

---

## Opcode 0x7928 (31016): `MsgGameSkillHitReq` [消費アイテム使用要求]

* **Original Japanese DMS Title**: `消費アイテム使用要求`
* **Raw DMS Script**: [`31016.dms`](../dms/31016.dms)
* **Legacy Delphi Class**: `TMsgGameSkillHitReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31016.dms
function parse(packet){
	with(packet){
		setTitle("消費アイテム使用要求");
		readInt32("CoItemタイプ");
	}
}
```

---

## Opcode 0x7929 (31017): `MsgGameUseCoItemAns` [COITEM使用返答]

* **Original Japanese DMS Title**: `COITEM使用返答`
* **Raw DMS Script**: [`31017.dms`](../dms/31017.dms)
* **Legacy Delphi Class**: `TMsgGameUseCoItemAns` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31017.dms
function parse(packet){
	with(packet){
		setTitle("COITEM使用返答");
		readInt32("キャラID");
		readInt32("戻り値");
		readInt32("CoItemタイプ");
		readInt32("数");
	}
}
```

---

## Opcode 0x792A (31018): `MsgUnknown_0x792A` [交換失敗通知]

* **Original Japanese DMS Title**: `交換失敗通知`
* **Raw DMS Script**: [`31018.dms`](../dms/31018.dms)
* **Legacy Delphi Class**: `TMsgGameUseCoItemAns` (Address: `005AC4BC`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31018.dms
function parse(packet){
	with(packet){
		setTitle("交換失敗通知");
		readInt32("reason");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameUseCoItemAns.Create(?:?; ?:?; ?:?)
005AC4BC    push        ebx
 005AC4BD    push        esi
 005AC4BE    push        edi
 005AC4BF    test        dl,dl
>005AC4C1    je          005AC4CB
 005AC4C3    add         esp,0FFFFFFF0
 005AC4C6    call        @ClassCreate
 005AC4CB    mov         esi,ecx
 005AC4CD    mov         ebx,edx
 005AC4CF    mov         edi,eax
 005AC4D1    mov         dx,792A
 005AC4D5    mov         eax,edi
 005AC4D7    call        TYgPacket.WriteID
 005AC4DC    mov         edx,esi
 005AC4DE    mov         eax,edi
 005AC4E0    call        TYgPacket.WriteInt32
 005AC4E5    mov         eax,edi
 005AC4E7    test  
```

---

## Opcode 0x792B (31019): `MsgGameTradeProposeReq` [交換申請通知]

* **Original Japanese DMS Title**: `交換申請通知`
* **Raw DMS Script**: [`31019.dms`](../dms/31019.dms)
* **Legacy Delphi Class**: `TMsgGameTradeProposeReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31019.dms
function parse(packet){
	with(packet){
		setTitle("交換申請通知");
		readInt32("idChar");
	}
}
```

---

## Opcode 0x792C (31020): `MsgUnknown_0x792C` [交換応答要求]

* **Original Japanese DMS Title**: `交換応答要求`
* **Raw DMS Script**: [`31020.dms`](../dms/31020.dms)
* **Legacy Delphi Class**: `TMsgGameTradeResponseReq` (Address: `005AC500`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31020.dms
function parse(packet){
	with(packet){
		setTitle("交換応答要求");
		readWStr(0x1A, "name");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameTradeResponseReq.Create(Chara:TChara)
005AC500    push        ebx
 005AC501    push        esi
 005AC502    push        edi
 005AC503    test        dl,dl
>005AC505    je          005AC50F
 005AC507    add         esp,0FFFFFFF0
 005AC50A    call        @ClassCreate
 005AC50F    mov         esi,ecx
 005AC511    mov         ebx,edx
 005AC513    mov         edi,eax
 005AC515    mov         dx,792C
 005AC519    mov         eax,edi
 005AC51B    call        TYgPacket.WriteID
 005AC520    mov         ecx,1A
 005AC525    mov         edx,dword ptr [esi+0E4];TChara.Name:string
 005AC52B    mov         eax,edi
 005AC52D    call        TYgPac
```

---

## Opcode 0x792D (31021): `MsgGameTradeAcceptReq` [交換応答返答]

* **Original Japanese DMS Title**: `交換応答返答`
* **Raw DMS Script**: [`31021.dms`](../dms/31021.dms)
* **Legacy Delphi Class**: `TMsgGameTradeAcceptReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31021.dms
function parse(packet){
	with(packet){
		setTitle("交換応答返答");
		readInt32("bAttend");
	}
}
```

---

## Opcode 0x792E (31022): `MsgUnknown_0x792E` [交換相手了承通知]

* **Original Japanese DMS Title**: `交換相手了承通知`
* **Raw DMS Script**: [`31022.dms`](../dms/31022.dms)
* **Legacy Delphi Class**: `TMsgGameTradeOtherSideAttendNtf` (Address: `005AC550`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31022.dms
function parse(packet){
	with(packet){
		setTitle("交換相手了承通知");
		readInt32("idCharOther");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameTradeOtherSideAttendNtf.Create(Chara:TChara)
005AC550    push        ebx
 005AC551    push        esi
 005AC552    push        edi
 005AC553    test        dl,dl
>005AC555    je          005AC55F
 005AC557    add         esp,0FFFFFFF0
 005AC55A    call        @ClassCreate
 005AC55F    mov         esi,ecx
 005AC561    mov         ebx,edx
 005AC563    mov         edi,eax
 005AC565    mov         dx,792E
 005AC569    mov         eax,edi
 005AC56B    call        TYgPacket.WriteID
 005AC570    mov         edx,dword ptr [esi+0E0];TChara.ID:Integer
 005AC576    mov         eax,edi
 005AC578    call        TYgPacket.WriteInt32
 005AC57D    mov  
```

---

## Opcode 0x792F (31023): `MsgUnknown_0x792F` [交換相手アイテム情報通知]

* **Original Japanese DMS Title**: `交換相手アイテム情報通知`
* **Raw DMS Script**: [`31023.dms`](../dms/31023.dms)
* **Legacy Delphi Class**: `TMsgGameTradeOtherSideBasketInfoNtf` (Address: `005AC598`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31023.dms
function parse(packet){
	with(packet){
		setTitle("交換相手アイテム情報通知");
		for(var i = 1; i <= 5; i++){
			readInt32("  typeItem+typeNum" + i.toString());
			readWord("  dim1index or count" + i.toString());
			readWord("  dim2index or (invalid)" + i.toString());
			readInt32("  idItem or (invalid)" + i.toString());
			for(var j = 0; j < 5; j++)
				readInt32("  reinforceslot[" + j.toString() + "]" + i.toString());
		}
		readInt64("money");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameTradeOtherSideBasketInfoNtf.Create(Chara:TChara)
005AC598    push        ebx
 005AC599    push        esi
 005AC59A    push        edi
 005AC59B    test        dl,dl
>005AC59D    je          005AC5A7
 005AC59F    add         esp,0FFFFFFF0
 005AC5A2    call        @ClassCreate
 005AC5A7    mov         esi,ecx
 005AC5A9    mov         ebx,edx
 005AC5AB    mov         edi,eax
 005AC5AD    mov         dx,792F
 005AC5B1    mov         eax,edi
 005AC5B3    call        TYgPacket.WriteID
 005AC5B8    lea         edx,[esi+340];TChara.TradeBasket:?
 005AC5BE    mov         ecx,0A0
 005AC5C3    mov         eax,edi
 005AC5C5    call        TStream.Write
```

---

## Opcode 0x7930 (31024): `MsgGameTradeAddItemReq` [交換アイテム追加通知]

* **Original Japanese DMS Title**: `交換アイテム追加通知`
* **Raw DMS Script**: [`31024.dms`](../dms/31024.dms)
* **Legacy Delphi Class**: `TMsgGameTradeAddItemReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31024.dms
function parse(packet){
	with(packet){
		setTitle("交換アイテム追加通知");
		readInt32("typeItem+typeNum");
		readWord("dim1Index or count");
		readWord("dim2Index or (invalid)");
		readInt32("idItem or (invalid)");
		for(var i = 0; i < 5; i++)
			readInt32("reinforceslot[" + i.toString() + "]");
	}
}
```

---

## Opcode 0x7931 (31025): `MsgGameTradeCancelReq` [交換キャンセル通知]

* **Original Japanese DMS Title**: `交換キャンセル通知`
* **Raw DMS Script**: [`31025.dms`](../dms/31025.dms)
* **Legacy Delphi Class**: `TMsgGameTradeCancelReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31025.dms
function parse(packet){
	with(packet){
		setTitle("交換キャンセル通知");
	}
}
```

---

## Opcode 0x7932 (31026): `MsgGameTradeLockReq` [交換OK通知]

* **Original Japanese DMS Title**: `交換OK通知`
* **Raw DMS Script**: [`31026.dms`](../dms/31026.dms)
* **Legacy Delphi Class**: `TMsgGameTradeLockReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31026.dms
function parse(packet){
	with(packet){
		setTitle("交換OK通知");
	}
}
```

---

## Opcode 0x7933 (31027): `MsgGameTradeConfirmReq` [交換終了通知]

* **Original Japanese DMS Title**: `交換終了通知`
* **Raw DMS Script**: [`31027.dms`](../dms/31027.dms)
* **Legacy Delphi Class**: `TMsgGameTradeConfirmReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31027.dms
function parse(packet){
	with(packet){
		setTitle("交換終了通知");
	}
}
```

---

## Opcode 0x7934 (31028): `MsgUnknown_0x7934` [交換完了通知]

* **Original Japanese DMS Title**: `交換完了通知`
* **Raw DMS Script**: [`31028.dms`](../dms/31028.dms)
* **Legacy Delphi Class**: `TMsgGameTradeCompleteNtf` (Address: `005AC630`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31028.dms
function parse(packet){
	with(packet){
		setTitle("交換完了通知");
		for(var i = 1; i <= 5; i++){
			readInt32("  [out]typeItem+typeNum" + i.toString());
			readWord("  [out]dim1index or count" + i.toString());
			readWord("  [out]dim2index or (invalid)" + i.toString());
			readInt32("  [out]idItem or (invalid)" + i.toString());
			for(var j = 0; j < 5; j++)
				readInt32("  [out]reinforceslot[" + j.toString() + "]" + i.toString());
		}
		readInt64("[out]money");
		for(var i = 1; i <= 5; i++){
			readInt32("  [in]typeItem+typeNum" + i.toString());
			readWord("  [in]dim1index or count" + i.toString());
			readWord("  [in]dim2index or (invalid)" + i.toString());
			readInt32("  [in]idItem or (invalid)" + i.toString());
			for(var j = 0; j < 5; j++)
				readInt32("  [in]reinforceslot[" + j.toString() + "]" + i.toString());
		}
		readInt64("[in]money");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameTradeCompleteNtf.Create(Chara:TChara)
005AC630    push        ebp
 005AC631    mov         ebp,esp
 005AC633    push        ecx
 005AC634    push        ebx
 005AC635    push        esi
 005AC636    push        edi
 005AC637    test        dl,dl
>005AC639    je          005AC643
 005AC63B    add         esp,0FFFFFFF0
 005AC63E    call        @ClassCreate
 005AC643    mov         esi,ecx
 005AC645    mov         byte ptr [ebp-1],dl
 005AC648    mov         ebx,eax
 005AC64A    mov         edi,dword ptr [esi+33C];TChara.TradeTarget:TChara
 005AC650    mov         dx,7934
 005AC654    mov         eax,ebx
 005AC656    call        TYgP
```

---

## Opcode 0x7935 (31029): `MsgGameInventoryMoveReq` [NPCからアイテム購入要求]

* **Original Japanese DMS Title**: `NPCからアイテム購入要求`
* **Raw DMS Script**: [`31029.dms`](../dms/31029.dms)
* **Legacy Delphi Class**: `TMsgGameInventoryMoveReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31029.dms
function parse(packet){
	with(packet){
		setTitle("NPCからアイテム購入要求");
		readInt32("idNpc");
		for(var i = 1; i <= 20; i++){
			readInt32("  typeItem" + i.toString());
			readInt32("  count" + i.toString());
			readInt32("  unknown" + i.toString());
		}
	}
}
```

---

## Opcode 0x7936 (31030): `MsgUnknown_0x7936` [NPCからアイテム購入返答]

* **Original Japanese DMS Title**: `NPCからアイテム購入返答`
* **Raw DMS Script**: [`31030.dms`](../dms/31030.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x7936` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31030.dms
function parse(packet){
	with(packet){
		setTitle("NPCからアイテム購入返答");
		readInt32("戻り値");
		for(var i = 1; i <= 20; i++){
			readInt32(" typeItem" + i.toString());
			readWord("  idItemDim1Index" + i.toString());
			readWord("  idItemDim2Index" + i.toString());
			readInt32("  idItem[Dim1][Dim2]" + i.toString());
		}
		readInt64("totalPrice");
	}
}
```

---

## Opcode 0x7937 (31031): `MsgGameItemDropReq` [アイテム売値要求]

* **Original Japanese DMS Title**: `アイテム売値要求`
* **Raw DMS Script**: [`31031.dms`](../dms/31031.dms)
* **Legacy Delphi Class**: `TMsgGameItemDropReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31031.dms
function parse(packet){
	with(packet){
		setTitle("アイテム売値要求");
		readInt32("idNpc");
		readInt32("idType");
	}
}
```

---

## Opcode 0x7938 (31032): `MsgUnknown_0x7938` [アイテム売値返答]

* **Original Japanese DMS Title**: `アイテム売値返答`
* **Raw DMS Script**: [`31032.dms`](../dms/31032.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x7938` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31032.dms
function parse(packet){
	with(packet){
		setTitle("アイテム売値返答");
		readInt32("idType");
		readInt64("price");
	}
}
```

---

## Opcode 0x7939 (31033): `MsgGameItemPickUpReq` [アイテム売却要求]

* **Original Japanese DMS Title**: `アイテム売却要求`
* **Raw DMS Script**: [`31033.dms`](../dms/31033.dms)
* **Legacy Delphi Class**: `TMsgGameItemPickUpReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31033.dms
function parse(packet){
	with(packet){
		setTitle("アイテム売却要求");
		readInt32("idNpc");
		for(var i = 1; i <= 20; i++){
			readInt32("  idType" + i.toString());
			readWord("  count or Dim1Index" + i.toString());
			readWord("  (invalid) or Dim2Index" + i.toString());
			readInt32("  (invalid) or idItem" + i.toString());
		}
	}
}
```

---

## Opcode 0x793A (31034): `MsgUnknown_0x793A` [アイテム売却返答]

* **Original Japanese DMS Title**: `アイテム売却返答`
* **Raw DMS Script**: [`31034.dms`](../dms/31034.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x793A` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31034.dms
function parse(packet){
	with(packet){
		setTitle("アイテム売却返答");
		readInt64("price");
		var cnt = readWord("vecSoldItemsCount");
		for(var i = 1; i <= cnt; i++){
			readInt32("  idType" + i.toString());
			readWord("  count or Dim1Index" + i.toString());
			readWord("  (invalid) or Dim2Index" + i.toString());
			readInt32("  (invalid) or idItem" + i.toString());
		}
		readInt32("戻り値");
	}
}
```

---

## Opcode 0x793B (31035): `MsgGameItemUseReq` [NPCダイアログ開始通知]

* **Original Japanese DMS Title**: `NPCダイアログ開始通知`
* **Raw DMS Script**: [`31035.dms`](../dms/31035.dms)
* **Legacy Delphi Class**: `TMsgGameItemUseReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31035.dms
function parse(packet){
	with(packet){
		setTitle("NPCダイアログ開始通知");
		readInt32("NPCID");
	}
}
```

---

## Opcode 0x793E (31038): `MsgUnknown_0x793E` [NPCダイアログイベント通知]

* **Original Japanese DMS Title**: `NPCダイアログイベント通知`
* **Raw DMS Script**: [`31038.dms`](../dms/31038.dms)
* **Legacy Delphi Class**: `TMsgGameNpcDialogEventNtf` (Address: `005AC6C4`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31038.dms
function parse(packet){
	packet.setTitle("NPCダイアログイベント通知");
	packet.readInt32("NPCID");
	packet.readInt32("イベント");
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameNpcDialogEventNtf.Create(NpcId:Integer; Event:Integer)
005AC6C4    push        ebp
 005AC6C5    mov         ebp,esp
 005AC6C7    push        ebx
 005AC6C8    push        esi
 005AC6C9    push        edi
 005AC6CA    test        dl,dl
>005AC6CC    je          005AC6D6
 005AC6CE    add         esp,0FFFFFFF0
 005AC6D1    call        @ClassCreate
 005AC6D6    mov         esi,ecx
 005AC6D8    mov         ebx,edx
 005AC6DA    mov         edi,eax
 005AC6DC    mov         dx,793E
 005AC6E0    mov         eax,edi
 005AC6E2    call        TYgPacket.WriteID
 005AC6E7    mov         edx,esi
 005AC6E9    mov         eax,edi
 005AC6EB    call        TYgPacket.W
```

---

## Opcode 0x793F (31039): `MsgUnknown_0x793F` [NPCダイアログ商品リスト通知]

* **Original Japanese DMS Title**: `NPCダイアログ商品リスト通知`
* **Raw DMS Script**: [`31039.dms`](../dms/31039.dms)
* **Legacy Delphi Class**: `TMsgGameNpcDialogSaleListNtf` (Address: `005AC718`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31039.dms
function parse(packet){
	with(packet){
		setTitle("NPCダイアログ商品リスト通知");
		readInt32("idNpc");
		var cnt = readWord("vecItemCount");
		for(var i = 1; i <= cnt; i++){
			readInt32("  typeItem" + i.toString());
			readInt64("  priceItem" + i.toString());
		}
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameNpcDialogSaleListNtf.Create(NpcId:Integer; ItemList:TNpcBuyItemList)
005AC718    push        ebp
 005AC719    mov         ebp,esp
 005AC71B    add         esp,0FFFFFFF4
 005AC71E    push        ebx
 005AC71F    push        esi
 005AC720    test        dl,dl
>005AC722    je          005AC72C
 005AC724    add         esp,0FFFFFFF0
 005AC727    call        @ClassCreate
 005AC72C    mov         esi,ecx
 005AC72E    mov         byte ptr [ebp-5],dl
 005AC731    mov         dword ptr [ebp-4],eax
 005AC734    mov         ebx,dword ptr [ebp+8]
 005AC737    mov         dx,793F
 005AC73B    mov         eax,dword ptr [ebp-4]
 005AC73E    call        TYgPacket.WriteID
 005A
```

---

## Opcode 0x7940 (31040): `MsgGameQuickSlotSetReq` [NPCアクション終了通知]

* **Original Japanese DMS Title**: `NPCアクション終了通知`
* **Raw DMS Script**: [`31040.dms`](../dms/31040.dms)
* **Legacy Delphi Class**: `TMsgGameQuickSlotSetReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31040.dms
function parse(packet){
	with(packet){
		setTitle("NPCアクション終了通知");
	}
}
```

---

## Opcode 0x7942 (31042): `MsgGameVisualAttachNtf` [NPC生成通知]

* **Original Japanese DMS Title**: `NPC生成通知`
* **Raw DMS Script**: [`31042.dms`](../dms/31042.dms)
* **Legacy Delphi Class**: `TMsgGameNpcCreateNtf` (Address: `005AC9DC`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31042.dms
function parse(packet){
	packet.setTitle("NPC生成通知");
	packet.readUInt32("NPCID");
	packet.readUInt32("idShellType");
	packet.readWord("X");
	packet.readWord("Y");
	packet.readByte("向き");
	packet.readBinary(3, "(padding)");
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameNpcCreateNtf.Create(NpcID:Integer; Dir:Byte; Y:Word; X:Word; ShellType:Integer)
005AC9DC    push        ebp
 005AC9DD    mov         ebp,esp
 005AC9DF    push        ebx
 005AC9E0    push        esi
 005AC9E1    push        edi
 005AC9E2    test        dl,dl
>005AC9E4    je          005AC9EE
 005AC9E6    add         esp,0FFFFFFF0
 005AC9E9    call        @ClassCreate
 005AC9EE    mov         esi,ecx
 005AC9F0    mov         ebx,edx
 005AC9F2    mov         edi,eax
 005AC9F4    mov         dx,7942
 005AC9F8    mov         eax,edi
 005AC9FA    call        TYgPacket.WriteID
 005AC9FF    mov         edx,esi
 005ACA01    mov         eax,edi
 005ACA03    call        TYgPacket.W
```

---

## Opcode 0x7944 (31044): `MsgGameStarEquipReq` [BEITEM装備要求]

* **Original Japanese DMS Title**: `BEITEM装備要求`
* **Raw DMS Script**: [`31044.dms`](../dms/31044.dms)
* **Legacy Delphi Class**: `TMsgGameStarEquipReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31044.dms
function parse(packet){
	with(packet){
		setTitle("BEITEM装備要求");
		readWord("BeItemID-low");
		readWord("BeItemID-high");
		readInt32("BeItemID-???");
	}
}
```

---

## Opcode 0x7945 (31045): `MsgGameEquipAns` [BEITEM装備返答]

* **Original Japanese DMS Title**: `BEITEM装備返答`
* **Raw DMS Script**: [`31045.dms`](../dms/31045.dms)
* **Legacy Delphi Class**: `TMsgGameEquipBeItemAns` (Address: `005ACAA4`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31045.dms
function parse(packet){
	with(packet){
		setTitle("BEITEM装備返答");
		readInt32("戻り値");
		readInt32("キャラID");
		readWord("idBeItemDim1Index");
		readWord("idBeItemDim2Index");
		readInt32("idBeItem");
		readInt32("typeBeItem");
		for(var i = 1; i <= 5; i++){
			readInt32("reinforceslot" + i.toString());
		}
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameEquipBeItemAns.Create(Chara:TChara; Item:TEquippableItem)
005ACAA4    push        ebp
 005ACAA5    mov         ebp,esp
 005ACAA7    push        ecx
 005ACAA8    push        ebx
 005ACAA9    push        esi
 005ACAAA    push        edi
 005ACAAB    test        dl,dl
>005ACAAD    je          005ACAB7
 005ACAAF    add         esp,0FFFFFFF0
 005ACAB2    call        @ClassCreate
 005ACAB7    mov         edi,ecx
 005ACAB9    mov         byte ptr [ebp-1],dl
 005ACABC    mov         ebx,eax
 005ACABE    mov         esi,dword ptr [ebp+8]
 005ACAC1    mov         dx,7945
 005ACAC5    mov         eax,ebx
 005ACAC7    call        TYgPacket.WriteID
 005ACACC    m
```

---

## Opcode 0x7946 (31046): `MsgGameStarUnequipReq` [BEITEM装備解除要求]

* **Original Japanese DMS Title**: `BEITEM装備解除要求`
* **Raw DMS Script**: [`31046.dms`](../dms/31046.dms)
* **Legacy Delphi Class**: `TMsgGameStarUnequipReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31046.dms
function parse(packet){
	with(packet){
		setTitle("BEITEM装備解除要求");
		readWord("BeItemID-low");
		readWord("BeItemID-high");
		readInt32("BeItemID");
	}
}
```

---

## Opcode 0x7947 (31047): `MsgGameUnequipAns` [BEITEM装備解除返答]

* **Original Japanese DMS Title**: `BEITEM装備解除返答`
* **Raw DMS Script**: [`31047.dms`](../dms/31047.dms)
* **Legacy Delphi Class**: `TMsgGameStripBeItemAns` (Address: `005ACB28`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31047.dms
function parse(packet){
	with(packet){
		setTitle("BEITEM装備解除返答");
		readInt32("戻り値");
		readInt32("キャラID");
		readWord("BeItemID-low");
		readWord("BeItemID-high");
		readInt32("BeItemID");
		readInt32("BeItemタイプ");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameStripBeItemAns.Create(Chara:TChara; Item:TEquippableItem)
005ACB28    push        ebp
 005ACB29    mov         ebp,esp
 005ACB2B    push        ecx
 005ACB2C    push        ebx
 005ACB2D    push        esi
 005ACB2E    push        edi
 005ACB2F    test        dl,dl
>005ACB31    je          005ACB3B
 005ACB33    add         esp,0FFFFFFF0
 005ACB36    call        @ClassCreate
 005ACB3B    mov         edi,ecx
 005ACB3D    mov         byte ptr [ebp-1],dl
 005ACB40    mov         ebx,eax
 005ACB42    mov         esi,dword ptr [ebp+8]
 005ACB45    mov         dx,7947
 005ACB49    mov         eax,ebx
 005ACB4B    call        TYgPacket.WriteID
 005ACB50    m
```

---

## Opcode 0x7948 (31048): `MsgUnknown_0x7948` [他キャラ情報通知]

* **Original Japanese DMS Title**: `他キャラ情報通知`
* **Raw DMS Script**: [`31048.dms`](../dms/31048.dms)
* **Legacy Delphi Class**: `TMsgGameOtherCharInfoNtf` (Address: `005ACBA0`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31048.dms
import struct.*;
function parse(packet){
	packet.setTitle("他キャラ情報通知");
	packet.readUInt32("キャラID");
	char_disp_info(packet);
	packet.readWord("X");
	packet.readWord("Y");
	packet.readInt32("ステータス");
	packet.readInt32("向き1");
	packet.readInt32("向き2");
	packet.readInt32("typeInteract");
	packet.readInt32("idInteractObject");
	packet.readInt32("idTitle");
	packet.readInt32("curWeaponType");
	packet.readInt32("typeQuestReward");
	//packet.readInt32();
	//packet.readInt32();
	//packet.readInt32("看板表示状況");
	//packet.readWStr(0x4A,"看板コメント");
	//packet.readBinary(2, "(padding)");
	char_byulitem_effect_info(packet);
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameOtherCharInfoNtf.Create(Chara:TChara; MoveFinished:Boolean)
005ACBA0    push        ebp
 005ACBA1    mov         ebp,esp
 005ACBA3    push        ebx
 005ACBA4    push        esi
 005ACBA5    push        edi
 005ACBA6    test        dl,dl
>005ACBA8    je          005ACBB2
 005ACBAA    add         esp,0FFFFFFF0
 005ACBAD    call        @ClassCreate
 005ACBB2    mov         esi,ecx
 005ACBB4    mov         ebx,edx
 005ACBB6    mov         edi,eax
 005ACBB8    mov         dx,7948
 005ACBBC    mov         eax,edi
 005ACBBE    call        TYgPacket.WriteID
 005ACBC3    mov         edx,dword ptr [esi+0E0];TChara.ID:Integer
 005ACBC9    mov         eax,edi
 0
```

---

## Opcode 0x7949 (31049): `MsgUnknown_0x7949` [他キャラ消滅通知]

* **Original Japanese DMS Title**: `他キャラ消滅通知`
* **Raw DMS Script**: [`31049.dms`](../dms/31049.dms)
* **Legacy Delphi Class**: `TMsgGameOtherCharDisappearNtf` (Address: `005ACC8C`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31049.dms
function parse(packet){
	packet.setTitle("他キャラ消滅通知");
	packet.readInt32("消滅キャラID");
	packet.readInt32("消滅タイプ");
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameOtherCharDisappearNtf.Create(Chara:TChara; OutOfRange:Boolean)
005ACC8C    push        ebp
 005ACC8D    mov         ebp,esp
 005ACC8F    push        ebx
 005ACC90    push        esi
 005ACC91    push        edi
 005ACC92    test        dl,dl
>005ACC94    je          005ACC9E
 005ACC96    add         esp,0FFFFFFF0
 005ACC99    call        @ClassCreate
 005ACC9E    mov         esi,ecx
 005ACCA0    mov         ebx,edx
 005ACCA2    mov         edi,eax
 005ACCA4    mov         dx,7949
 005ACCA8    mov         eax,edi
 005ACCAA    call        TYgPacket.WriteID
 005ACCAF    mov         edx,dword ptr [esi+0E0];TChara.ID:Integer
 005ACCB5    mov         eax,edi
 0
```

---

## Opcode 0x794A (31050): `MsgGameReinforceItemReq` [ホットキー設定通知]

* **Original Japanese DMS Title**: `ホットキー設定通知`
* **Raw DMS Script**: [`31050.dms`](../dms/31050.dms)
* **Legacy Delphi Class**: `TMsgGameReinforceItemReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31050.dms
function parse(packet){
	with(packet){
		setTitle("ホットキー設定通知");
		readInt32("インデックス");
		readInt32("アイテム種類");
		readInt32("アイテムSN1");
		readInt32("アイテムSN2");
	}
}
```

---

## Opcode 0x7952 (31058): `MsgGameCharInfoNtf` [キャラ情報通知]

* **Original Japanese DMS Title**: `キャラ情報通知`
* **Raw DMS Script**: [`31058.dms`](../dms/31058.dms)
* **Legacy Delphi Class**: `TMsgGameCharInfoNtf` (Address: `005ACDBC`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31058.dms
import struct.*;
function parse(packet){
	with(packet){
		setTitle("キャラ情報通知");
		readUInt32("bAvailable");
		char_play_info(packet);
		char_beitem_list(packet, readWord("countBeItemList"));
		char_coitem_list(packet, readWord("countCoItemList"));
		char_questitem_list(packet, readWord("countQuestItemList"));
		char_enitem_list(packet, readWord("countEnItemList"));
		char_byulitem_list(packet, readWord("countByulItemList"));
		char_byulitem_list2(packet, readWord("countByulItemList2"));
		char_byulitem_effect_list(packet, readWord("countByulItemEffectList"));
		readInt32("bByulPresent");
		readInt32("bProductPresent");
		readInt32("bHistoryByul");
		readInt32("bPresentBox");
		readInt32("bHistoryPresentRcv");
		readInt32("bHistoryPresentSnd");
		readInt32("bHistoryTease");
		readInt32("idQuestReward");
		readInt32("remainQuestRewardSecond");
		char_byulitem_effect_info(packet);
		readInt32("limitInvalidCouponInput");
		readInt32("remainCouponInputSecond");
		readInt32("DirX");
		readInt32("DirY");

	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameCharInfoNtf.Create(Chara:TChara)
005ACDBC    push        ebp
 005ACDBD    mov         ebp,esp
 005ACDBF    add         esp,0FFFFFFE4
 005ACDC2    push        ebx
 005ACDC3    test        dl,dl
>005ACDC5    je          005ACDCF
 005ACDC7    add         esp,0FFFFFFF0
 005ACDCA    call        @ClassCreate
 005ACDCF    mov         dword ptr [ebp-0C],ecx
 005ACDD2    mov         byte ptr [ebp-5],dl
 005ACDD5    mov         dword ptr [ebp-4],eax
 005ACDD8    mov         dx,7952
 005ACDDC    mov         eax,dword ptr [ebp-4]
 005ACDDF    call        TYgPacket.WriteID
 005ACDE4    mov         edx,1
 005ACDE9    mov         eax,dword 
```

---

## Opcode 0x7956 (31062): `MsgGameFieldInfoDoneNtf` [フィールド情報完了通知]

* **Original Japanese DMS Title**: `フィールド情報完了通知`
* **Raw DMS Script**: [`31062.dms`](../dms/31062.dms)
* **Legacy Delphi Class**: `TMsgGameFieldInfoDoneNtf` (Address: `005AD210`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31062.dms
function parse(packet){
	packet.setTitle("フィールド情報完了通知");
	packet.readInt32("フィールドID");
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameFieldInfoDoneNtf.Create(Field:TField)
005AD210    push        ebx
 005AD211    push        esi
 005AD212    push        edi
 005AD213    test        dl,dl
>005AD215    je          005AD21F
 005AD217    add         esp,0FFFFFFF0
 005AD21A    call        @ClassCreate
 005AD21F    mov         esi,ecx
 005AD221    mov         ebx,edx
 005AD223    mov         edi,eax
 005AD225    mov         dx,7956
 005AD229    mov         eax,edi
 005AD22B    call        TYgPacket.WriteID
 005AD230    test        esi,esi
>005AD232    jne         005AD244
 005AD234    mov         ecx,4
 005AD239    mov         dl,0CC
 005AD23B    mov         eax,edi
 
```

---

## Opcode 0x7957 (31063): `MsgUnknown_0x7957` [エピソードプレイ再開通知]

* **Original Japanese DMS Title**: `エピソードプレイ再開通知`
* **Raw DMS Script**: [`31063.dms`](../dms/31063.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x7957` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31063.dms
function parse(packet){
	with(packet){
		setTitle("エピソードプレイ再開通知");
	}
}
```

---

## Opcode 0x795A (31066): `MsgGameFieldLoadingStartNtf` [フィールド読み込み開始通知]

* **Original Japanese DMS Title**: `フィールド読み込み開始通知`
* **Raw DMS Script**: [`31066.dms`](../dms/31066.dms)
* **Legacy Delphi Class**: `TMsgGameFieldLoadingStartNtf` (Address: `005AD2FC`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31066.dms
function parse(packet){
	packet.setTitle("フィールド読み込み開始通知");
	packet.readByte("フィールドタイプ");
	packet.readBinary(3, "(padding)");
	packet.readInt32("フィールドID");
	packet.readWord("X");
	packet.readWord("Y");
	packet.readUInt32("bHuntField");
	packet.readUInt32("idHuntField");
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameFieldLoadingStartNtf.Create(Chara:TChara)
005AD2FC    push        ebx
 005AD2FD    push        esi
 005AD2FE    push        edi
 005AD2FF    test        dl,dl
>005AD301    je          005AD30B
 005AD303    add         esp,0FFFFFFF0
 005AD306    call        @ClassCreate
 005AD30B    mov         esi,ecx
 005AD30D    mov         ebx,edx
 005AD30F    mov         edi,eax
 005AD311    movzx       eax,byte ptr [esi+2C];TChara.FFieldState:TFieldState
 005AD315    dec         al
>005AD317    je          005AD32E
 005AD319    sub         al,2
>005AD31B    je          005AD3B0
 005AD321    dec         al
>005AD323    je          005AD3F7
>005AD3
```

---

## Opcode 0x795B (31067): `MsgGameEmoteReq` [フィールド読み込み完了通知]

* **Original Japanese DMS Title**: `フィールド読み込み完了通知`
* **Raw DMS Script**: [`31067.dms`](../dms/31067.dms)
* **Legacy Delphi Class**: `TMsgGameEmoteReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31067.dms
function parse(packet){
	packet.setTitle("フィールド読み込み完了通知");
}
```

---

## Opcode 0x795C (31068): `MsgGameFieldEntitySpawnNtf` [トリガ作用通知(モンスター消去)]

* **Original Japanese DMS Title**: `トリガ作用通知(モンスター消去)`
* **Raw DMS Script**: [`31068.dms`](../dms/31068.dms)
* **Legacy Delphi Class**: `TMsgGameTriggerActionNtf` (Address: `005ADA40`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31068.dms
function parse(packet){
	switch(packet.readUInt32("typeAction")){
		case 0x03:
			packet.setTitle("トリガ作用通知(モンスター消去)");
			var cnt = packet.readInt32("消去モンスター数");
			for(var i = 0; i < cnt; i++){
				packet.readInt32("モンスターID");
			}
			for(var len = 0x44-cnt*4; len > 0; len-=4){
				packet.readBinary(4, "(padding)");
			}
			break;
		case 0x0A:
			packet.setTitle("トリガ作用通知(ワープゲート表示)");
			packet.readUInt32("ゲートID");
			packet.readUInt32("X(x100)");
			packet.readUInt32("Y(x100)");
			packet.readUInt32("shell");
			packet.readUInt32("idCli");	// マップ内に決まった数字で１つしか置けない様子？存在しない番号でも置けない？
			packet.readUInt32("dir");
			packet.readUInt32("idDestField");
			packet.readBinary(0x2C, "(padding)");
			break;
		case 0x25:
			packet.setTitle("トリガ作用通知(バス移動デモ)");
			packet.readInt32();
			packet.readInt32();
			packet.readInt32();
			packet.readBinary(0x3C, "(padding)");
			break;
		case 0x27:
			var i = packet.readUInt32("padding？(SEとBGMで違う)");	// 600,601=SE 800=BGM
			if(i == 800){
				packet.setTitle("トリガ作用通知(BGM変更)");
				packet.readUInt32("BGM番号");
			}else if((i == 600) || (i == 601)){
				packet.setTitle("トリガ作用通知(SE変更)");
				packet.readUInt32("SE番号");
			}else{
				packet.setTitle("トリガ作用通知(BGM変更？)");
				packet.readUInt32("BGM番号");
			}
			packet.readUInt32("再生(BOOLEAN)");		// 再生TRUEで同じ局番号のリクエストしても、最初から再生はないみたい？
			packet.readUInt32("再生回数(0:無限)");	// そして、繰り返しが終わっていたとしても同じ番号リクエストだと再生はしてくれないみたい
			packet.readBinary(0x38, "(padding)");
			break;
		default:
			packet.setTitle("トリガ作用通知(未対応フラグ)");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameTriggerActionNtf.Create(TimerID:Integer)
005ADA40    push        ebx
 005ADA41    push        esi
 005ADA42    push        edi
 005ADA43    test        dl,dl
>005ADA45    je          005ADA4F
 005ADA47    add         esp,0FFFFFFF0
 005ADA4A    call        @ClassCreate
 005ADA4F    mov         esi,ecx
 005ADA51    mov         ebx,edx
 005ADA53    mov         edi,eax
 005ADA55    mov         dx,795C
 005ADA59    mov         eax,edi
 005ADA5B    call        TYgPacket.WriteID
 005ADA60    mov         edx,18
 005ADA65    mov         eax,edi
 005ADA67    call        TYgPacket.WriteInt32
 005ADA6C    mov         edx,esi
 005ADA6E    mov    
```

---

## Opcode 0x795D (31069): `MsgUnknown_0x795D` [レバー引き要求]

* **Original Japanese DMS Title**: `レバー引き要求`
* **Raw DMS Script**: [`31069.dms`](../dms/31069.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x795D` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31069.dms
function parse(packet){
	with(packet){
		setTitle("レバー引き要求");
		readInt32("idObject");
	}
}
```

---

## Opcode 0x795E (31070): `MsgUnknown_0x795E` [レバー引き返答]

* **Original Japanese DMS Title**: `レバー引き返答`
* **Raw DMS Script**: [`31070.dms`](../dms/31070.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x795E` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31070.dms
function parse(packet){
	with(packet){
		setTitle("レバー引き返答");
		readInt32("idObject");
		readInt32("result");
		readByte("status");
		readBinary(3, "(padding)");
	}
}
```

---

## Opcode 0x7963 (31075): `MsgGameChatNtf` [チャット通知]

* **Original Japanese DMS Title**: `チャット通知`
* **Raw DMS Script**: [`31075.dms`](../dms/31075.dms)
* **Legacy Delphi Class**: `TMsgGameChatNtf` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31075.dms
function parse(packet){
	packet.setTitle("チャット通知");
	packet.readInt32("送信元ID");
	var flag = packet.readUInt32("カラー");
	packet.readUInt32("Emoticon");
	packet.readWStr(0x1A, "キャラ名");
	packet.readUInt32("メッセージフォーマット番号(MATCHING_SYS_MSG.clt)");
	var cnt = packet.readWord("フォーマットパラメータ数");
	for(var i = 1; i <= cnt; i++){
		packet.readWStr(packet.readWord("  文字列長" + i.toString()), "  文字列" + i.toString());
	}
	packet.readWStr(packet.readWord("メッセージ長"), "メッセージ");
}
```

---

## Opcode 0x7964 (31076): `MsgUnknown_0x7964` [メッセージ通知]

* **Original Japanese DMS Title**: `メッセージ通知`
* **Raw DMS Script**: [`31076.dms`](../dms/31076.dms)
* **Legacy Delphi Class**: `TMsgGameMessageNtf` (Address: `005ADDDC`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31076.dms
function parse(packet){
	with(packet){
		setTitle("メッセージ通知");
		readInt32("メッセージタイプ");
		readInt32();
		readInt32();
		readInt32();
		readWStr(readWord("題名長"), "題名");
		readWStr(readWord("本文長"), "本文");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameMessageNtf.Create(Subject:string; MessageType:Integer; Content:string)
005ADDDC    push        ebp
 005ADDDD    mov         ebp,esp
 005ADDDF    push        ebx
 005ADDE0    push        esi
 005ADDE1    push        edi
 005ADDE2    test        dl,dl
>005ADDE4    je          005ADDEE
 005ADDE6    add         esp,0FFFFFFF0
 005ADDE9    call        @ClassCreate
 005ADDEE    mov         esi,ecx
 005ADDF0    mov         ebx,edx
 005ADDF2    mov         edi,eax
 005ADDF4    mov         dx,7964
 005ADDF8    mov         eax,edi
 005ADDFA    call        TYgPacket.WriteID
 005ADDFF    mov         edx,dword ptr [ebp+8]
 005ADE02    mov         eax,edi
 005ADE04    call     
```

---

## Opcode 0x7965 (31077): `MsgGameChannelSwitchReq` [ワープ要求]

* **Original Japanese DMS Title**: `ワープ要求`
* **Raw DMS Script**: [`31077.dms`](../dms/31077.dms)
* **Legacy Delphi Class**: `TMsgGameChannelSwitchReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31077.dms
function parse(packet){
	with(packet){
		setTitle("ワープ要求");
		readInt32("idChar");
		readChar("idWarpCell");
		readBinary(3,"(padding?)");
	}
}
```

---

## Opcode 0x7966 (31078): `MsgGameWarpStartNtf` [ワープ返答]

* **Original Japanese DMS Title**: `ワープ返答`
* **Raw DMS Script**: [`31078.dms`](../dms/31078.dms)
* **Legacy Delphi Class**: `TMsgGameWarpAns` (Address: `005ADE60`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31078.dms
function parse(packet){
	with(packet){
		setTitle("ワープ返答");
		readInt32("result");
		readInt32("idDestField");
		readWord("posDestX");
		readWord("posDestY");
		if( readInt32("bHuntField") ) {
			readInt32("idHuntField");
		}else {
			readBinary(4, "(padding)");
		}
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameWarpAns.Create(Chara:TChara)
005ADE60    push        ebx
 005ADE61    push        esi
 005ADE62    push        edi
 005ADE63    test        dl,dl
>005ADE65    je          005ADE6F
 005ADE67    add         esp,0FFFFFFF0
 005ADE6A    call        @ClassCreate
 005ADE6F    mov         esi,ecx
 005ADE71    mov         ebx,edx
 005ADE73    mov         edi,eax
 005ADE75    mov         dx,7966
 005ADE79    mov         eax,edi
 005ADE7B    call        TYgPacket.WriteID
 005ADE80    mov         edx,1
 005ADE85    mov         eax,edi
 005ADE87    call        TYgPacket.WriteRC
 005ADE8C    mov         eax,dword ptr [esi+29C];TChara.F
```

---

## Opcode 0x7967 (31079): `MsgGameWarpGateReq` [ワープ完了通知]

* **Original Japanese DMS Title**: `ワープ完了通知`
* **Raw DMS Script**: [`31079.dms`](../dms/31079.dms)
* **Legacy Delphi Class**: `TMsgGameWarpGateReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31079.dms
function parse(packet){
	with(packet){
		setTitle("ワープ完了通知");
		readInt32("キャラID");
		readInt32("移動先フィールドID");
	}
}
```

---

## Opcode 0x7968 (31080): `MsgGameWarpResultNtf` [ワープ結果通知]

* **Original Japanese DMS Title**: `ワープ結果通知`
* **Raw DMS Script**: [`31080.dms`](../dms/31080.dms)
* **Legacy Delphi Class**: `TMsgGameWarpResultNtf` (Address: `005ADF0C`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31080.dms
function parse(packet){
	with(packet){
		setTitle("ワープ結果通知");
		readInt32("フィールドID");
		readWord("X");
		readWord("Y");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameWarpResultNtf.Create(Chara:TChara)
005ADF0C    push        ebx
 005ADF0D    push        esi
 005ADF0E    push        edi
 005ADF0F    test        dl,dl
>005ADF11    je          005ADF1B
 005ADF13    add         esp,0FFFFFFF0
 005ADF16    call        @ClassCreate
 005ADF1B    mov         esi,ecx
 005ADF1D    mov         ebx,edx
 005ADF1F    mov         edi,eax
 005ADF21    mov         dx,7968
 005ADF25    mov         eax,edi
 005ADF27    call        TYgPacket.WriteID
 005ADF2C    mov         eax,dword ptr [esi+29C];TChara.Field:TField
 005ADF32    call        0060B9DC
 005ADF37    mov         edx,eax
 005ADF39    mov         eax
```

---

## Opcode 0x7969 (31081): `MsgGameMonMoveNtf` [モンスター移動通知]

* **Original Japanese DMS Title**: `モンスター移動通知`
* **Raw DMS Script**: [`31081.dms`](../dms/31081.dms)
* **Legacy Delphi Class**: `TMsgGameMonMoveNtf` (Address: `005ADF68`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31081.dms
function parse(packet){
	with(packet){
		setTitle("モンスター移動通知");
		readInt32("idMonster");
		readWord("posCurX");
		readWord("posCurY");
		readWord("posDestX");
		readWord("posDestY");
		readInt32("motion");
		readByte("speedRate");
		readBinary(3, "(padding)");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameMonMoveNtf.Create(Monster:TMonster)
005ADF68    push        ebx
 005ADF69    push        esi
 005ADF6A    push        edi
 005ADF6B    test        dl,dl
>005ADF6D    je          005ADF77
 005ADF6F    add         esp,0FFFFFFF0
 005ADF72    call        @ClassCreate
 005ADF77    mov         esi,ecx
 005ADF79    mov         ebx,edx
 005ADF7B    mov         edi,eax
 005ADF7D    mov         dx,7969
 005ADF81    mov         eax,edi
 005ADF83    call        TYgPacket.WriteID
 005ADF88    mov         edx,dword ptr [esi+10];TMonster.FId:Integer
 005ADF8B    mov         eax,edi
 005ADF8D    call        TYgPacket.WriteInt32
 005ADF92    mov
```

---

## Opcode 0x796A (31082): `MsgGameMonAttackNtf` [モンスター攻撃通知]

* **Original Japanese DMS Title**: `モンスター攻撃通知`
* **Raw DMS Script**: [`31082.dms`](../dms/31082.dms)
* **Legacy Delphi Class**: `TMsgGameMonAttackNtf` (Address: `005AE004`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31082.dms
function parse(packet){
	with(packet){
		setTitle("モンスター攻撃通知");
		readInt32("idMonster");
		readWord("posX");
		readWord("posY");
		readInt32("typeMotion");
		readInt32("idTargetChar");
		readInt32("damage");
		readInt64("pocketMoney");
		readByte("speedRate");
		readByte("typeHit");
		readBinary(2, "(padding)");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameMonAttackNtf.Create(Monster:TMonster; TypeHit:Integer; Damage:Integer)
005AE004    push        ebp
 005AE005    mov         ebp,esp
 005AE007    push        ebx
 005AE008    push        esi
 005AE009    push        edi
 005AE00A    test        dl,dl
>005AE00C    je          005AE016
 005AE00E    add         esp,0FFFFFFF0
 005AE011    call        @ClassCreate
 005AE016    mov         esi,ecx
 005AE018    mov         ebx,edx
 005AE01A    mov         edi,eax
 005AE01C    mov         dx,796A
 005AE020    mov         eax,edi
 005AE022    call        TYgPacket.WriteID
 005AE027    mov         edx,dword ptr [esi+10];TMonster.FId:Integer
 005AE02A    mov         eax,edi

```

---

## Opcode 0x796C (31084): `MsgUnknown_0x796C` [モンスター死亡通知]

* **Original Japanese DMS Title**: `モンスター死亡通知`
* **Raw DMS Script**: [`31084.dms`](../dms/31084.dms)
* **Legacy Delphi Class**: `TMsgGameMonDeadNtf` (Address: `005AE0BC`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31084.dms
function parse(packet){
	with(packet){
		setTitle("モンスター死亡通知");
		readInt32("idMonster");
		readWord("posX");
		readWord("posY");
		var cnt = readWord("vecItemMonDead");
		for(var i = 0; i < cnt; i++){
			readInt32("├Unknown");
			readInt32("├Unknown");
			readInt32("└Unknown");
		}
		readInt32("countDropItem");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameMonDeadNtf.Create(Monster:TMonster; Drop:TCoItem; BootyBoxID:Integer)
005AE0BC    push        ebp
 005AE0BD    mov         ebp,esp
 005AE0BF    push        ecx
 005AE0C0    push        ebx
 005AE0C1    push        esi
 005AE0C2    push        edi
 005AE0C3    test        dl,dl
>005AE0C5    je          005AE0CF
 005AE0C7    add         esp,0FFFFFFF0
 005AE0CA    call        @ClassCreate
 005AE0CF    mov         esi,ecx
 005AE0D1    mov         byte ptr [ebp-1],dl
 005AE0D4    mov         ebx,eax
 005AE0D6    mov         edi,dword ptr [ebp+8]
 005AE0D9    mov         dx,796C
 005AE0DD    mov         eax,ebx
 005AE0DF    call        TYgPacket.WriteID
 005AE0E4    m
```

---

## Opcode 0x796D (31085): `MsgGameMonStatusNtf` [モンスター生成通知]

* **Original Japanese DMS Title**: `モンスター生成通知`
* **Raw DMS Script**: [`31085.dms`](../dms/31085.dms)
* **Legacy Delphi Class**: `TMsgGameMonSpawnNtf` (Address: `005AE170`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31085.dms
function parse(packet){
	with(packet){
		setTitle("モンスター生成通知");
		readInt32("idMonster");
		readInt32("typeMonster");
		readInt32("hpCurrent");
		readInt32("hpMax");
		readWord("X");
		readWord("Y");
		readInt32("dirX");
		readInt32("dirY");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameMonSpawnNtf.Create(Monster:TMonster)
005AE170    push        ebx
 005AE171    push        esi
 005AE172    push        edi
 005AE173    test        dl,dl
>005AE175    je          005AE17F
 005AE177    add         esp,0FFFFFFF0
 005AE17A    call        @ClassCreate
 005AE17F    mov         esi,ecx
 005AE181    mov         ebx,edx
 005AE183    mov         edi,eax
 005AE185    mov         dx,796D
 005AE189    mov         eax,edi
 005AE18B    call        TYgPacket.WriteID
 005AE190    mov         edx,dword ptr [esi+10];TMonster.FId:Integer
 005AE193    mov         eax,edi
 005AE195    call        TYgPacket.WriteInt32
 005AE19A    mov
```

---

## Opcode 0x796E (31086): `MsgGameMonInfoNtf` [モンスター情報通知]

* **Original Japanese DMS Title**: `モンスター情報通知`
* **Raw DMS Script**: [`31086.dms`](../dms/31086.dms)
* **Legacy Delphi Class**: `TMsgGameMonInfoNtf` (Address: `005AE1EC`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31086.dms
function parse(packet){
	with(packet){
		setTitle("モンスター情報通知");
		readInt32("idMonster");
		readInt32("typeMonster");
		readInt32("hpCurrent");
		readInt32("hpMax");
		readWord("X");
		readWord("Y");
		readInt32("dirX");
		readInt32("dirY");
		readInt32("bOwnership");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameMonInfoNtf.Create(Monster:TMonster)
005AE1EC    push        ebx
 005AE1ED    push        esi
 005AE1EE    push        edi
 005AE1EF    test        dl,dl
>005AE1F1    je          005AE1FB
 005AE1F3    add         esp,0FFFFFFF0
 005AE1F6    call        @ClassCreate
 005AE1FB    mov         esi,ecx
 005AE1FD    mov         ebx,edx
 005AE1FF    mov         edi,eax
 005AE201    mov         dx,796E
 005AE205    mov         eax,edi
 005AE207    call        TYgPacket.WriteID
 005AE20C    mov         edx,dword ptr [esi+10];TMonster.FId:Integer
 005AE20F    mov         eax,edi
 005AE211    call        TYgPacket.WriteInt32
 005AE216    mov
```

---

## Opcode 0x796F (31087): `MsgUnknown_0x796F` [キャラEXP通知]

* **Original Japanese DMS Title**: `キャラEXP通知`
* **Raw DMS Script**: [`31087.dms`](../dms/31087.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x796F` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31087.dms
function parse(packet){
	with(packet){
		setTitle("キャラEXP通知");
		readInt32("expUp");
		readInt32("exp");
	}
}
```

---

## Opcode 0x7970 (31088): `MsgGameCharLvUpNtf` [キャラレベルアップ通知]

* **Original Japanese DMS Title**: `キャラレベルアップ通知`
* **Raw DMS Script**: [`31088.dms`](../dms/31088.dms)
* **Legacy Delphi Class**: `TMsgGameCharLvUpNtf` (Address: `005AE27C`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31088.dms
function parse(packet){
	with(packet){
		setTitle("キャラレベルアップ通知");
		readInt32("level");
		readInt32("exp");
		readInt32("expMax");
		readInt32("skillPoint");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameCharLvUpNtf.Create(Chara:TChara)
005AE27C    push        ebx
 005AE27D    push        esi
 005AE27E    push        edi
 005AE27F    test        dl,dl
>005AE281    je          005AE28B
 005AE283    add         esp,0FFFFFFF0
 005AE286    call        @ClassCreate
 005AE28B    mov         esi,ecx
 005AE28D    mov         ebx,edx
 005AE28F    mov         edi,eax
 005AE291    mov         dx,7970
 005AE295    mov         eax,edi
 005AE297    call        TYgPacket.WriteID
 005AE29C    mov         edx,dword ptr [esi+50];TChara.FLevel:Integer
 005AE29F    mov         eax,edi
 005AE2A1    call        TYgPacket.WriteInt32
 005AE2A6    mo
```

---

## Opcode 0x7972 (31090): `MsgUnknown_0x7972` [エピソード結果通知]

* **Original Japanese DMS Title**: `エピソード結果通知`
* **Raw DMS Script**: [`31090.dms`](../dms/31090.dms)
* **Legacy Delphi Class**: `TMsgGameEpisodeResultNtf` (Address: `005AE2E8`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31090.dms
import struct.*;
function parse(packet){
	with(packet){
		setTitle("エピソード結果通知");
		readByte("countChar");
		var cnt = readWord("キャラ数");
		episode_char_info(packet, cnt);
		readInt32("points");
		readInt32("rank");
		var cnt = readWord("bootyBoxCount");
		for(var i = 1; i <= cnt; i++){
			readInt32("idChar");
			var icnt = readWord("itemBootyCount");
			for (var j = 0; j < icnt; j++){
				readItem(packet, false);
			}
			readByte("cntBootyItem");
			readItem(packet, false);
			readInt32("bPowerUp");
		}
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameEpisodeResultNtf.Create(Instance:TEpisodeInstance; Chara:TChara)
005AE2E8    push        ebp
 005AE2E9    mov         ebp,esp
 005AE2EB    add         esp,0FFFFFFF0
 005AE2EE    push        ebx
 005AE2EF    push        esi
 005AE2F0    push        edi
 005AE2F1    test        dl,dl
>005AE2F3    je          005AE2FD
 005AE2F5    add         esp,0FFFFFFF0
 005AE2F8    call        @ClassCreate
 005AE2FD    mov         esi,ecx
 005AE2FF    mov         byte ptr [ebp-5],dl
 005AE302    mov         dword ptr [ebp-4],eax
 005AE305    mov         dx,7972
 005AE309    mov         eax,dword ptr [ebp-4]
 005AE30C    call        TYgPacket.WriteID
 005AE311    mov       
```

---

## Opcode 0x7974 (31092): `MsgGameBootyBoxDoneReq` [戦利品箱終了要求]

* **Original Japanese DMS Title**: `戦利品箱終了要求`
* **Raw DMS Script**: [`31092.dms`](../dms/31092.dms)
* **Legacy Delphi Class**: `TMsgGameBootyBoxDoneReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31092.dms
function parse(packet){
	with(packet){
		setTitle("戦利品箱終了要求");
	}
}
```

---

## Opcode 0x7975 (31093): `MsgUnknown_0x7975` [戦利品箱終了返答]

* **Original Japanese DMS Title**: `戦利品箱終了返答`
* **Raw DMS Script**: [`31093.dms`](../dms/31093.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x7975` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31093.dms
function parse(packet){
	with(packet){
		setTitle("戦利品箱終了返答");
		readInt32("result");
	}
}
```

---

## Opcode 0x7977 (31095): `MsgUnknown_0x7977` [クエスト結果通知]

* **Original Japanese DMS Title**: `クエスト結果通知`
* **Raw DMS Script**: [`31095.dms`](../dms/31095.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x7977` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31095.dms
import struct.*;
function parse(packet){
	with(packet){
		setTitle("クエスト結果通知");
		readInt32("idQuest");
		var i, cnt;
		cnt = readWord("vecUseItem");
		for(i = 0; i < cnt; i++){
			readItem(packet, false);
		}
		cnt = readWord("vecAcqItem");
		for(i = 0; i < cnt; i++){
			readItem(packet, false);
		}
		readInt32("spReward_sn");
	}
}
```

---

## Opcode 0x7981 (31105): `MsgUnknown_0x7981` [アナウンス通知]

* **Original Japanese DMS Title**: `アナウンス通知`
* **Raw DMS Script**: [`31105.dms`](../dms/31105.dms)
* **Legacy Delphi Class**: `TMsgGameUpdateHandMoneyNtf` (Address: `005AE5B0`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31105.dms
function parse(packet){
	with(packet){
		setTitle("アナウンス通知");
  	readUInt32("メッセージフォーマット番号(MATCHING_SYS_MSG.clt)");
  	var cnt = readWord("フォーマットパラメータ数");
  	for(var i = 1; i <= cnt; i++){
  		readWStr(packet.readWord("  文字列長" + i.toString()), "  文字列" + i.toString());
  	}
		readWStr(readWord("メッセージ長"), "メッセージ");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameUpdateHandMoneyNtf.Create(Money:Int64; ?:?)
constructor sub_005AE5B0(FormatNum:Integer; Text:string; ?:?; FormatArgs:string);
begin
 005AE5B0    push        ebp
 005AE5B1    mov         ebp,esp
 005AE5B3    add         esp,0FFFFFFF4
 005AE5B6    push        ebx
 005AE5B7    push        esi
 005AE5B8    test        dl,dl
>005AE5BA    je          005AE5C4
 005AE5BC    add         esp,0FFFFFFF0
 005AE5BF    call        @ClassCreate
 005AE5C4    mov         esi,ecx
 005AE5C6    mov         byte ptr [ebp-5],dl
 005AE5C9    mov         dword ptr [ebp-4],eax
 005AE5CC    mov         ebx,dword ptr [ebp+0C]
 005AE5CF    mov         dx,7981
 005A
```

---

## Opcode 0x7988 (31112): `MsgUnknown_0x7988` [水情報通知]

* **Original Japanese DMS Title**: `水情報通知`
* **Raw DMS Script**: [`31112.dms`](../dms/31112.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x7988` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31112.dms
function parse(packet){
	with(packet){
		setTitle("水情報通知");
		readInt32("idWater");
		readInt32("dH");
		readInt32("timeDH");
		readInt32("speed1");
		readInt32("timeSpeed1");
		readInt32("speed2");
		readInt32("timeSpeed2");
		readInt32("texture1");
		readInt32("texture2");
	}
}
```

---

## Opcode 0x798C (31116): `MsgGameSitDownReq` [キャラ着座要求]

* **Original Japanese DMS Title**: `キャラ着座要求`
* **Raw DMS Script**: [`31116.dms`](../dms/31116.dms)
* **Legacy Delphi Class**: `TMsgGameSitDownReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31116.dms
function parse(packet){
	packet.setTitle("キャラ着座要求");
	packet.readWord("X");
	packet.readWord("Y");
	packet.readInt32("bChair");
}
```

---

## Opcode 0x798D (31117): `MsgGameSitDownAns` [キャラ着座返答]

* **Original Japanese DMS Title**: `キャラ着座返答`
* **Raw DMS Script**: [`31117.dms`](../dms/31117.dms)
* **Legacy Delphi Class**: `TMsgGameSitDownAns` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31117.dms
function parse(packet){
	packet.setTitle("キャラ着座返答");
	packet.readInt32("戻り値");
	packet.readInt32("キャラID");
	packet.readWord("X");
	packet.readWord("Y");
	packet.readInt32("bChair");
}
```

---

## Opcode 0x798E (31118): `MsgGameStandUpNtf` [キャラ起立通知]

* **Original Japanese DMS Title**: `キャラ起立通知`
* **Raw DMS Script**: [`31118.dms`](../dms/31118.dms)
* **Legacy Delphi Class**: `TMsgGameCharSitResultNtf` (Address: `005AE83C`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31118.dms
function parse(packet){
	packet.setTitle("キャラ起立通知");
	packet.readInt32("キャラID");
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameCharSitResultNtf.Create(?:?)
005AE83C    push        ebx
 005AE83D    push        esi
 005AE83E    push        edi
 005AE83F    test        dl,dl
>005AE841    je          005AE84B
 005AE843    add         esp,0FFFFFFF0
 005AE846    call        @ClassCreate
 005AE84B    mov         esi,ecx
 005AE84D    mov         ebx,edx
 005AE84F    mov         edi,eax
 005AE851    mov         dx,798E
 005AE855    mov         eax,edi
 005AE857    call        TYgPacket.WriteID
 005AE85C    mov         edx,dword ptr [esi+0E0];TChara.ID:Integer
 005AE862    mov         eax,edi
 005AE864    call        TYgPacket.WriteInt32
 005AE869    mov  
```

---

## Opcode 0x798F (31119): `MsgGameCharDirectNtf` [キャラ向き変更要求]

* **Original Japanese DMS Title**: `キャラ向き変更要求`
* **Raw DMS Script**: [`31119.dms`](../dms/31119.dms)
* **Legacy Delphi Class**: `TMsgGameCharDirectNtf` (Address: `005AE884`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31119.dms
function parse(packet){
	packet.setTitle("キャラ向き変更要求");
	packet.readInt32("キャラID");
	packet.readInt32("向き");
	packet.readInt32("向き2？");
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameCharDirectNtf.Create(Chara:TChara)
005AE884    push        ebx
 005AE885    push        esi
 005AE886    push        edi
 005AE887    test        dl,dl
>005AE889    je          005AE893
 005AE88B    add         esp,0FFFFFFF0
 005AE88E    call        @ClassCreate
 005AE893    mov         esi,ecx
 005AE895    mov         ebx,edx
 005AE897    mov         edi,eax
 005AE899    mov         dx,798F
 005AE89D    mov         eax,edi
 005AE89F    call        TYgPacket.WriteID
 005AE8A4    mov         edx,dword ptr [esi+0E0];TChara.ID:Integer
 005AE8AA    mov         eax,edi
 005AE8AC    call        TYgPacket.WriteInt32
 005AE8B1    lea  
```

---

## Opcode 0x7992 (31122): `MsgUnknown_0x7992` [タイマー開始通知]

* **Original Japanese DMS Title**: `タイマー開始通知`
* **Raw DMS Script**: [`31122.dms`](../dms/31122.dms)
* **Legacy Delphi Class**: `TMsgGameBeginTimerNtf` (Address: `005AE94C`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31122.dms
function parse(packet){
	with(packet){
		setTitle("タイマー開始通知");
		readInt32("タイマーID");
		readInt32("開始時間");
		readInt32("表示時間");
		readInt32("終了時間");
		readInt32("typeVisible");
		readInt32("typeTarget");
		readInt32("アイコンID");
		readInt32("descType");
		for(var i = 1; i <= 3; i++){
			readInt32("timeBegin" + i.toString());
		}
		for(var i = 1; i <= 3; i++){
			readInt32("color" + i.toString());
		}
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameBeginTimerNtf.Create(StartTime:Integer; TimerID:Integer; VisibleType:Integer)
005AE94C    push        ebp
 005AE94D    mov         ebp,esp
 005AE94F    push        ebx
 005AE950    push        esi
 005AE951    push        edi
 005AE952    test        dl,dl
>005AE954    je          005AE95E
 005AE956    add         esp,0FFFFFFF0
 005AE959    call        @ClassCreate
 005AE95E    mov         esi,ecx
 005AE960    mov         ebx,edx
 005AE962    mov         edi,eax
 005AE964    mov         dx,7992
 005AE968    mov         eax,edi
 005AE96A    call        TYgPacket.WriteID
 005AE96F    mov         edx,dword ptr [ebp+8]
 005AE972    mov         eax,edi
 005AE974    call     
```

---

## Opcode 0x7996 (31126): `MsgGamePushObjectReq` [プッシュオブジェクト要求]

* **Original Japanese DMS Title**: `プッシュオブジェクト要求`
* **Raw DMS Script**: [`31126.dms`](../dms/31126.dms)
* **Legacy Delphi Class**: `TMsgGamePushObjectReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31126.dms
function parse(packet){
	with(packet){
		setTitle("プッシュオブジェクト要求");
		readInt32("idObject");
	}
}
```

---

## Opcode 0x7997 (31127): `MsgUnknown_0x7997` [プッシュオブジェクト返答]

* **Original Japanese DMS Title**: `プッシュオブジェクト返答`
* **Raw DMS Script**: [`31127.dms`](../dms/31127.dms)
* **Legacy Delphi Class**: `TMsgGamePushObjectAns` (Address: `005AEB70`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31127.dms
function parse(packet){
	with(packet){
		setTitle("プッシュオブジェクト返答");
		readInt32("idChar");
		readInt32("idObject");
		readInt32("result");
		readWord("posX");
		readWord("posY");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGamePushObjectAns.Create(?:?; ?:?)
constructor TMsgGamePushObjectAns.Create(?:?; ?:?; ?:?; ?:?);
begin
 005AEB70    push        ebp
 005AEB71    mov         ebp,esp
 005AEB73    push        ebx
 005AEB74    push        esi
 005AEB75    push        edi
 005AEB76    test        dl,dl
>005AEB78    je          005AEB82
 005AEB7A    add         esp,0FFFFFFF0
 005AEB7D    call        @ClassCreate
 005AEB82    mov         esi,ecx
 005AEB84    mov         ebx,edx
 005AEB86    mov         edi,eax
 005AEB88    mov         dx,7997
 005AEB8C    mov         eax,edi
 005AEB8E    call        TYgPacket.WriteID
 005AEB93    test        esi,esi

```

---

## Opcode 0x7998 (31128): `MsgUnknown_0x7998` [プッシュオブジェクト停止通知]

* **Original Japanese DMS Title**: `プッシュオブジェクト停止通知`
* **Raw DMS Script**: [`31128.dms`](../dms/31128.dms)
* **Legacy Delphi Class**: `TMsgGameStopPushObjectNtf` (Address: `005AEBF8`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31128.dms
function parse(packet){
	with(packet){
		setTitle("プッシュオブジェクト停止通知");
		readInt32("idChar");
		readInt32("idObject");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameStopPushObjectNtf.Create(Chara:TChara; Obj:TEpisodeObject)
005AEBF8    push        ebp
 005AEBF9    mov         ebp,esp
 005AEBFB    push        ebx
 005AEBFC    push        esi
 005AEBFD    push        edi
 005AEBFE    test        dl,dl
>005AEC00    je          005AEC0A
 005AEC02    add         esp,0FFFFFFF0
 005AEC05    call        @ClassCreate
 005AEC0A    mov         esi,ecx
 005AEC0C    mov         ebx,edx
 005AEC0E    mov         edi,eax
 005AEC10    mov         dx,7998
 005AEC14    mov         eax,edi
 005AEC16    call        TYgPacket.WriteID
 005AEC1B    mov         edx,dword ptr [esi+0E0];TChara.ID:Integer
 005AEC21    mov         eax,edi
 0
```

---

## Opcode 0x799D (31133): `MsgUnknown_0x799D` [スキルエフェクト開始通知]

* **Original Japanese DMS Title**: `スキルエフェクト開始通知`
* **Raw DMS Script**: [`31133.dms`](../dms/31133.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x799D` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31133.dms
function parse(packet){
	with(packet){
		setTitle("スキルエフェクト開始通知");
		readInt32("タイプ");
		readInt32("ID");
		var cnt = readWord("vecStatusChangeEffectCount");
		for(var i = 1; i <= cnt; i++){
			readByte("StatusChangeEffect" + i.toString());
			readBinary(3, "(padding)");
		}
		readInt32("varHpRec");
		readWord("varMoveSpd");
		readWord("varAtkSpd");
		readBinary(2, "(padding)");
	}
}
```

---

## Opcode 0x799E (31134): `MsgUnknown_0x799E` [スキルエフェクト停止通知]

* **Original Japanese DMS Title**: `スキルエフェクト停止通知`
* **Raw DMS Script**: [`31134.dms`](../dms/31134.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x799E` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31134.dms
function parse(packet){
	with(packet){
		setTitle("スキルエフェクト停止通知");
		readInt32("タイプ");
		readInt32("ID");
		readWord("エフェクトタイプ");
		readBinary(2, "(padding)");
	}
}
```

---

## Opcode 0x799F (31135): `MsgGameAtkMovChangeNtf` [攻撃・移動速度変更通知]

* **Original Japanese DMS Title**: `攻撃・移動速度変更通知`
* **Raw DMS Script**: [`31135.dms`](../dms/31135.dms)
* **Legacy Delphi Class**: `TMsgGameAtkMovChangeNtf` (Address: `005AEC54`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31135.dms
function parse(packet){
	packet.setTitle("攻撃・移動速度変更通知");
	packet.readUInt32("キャラID");
	packet.readSingle("攻撃速度");
	packet.readSingle("移動速度");
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameAtkMovChangeNtf.Create(Chara:TChara)
005AEC54    push        ebx
 005AEC55    push        esi
 005AEC56    push        edi
 005AEC57    test        dl,dl
>005AEC59    je          005AEC63
 005AEC5B    add         esp,0FFFFFFF0
 005AEC5E    call        @ClassCreate
 005AEC63    mov         esi,ecx
 005AEC65    mov         ebx,edx
 005AEC67    mov         edi,eax
 005AEC69    mov         dx,799F
 005AEC6D    mov         eax,edi
 005AEC6F    call        TYgPacket.WriteID
 005AEC74    mov         edx,dword ptr [esi+0E0];TChara.ID:Integer
 005AEC7A    mov         eax,edi
 005AEC7C    call        TYgPacket.WriteInt32
 005AEC81    push 
```

---

## Opcode 0x79A0 (31136): `MsgUnknown_0x79A0` [ミニマップ変更通知]

* **Original Japanese DMS Title**: `ミニマップ変更通知`
* **Raw DMS Script**: [`31136.dms`](../dms/31136.dms)
* **Legacy Delphi Class**: `TMsgGameChgMinimapNtf` (Address: `005AECB8`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31136.dms
function parse(packet){
	with(packet){
		setTitle("ミニマップ変更通知");
		readByte("ミニマップID");
		readBinary(3, "(padding)");
		readInt32("typePcShow");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameChgMinimapNtf.Create(MinimapID:Byte; PcShowType:Integer)
005AECB8    push        ebp
 005AECB9    mov         ebp,esp
 005AECBB    push        ecx
 005AECBC    push        ebx
 005AECBD    push        esi
 005AECBE    test        dl,dl
>005AECC0    je          005AECCA
 005AECC2    add         esp,0FFFFFFF0
 005AECC5    call        @ClassCreate
 005AECCA    mov         ebx,ecx
 005AECCC    mov         byte ptr [ebp-1],dl
 005AECCF    mov         esi,eax
 005AECD1    mov         dx,79A0
 005AECD5    mov         eax,esi
 005AECD7    call        TYgPacket.WriteID
 005AECDC    movzx       edx,bl
 005AECDF    mov         eax,esi
 005AECE1    call        
```

---

## Opcode 0x79A1 (31137): `MsgUnknown_0x79A1` [称号入手通知]

* **Original Japanese DMS Title**: `称号入手通知`
* **Raw DMS Script**: [`31137.dms`](../dms/31137.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x79A1` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31137.dms
function parse(packet){
	packet.setTitle("称号入手通知");
	packet.readInt32("称号ID");
}
```

---

## Opcode 0x79A2 (31138): `MsgGameLockerOpenReq` [称号装備要求]

* **Original Japanese DMS Title**: `称号装備要求`
* **Raw DMS Script**: [`31138.dms`](../dms/31138.dms)
* **Legacy Delphi Class**: `TMsgGameLockerOpenReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31138.dms
function parse(packet){
	with(packet){
		setTitle("称号装備要求");
		readInt32("称号ID");
	}
}
```

---

## Opcode 0x79A3 (31139): `MsgGameEquipTitleAns` [称号装備返答]

* **Original Japanese DMS Title**: `称号装備返答`
* **Raw DMS Script**: [`31139.dms`](../dms/31139.dms)
* **Legacy Delphi Class**: `TMsgGameEquipTitleAns` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31139.dms
function parse(packet){
	packet.setTitle("称号装備返答");
	packet.readInt32("戻り値");
	packet.readInt32("キャラID");
	packet.readInt32("称号ID");
	packet.readInt32("bForce");
}
```

---

## Opcode 0x79A4 (31140): `MsgGameLockerCloseReq` [称号装備解除要求]

* **Original Japanese DMS Title**: `称号装備解除要求`
* **Raw DMS Script**: [`31140.dms`](../dms/31140.dms)
* **Legacy Delphi Class**: `TMsgGameLockerCloseReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31140.dms
function parse(packet){
	with(packet){
		setTitle("称号装備解除要求");
		readInt32("称号ID");
	}
}
```

---

## Opcode 0x79A5 (31141): `MsgUnknown_0x79A5` [称号装備解除返答]

* **Original Japanese DMS Title**: `称号装備解除返答`
* **Raw DMS Script**: [`31141.dms`](../dms/31141.dms)
* **Legacy Delphi Class**: `TMsgGameEquipTitleAns` (Address: `005AED84`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31141.dms
function parse(packet){
	with(packet){
		setTitle("称号装備解除返答");
		readInt32("戻り値");
		readInt32("キャラID");
		readInt32("称号ID");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameEquipTitleAns.Create(?:?)
constructor TMsgGameStripTitleAns.Create(?:?);
begin
 005AED84    push        ebp
 005AED85    mov         ebp,esp
 005AED87    push        ecx
 005AED88    push        ebx
 005AED89    push        esi
 005AED8A    push        edi
 005AED8B    test        dl,dl
>005AED8D    je          005AED97
 005AED8F    add         esp,0FFFFFFF0
 005AED92    call        @ClassCreate
 005AED97    mov         edi,ecx
 005AED99    mov         byte ptr [ebp-1],dl
 005AED9C    mov         ebx,eax
 005AED9E    mov         esi,dword ptr [ebp+8]
 005AEDA1    mov         dx,79A5
 005AEDA5    mov         eax,ebx
 00
```

---

## Opcode 0x79A8 (31144): `MsgGameEnchantCrystalReq` [結晶レベル要求]

* **Original Japanese DMS Title**: `結晶レベル要求`
* **Raw DMS Script**: [`31144.dms`](../dms/31144.dms)
* **Legacy Delphi Class**: `TMsgGameEnchantCrystalReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31144.dms
function parse(packet){
	with(packet){
		setTitle("結晶レベル要求");
		readInt32("type");
		var cnt = readWord("itemsCount");
		for(var i = 1; i <= cnt; i++){
			readInt32("  typeItem+typeNum" + i.toString());
			readInt32("  count" + i.toString());
			readInt32("  (invalid)" + i.toString());
		}
	}
}
```

---

## Opcode 0x79A9 (31145): `MsgUnknown_0x79A9` [結晶レベル返答]

* **Original Japanese DMS Title**: `結晶レベル返答`
* **Raw DMS Script**: [`31145.dms`](../dms/31145.dms)
* **Legacy Delphi Class**: `TMsgGameEnchantCrystalLevelAns` (Address: `005AEDF0`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31145.dms
function parse(packet){
	with(packet){
		setTitle("結晶レベル返答");
		readInt32("戻り値");
		readInt32("type");
		readInt32("level");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameEnchantCrystalLevelAns.Create(EnchantType:Integer; Level:Integer)
005AEDF0    push        ebp
 005AEDF1    mov         ebp,esp
 005AEDF3    push        ecx
 005AEDF4    push        ebx
 005AEDF5    push        esi
 005AEDF6    push        edi
 005AEDF7    test        dl,dl
>005AEDF9    je          005AEE03
 005AEDFB    add         esp,0FFFFFFF0
 005AEDFE    call        @ClassCreate
 005AEE03    mov         edi,ecx
 005AEE05    mov         byte ptr [ebp-1],dl
 005AEE08    mov         ebx,eax
 005AEE0A    mov         esi,dword ptr [ebp+8]
 005AEE0D    mov         dx,79A9
 005AEE11    mov         eax,ebx
 005AEE13    call        TYgPacket.WriteID
 005AEE18    t
```

---

## Opcode 0x79AA (31146): `MsgGameCrystallizeReq` [結晶精製要求]

* **Original Japanese DMS Title**: `結晶精製要求`
* **Raw DMS Script**: [`31146.dms`](../dms/31146.dms)
* **Legacy Delphi Class**: `TMsgGameCrystallizeReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31146.dms
function parse(packet){
	with(packet){
		setTitle("結晶精製要求");
		readInt32("type");
		readInt32("level");
		var cnt = readWord("itemsCount");
		for(var i = 1; i <= cnt; i++){
			readInt32("  typeItem+typeNum" + i.toString());
			readInt32("  count" + i.toString());
			readInt32("  (invalid)" + i.toString());
		}
	}
}
```

---

## Opcode 0x79AB (31147): `MsgUnknown_0x79AB` [結晶精製返答]

* **Original Japanese DMS Title**: `結晶精製返答`
* **Raw DMS Script**: [`31147.dms`](../dms/31147.dms)
* **Legacy Delphi Class**: `TMsgGameEnchantCrystallizeAns` (Address: `005AEE6C`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31147.dms
function parse(packet){
	with(packet){
		setTitle("結晶精製返答");
		readInt32("戻り値");
		var cnt = readWord("vecGoneItemsCount");
		for(var i = 1; i <= cnt; i++){
			readInt32("  typeItem+typeNum" + i.toString());
			readInt32("  count" + i.toString());
			readInt32("  (invalid)" + i.toString());
		}
		readInt32("[crystal]typeItem+typeNum");
		readInt32("[crystal]count");
		readInt32("[crystal](invalid)");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameEnchantCrystallizeAns.Create(GoneItems:TArray<UYgItem.TItemRec>; EnItem:TEnItem)
005AEE6C    push        ebp
 005AEE6D    mov         ebp,esp
 005AEE6F    push        ecx
 005AEE70    push        ebx
 005AEE71    push        esi
 005AEE72    push        edi
 005AEE73    test        dl,dl
>005AEE75    je          005AEE7F
 005AEE77    add         esp,0FFFFFFF0
 005AEE7A    call        @ClassCreate
 005AEE7F    mov         edi,ecx
 005AEE81    mov         byte ptr [ebp-1],dl
 005AEE84    mov         esi,eax
 005AEE86    mov         dx,79AB
 005AEE8A    mov         eax,esi
 005AEE8C    call        TYgPacket.WriteID
 005AEE91    mov         edx,1
 005AEE96    mov         eax,e
```

---

## Opcode 0x79B2 (31154): `MsgGamePromoteInfoNtf` [進級情報通知]

* **Original Japanese DMS Title**: `進級情報通知`
* **Raw DMS Script**: [`31154.dms`](../dms/31154.dms)
* **Legacy Delphi Class**: `TMsgGamePromoteInfoNtf` (Address: `005AEF1C`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31154.dms
function parse(packet){
	packet.setTitle("進級情報通知");
	packet.readInt32("キャラID");
	for(var i = 1; i <= 10; i++){
		packet.readWord("nTypeSN" + i.toString());
	}
	for(var i = 1; i <= 10; i++){
		packet.readWord("nCondId" + i.toString());
	}
	for(var i = 1; i <= 13; i++){
		packet.readInt32("nGradeLevel" + i.toString());
	}
	packet.readInt32("nBaseMeanValue");
	var cnt = packet.readWord("vecCondCount");
	for(var i = 1; i <= cnt; i++){
		packet.readInt32("vecCond1_" + i.toString());
		packet.readInt32("vecCond2_" + i.toString());
		packet.readInt32("vecCond3_" + i.toString());
	}
	packet.readInt32();
	packet.readInt32();
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGamePromoteInfoNtf.Create(Chara:TChara)
005AEF1C    push        ebx
 005AEF1D    push        esi
 005AEF1E    push        edi
 005AEF1F    test        dl,dl
>005AEF21    je          005AEF2B
 005AEF23    add         esp,0FFFFFFF0
 005AEF26    call        @ClassCreate
 005AEF2B    mov         esi,ecx
 005AEF2D    mov         ebx,edx
 005AEF2F    mov         edi,eax
 005AEF31    mov         dx,79B2
 005AEF35    mov         eax,edi
 005AEF37    call        TYgPacket.WriteID
 005AEF3C    mov         edx,dword ptr [esi+0E0];TChara.ID:Integer
 005AEF42    mov         eax,edi
 005AEF44    call        TYgPacket.WriteInt32
 005AEF49    mov  
```

---

## Opcode 0x79B5 (31157): `MsgUnknown_0x79B5` [エピソード情報通知]

* **Original Japanese DMS Title**: `エピソード情報通知`
* **Raw DMS Script**: [`31157.dms`](../dms/31157.dms)
* **Legacy Delphi Class**: `TMsgGameEpisodeInfoNtf` (Address: `005AF100`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31157.dms
function parse(packet){
	with(packet){
		setTitle("エピソード情報通知");
		readInt32("typeEpisode");
		readInt32("mode");
		readInt32("bPK");
		readWStr(readWord("title len"), "title");
		readInt32("cntLimitMilk");
		readInt32("cntTotalRevival");
		readInt32("cntFreeRevival");
		readInt32("curTotalRevival");
		readInt32("curFreeRevival");
		var cnt = readWord("vecEpisodePC");
		for(var i = 0; i < cnt; i++){
			readInt32("├idChar");
			readWStr(0x1A, "├name");
			readByte("├grade");
			readByte("├weapon");
			readWord("├idTeam");
			readBinary(2, "├(padding)");
			readInt32("├phone");
			readInt32("└idPromotion");
		}
		readSingle("scheduleCalorieEnter");
		readSingle("scheduleCalorieConsume");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameEpisodeInfoNtf.Create(Chara:TChara)
005AF100    push        ebp
 005AF101    mov         ebp,esp
 005AF103    add         esp,0FFFFFFF4
 005AF106    push        ebx
 005AF107    push        esi
 005AF108    push        edi
 005AF109    test        dl,dl
>005AF10B    je          005AF115
 005AF10D    add         esp,0FFFFFFF0
 005AF110    call        @ClassCreate
 005AF115    mov         dword ptr [ebp-8],ecx
 005AF118    mov         byte ptr [ebp-1],dl
 005AF11B    mov         ebx,eax
 005AF11D    mov         dx,79B5
 005AF121    mov         eax,ebx
 005AF123    call        TYgPacket.WriteID
 005AF128    mov         eax,dword pt
```

---

## Opcode 0x79BA (31162): `MsgUnknown_0x79BA` [オブジェクト破壊通知]

* **Original Japanese DMS Title**: `オブジェクト破壊通知`
* **Raw DMS Script**: [`31162.dms`](../dms/31162.dms)
* **Legacy Delphi Class**: `TMsgGameDestroyObjectNtf` (Address: `005AF5DC`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31162.dms
function parse(packet){
	with(packet){
		setTitle("オブジェクト破壊通知");
		readInt32("idObject");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameDestroyObjectNtf.Create(ObjectID:Integer)
005AF5DC    push        ebx
 005AF5DD    push        esi
 005AF5DE    push        edi
 005AF5DF    test        dl,dl
>005AF5E1    je          005AF5EB
 005AF5E3    add         esp,0FFFFFFF0
 005AF5E6    call        @ClassCreate
 005AF5EB    mov         esi,ecx
 005AF5ED    mov         ebx,edx
 005AF5EF    mov         edi,eax
 005AF5F1    mov         dx,79BA
 005AF5F5    mov         eax,edi
 005AF5F7    call        TYgPacket.WriteID
 005AF5FC    mov         edx,esi
 005AF5FE    mov         eax,edi
 005AF600    call        TYgPacket.WriteInt32
 005AF605    mov         eax,edi
 005AF607    test  
```

---

## Opcode 0x79BB (31163): `MsgUnknown_0x79BB` [オブジェクト消去通知]

* **Original Japanese DMS Title**: `オブジェクト消去通知`
* **Raw DMS Script**: [`31163.dms`](../dms/31163.dms)
* **Legacy Delphi Class**: `TMsgGameEraseObjectNtf` (Address: `005AF620`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31163.dms
function parse(packet){
	with(packet){
		setTitle("オブジェクト消去通知");
		readInt32("idObject");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameEraseObjectNtf.Create(ObjectID:Integer)
005AF620    push        ebx
 005AF621    push        esi
 005AF622    push        edi
 005AF623    test        dl,dl
>005AF625    je          005AF62F
 005AF627    add         esp,0FFFFFFF0
 005AF62A    call        @ClassCreate
 005AF62F    mov         esi,ecx
 005AF631    mov         ebx,edx
 005AF633    mov         edi,eax
 005AF635    mov         dx,79BB
 005AF639    mov         eax,edi
 005AF63B    call        TYgPacket.WriteID
 005AF640    mov         edx,esi
 005AF642    mov         eax,edi
 005AF644    call        TYgPacket.WriteInt32
 005AF649    mov         eax,edi
 005AF64B    test  
```

---

## Opcode 0x79C2 (31170): `MsgGameSpecialPhoneCallReq` [特殊番号コール要求]

* **Original Japanese DMS Title**: `特殊番号コール要求`
* **Raw DMS Script**: [`31170.dms`](../dms/31170.dms)
* **Legacy Delphi Class**: `TMsgGameSpecialPhoneCallReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31170.dms
function parse(packet){
	with(packet){
		setTitle("特殊番号コール要求");
		readInt32("電話番号");
	}
}
```

---

## Opcode 0x79C3 (31171): `MsgUnknown_0x79C3` [特殊番号コール返答]

* **Original Japanese DMS Title**: `特殊番号コール返答`
* **Raw DMS Script**: [`31171.dms`](../dms/31171.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x79C3` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31171.dms
function parse(packet){
	with(packet){
		setTitle("特殊番号コール返答");
		readInt32("戻り値");
	}
}
```

---

## Opcode 0x79C9 (31177): `MsgUnknown_0x79C9` [ランダムボックス結果通知]

* **Original Japanese DMS Title**: `ランダムボックス結果通知`
* **Raw DMS Script**: [`31177.dms`](../dms/31177.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x79C9` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31177.dms
function parse(packet){
	with(packet){
		setTitle("ランダムボックス結果通知");
		readInt32("ItemType");
		readInt32("Item???(数？)");
		readInt32("Item???");
	}
}
```

---

## Opcode 0x79D3 (31187): `MsgGameBroadcastAOINtf` [対象領域へアクション通知]

* **Original Japanese DMS Title**: `対象領域へアクション通知`
* **Raw DMS Script**: [`31187.dms`](../dms/31187.dms)
* **Legacy Delphi Class**: `TMsgGameBDataToAOINtf` (Address: `005AF760`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31187.dms
function parse(packet){
	with(packet){
		setTitle("対象領域へアクション通知");
		readInt32();
		readInt32("キャラID");
		readInt32();
		readInt32("idCommand");
		readInt32("idMotion");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameBDataToAOINtf.Create(BData:TBDataPacketRec)
005AF760    push        ebx
 005AF761    push        esi
 005AF762    push        edi
 005AF763    test        dl,dl
>005AF765    je          005AF76F
 005AF767    add         esp,0FFFFFFF0
 005AF76A    call        @ClassCreate
 005AF76F    mov         esi,ecx
 005AF771    mov         ebx,edx
 005AF773    mov         edi,eax
 005AF775    mov         dx,79D3
 005AF779    mov         eax,edi
 005AF77B    call        TYgPacket.WriteID
 005AF780    mov         edx,esi
 005AF782    mov         ecx,14
 005AF787    mov         eax,edi
 005AF789    call        TStream.WriteBuffer
 005AF78E    mov     
```

---

## Opcode 0x79D4 (31188): `MsgGameZoneNameNtf` [移動間隔通知]

* **Original Japanese DMS Title**: `移動間隔通知`
* **Raw DMS Script**: [`31188.dms`](../dms/31188.dms)
* **Legacy Delphi Class**: `TMsgGameMoveIntervalNtf` (Address: `005AF7AC`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31188.dms
function parse(packet){
	packet.setTitle("移動間隔通知");
	packet.readInt32("移動間隔");
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameMoveIntervalNtf.Create(Interval:Integer)
005AF7AC    push        ebx
 005AF7AD    push        esi
 005AF7AE    push        edi
 005AF7AF    test        dl,dl
>005AF7B1    je          005AF7BB
 005AF7B3    add         esp,0FFFFFFF0
 005AF7B6    call        @ClassCreate
 005AF7BB    mov         esi,ecx
 005AF7BD    mov         ebx,edx
 005AF7BF    mov         edi,eax
 005AF7C1    mov         dx,79D4
 005AF7C5    mov         eax,edi
 005AF7C7    call        TYgPacket.WriteID
 005AF7CC    mov         edx,esi
 005AF7CE    mov         eax,edi
 005AF7D0    call        TYgPacket.WriteInt32
 005AF7D5    mov         eax,edi
 005AF7D7    test  
```

---

## Opcode 0x79D5 (31189): `MsgGameMoveExNtf` [拡張移動通知]

* **Original Japanese DMS Title**: `拡張移動通知`
* **Raw DMS Script**: [`31189.dms`](../dms/31189.dms)
* **Legacy Delphi Class**: `TMsgGameMoveExNtf` (Address: `005AF7F0`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31189.dms
function parse(packet){
	packet.setTitle("拡張移動通知");
	packet.readInt32("idChar");
	packet.readWord("px");
	packet.readWord("py");
	packet.readChar("dx");
	packet.readChar("dy");
	packet.readBinary(2, "(padding)");
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameMoveExNtf.Create(Chara:TChara)
005AF7F0    push        ebx
 005AF7F1    push        esi
 005AF7F2    push        edi
 005AF7F3    test        dl,dl
>005AF7F5    je          005AF7FF
 005AF7F7    add         esp,0FFFFFFF0
 005AF7FA    call        @ClassCreate
 005AF7FF    mov         esi,ecx
 005AF801    mov         ebx,edx
 005AF803    mov         edi,eax
 005AF805    mov         dx,79D5
 005AF809    mov         eax,edi
 005AF80B    call        TYgPacket.WriteID
 005AF810    mov         edx,dword ptr [esi+0E0];TChara.ID:Integer
 005AF816    mov         eax,edi
 005AF818    call        TYgPacket.WriteInt32
 005AF81D    mov  
```

---

## Opcode 0x79E3 (31203): `MsgUnknown_0x79E3` [戦利品箱割り当て通知]

* **Original Japanese DMS Title**: `戦利品箱割り当て通知`
* **Raw DMS Script**: [`31203.dms`](../dms/31203.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x79E3` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31203.dms
function parse(packet){
	with(packet){
		setTitle("戦利品箱割り当て通知");
		readInt32("キャラID");
		readInt32("戦利品箱ID");
	}
}
```

---

## Opcode 0x79E4 (31204): `MsgUnknown_0x79E4` [定期プレイヤーHP情報通知]

* **Original Japanese DMS Title**: `定期プレイヤーHP情報通知`
* **Raw DMS Script**: [`31204.dms`](../dms/31204.dms)
* **Legacy Delphi Class**: `TMsgGamePeriodicPlayerHpInfoNtf` (Address: `005AF924`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31204.dms
import struct.*;
function parse(packet){
	with(packet){
		setTitle("定期プレイヤーHP情報通知");
		var cnt = readWord("vecHpInfoCount");
		player_hp_info(packet, cnt);
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGamePeriodicPlayerHpInfoNtf.Create(Instance:TEpisodeInstance; TeamID:Byte)
005AF924    push        ebp
 005AF925    mov         ebp,esp
 005AF927    add         esp,0FFFFFFEC
 005AF92A    push        ebx
 005AF92B    push        esi
 005AF92C    push        edi
 005AF92D    xor         ebx,ebx
 005AF92F    mov         dword ptr [ebp-4],ebx
 005AF932    mov         dword ptr [ebp-8],ebx
 005AF935    test        dl,dl
>005AF937    je          005AF941
 005AF939    add         esp,0FFFFFFF0
 005AF93C    call        @ClassCreate
 005AF941    mov         ebx,ecx
 005AF943    mov         byte ptr [ebp-0D],dl
 005AF946    mov         dword ptr [ebp-0C],eax
 005AF949    xor 
```

---

## Opcode 0x79E5 (31205): `MsgUnknown_0x79E5` [プレイヤーステータス通知]

* **Original Japanese DMS Title**: `プレイヤーステータス通知`
* **Raw DMS Script**: [`31205.dms`](../dms/31205.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x79E5` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31205.dms
function parse(packet){
	with(packet){
		setTitle("プレイヤーステータス通知");
		readInt32("idChar");
		readInt32("type");
		readInt32("bStart");
	}
}
```

---

## Opcode 0x79E8 (31208): `MsgUnknown_0x79E8` [モンスターHP情報通知]

* **Original Japanese DMS Title**: `モンスターHP情報通知`
* **Raw DMS Script**: [`31208.dms`](../dms/31208.dms)
* **Legacy Delphi Class**: `TMsgGameMonHpInfoNtf` (Address: `005AFAE4`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31208.dms
function parse(packet){
	with(packet){
		setTitle("モンスターHP情報通知");
		readInt32("idMonster");
		readInt32("hpCurrent");
		readInt32("hpMax");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameMonHpInfoNtf.Create(Monster:TMonster)
005AFAE4    push        ebx
 005AFAE5    push        esi
 005AFAE6    push        edi
 005AFAE7    test        dl,dl
>005AFAE9    je          005AFAF3
 005AFAEB    add         esp,0FFFFFFF0
 005AFAEE    call        @ClassCreate
 005AFAF3    mov         esi,ecx
 005AFAF5    mov         ebx,edx
 005AFAF7    mov         edi,eax
 005AFAF9    mov         dx,79E8
 005AFAFD    mov         eax,edi
 005AFAFF    call        TYgPacket.WriteID
 005AFB04    mov         edx,dword ptr [esi+10];TMonster.FId:Integer
 005AFB07    mov         eax,edi
 005AFB09    call        TYgPacket.WriteInt32
 005AFB0E    mov
```

---

## Opcode 0x79E9 (31209): `MsgUnknown_0x79E9` [イベントシステム参加通知]

* **Original Japanese DMS Title**: `イベントシステム参加通知`
* **Raw DMS Script**: [`31209.dms`](../dms/31209.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x79E9` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31209.dms
function parse(packet){
	with(packet){
		setTitle("イベントシステム参加通知");
		readInt32("戻り値");
		var cnt = readWord("vecEventCount");
		for(var i = 1; i <= cnt; i++){
			readInt32();
			readInt32();
			readInt32();
			readWStr(0x32, "str");
			readWStr(0x202, "str");
			readWStr(0x42, "str");
			readBinary(2, "(padding)");
		}
	}
}
```

---

## Opcode 0x79EC (31212): `MsgUnknown_0x79EC` [クーポン結果通知]

* **Original Japanese DMS Title**: `クーポン結果通知`
* **Raw DMS Script**: [`31212.dms`](../dms/31212.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x79EC` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31212.dms
import struct.*;
function parse(packet){
	with(packet){
		setTitle("クーポン結果通知");
		readInt32("戻り値");
		readInt32("eventSN");
		readWStr(0x22, "coupon");
		readBinary(2, "(padding)");
		readItem(packet);
		readInt32("limitInputCount");
	}
}
```

---

## Opcode 0x79ED (31213): `MsgUnknown_0x79ED` [プレゼントボックス結果通知]

* **Original Japanese DMS Title**: `プレゼントボックス結果通知`
* **Raw DMS Script**: [`31213.dms`](../dms/31213.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x79ED` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31213.dms
import struct.*;
function parse(packet){
	with(packet){
		setTitle("プレゼントボックス結果通知");
		readInt32("戻り値");
		var cnt = readWord("vecItemsCount");
		for(var i = 1; i <= cnt; i++){
			//readInt32();
			//readWord("Index1");
			//readWord("Index2");
			//readInt32("ObjectID");
			readItem(packet, false);
		}
		readInt32("reqBeSize");
		readInt32("reqCoSize");
	}
}
```

---

## Opcode 0x79EE (31214): `MsgUnknown_0x79EE` [武器熟練度アップ通知]

* **Original Japanese DMS Title**: `武器熟練度アップ通知`
* **Raw DMS Script**: [`31214.dms`](../dms/31214.dms)
* **Legacy Delphi Class**: `TMsgGameCharDexLvUpNtf` (Address: `005AFB44`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31214.dms
function parse(packet){
	with(packet){
		setTitle("武器熟練度アップ通知");
		readInt32("キャラID");
		readInt32("戻り値");
		readInt32("レベル");
		readInt32();
		readInt32("次レベルアップ値");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameCharDexLvUpNtf.Create(Chara:TChara)
005AFB44    push        ebx
 005AFB45    push        esi
 005AFB46    push        edi
 005AFB47    test        dl,dl
>005AFB49    je          005AFB53
 005AFB4B    add         esp,0FFFFFFF0
 005AFB4E    call        @ClassCreate
 005AFB53    mov         esi,ecx
 005AFB55    mov         ebx,edx
 005AFB57    mov         edi,eax
 005AFB59    mov         dx,79EE
 005AFB5D    mov         eax,edi
 005AFB5F    call        TYgPacket.WriteID
 005AFB64    mov         edx,dword ptr [esi+0E0];TChara.ID:Integer
 005AFB6A    mov         eax,edi
 005AFB6C    call        TYgPacket.WriteInt32
 005AFB71    mov  
```

---

## Opcode 0x79EF (31215): `MsgUnknown_0x79EF` [源石装着要求]

* **Original Japanese DMS Title**: `源石装着要求`
* **Raw DMS Script**: [`31215.dms`](../dms/31215.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x79EF` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31215.dms
function parse(packet){
	with(packet){
		setTitle("源石装着要求");
		readInt32("[beitem]typeItem+typeNum");
		readWord("[beitem]Dim1Index");
		readWord("[beitem]Dim2Index");
		readInt32("[beitem]idItem");
		for(var i = 0; i < 5; i++)
			readInt32("[beitem]reinforceslot[" + i.toString() + "]");
		readInt32("[book]typeItem+typeNum");
		readInt32("[book]count");
		readInt32("[book](invalid)");
		readInt32("[stone]typeItem+typeNum");
		readInt32("[stone]count");
		readInt32("[stone](invalid)");
		readInt32("slot");
	}
}
```

---

## Opcode 0x79F0 (31216): `MsgUnknown_0x79F0` [源石装着返答]

* **Original Japanese DMS Title**: `源石装着返答`
* **Raw DMS Script**: [`31216.dms`](../dms/31216.dms)
* **Legacy Delphi Class**: `TMsgGameReinforceBeItemAttachStoneAns` (Address: `005AFBF0`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31216.dms
function parse(packet){
	with(packet){
		setTitle("源石装着返答");
		readInt32("戻り値");
		readInt32("キャラID");
		var cnt = readWord("vecGoneItemsCount");
		for(var i = 1; i <= cnt; i++){
			readInt32("  typeItem+typeNum" + i.toString());
			readInt32("  count" + i.toString());
			readInt32("  (invalid)" + i.toString());
		}
		readInt32("typeItem+typeNum");
		readWord("Dim1Index");
		readWord("Dim2Index");
		readInt32("idItem");
		for(var i = 0; i < 5; i++)
			readInt32("reinforceslot[" + i.toString() + "]");
		readInt32("remainTimeInSecond");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameReinforceBeItemAttachStoneAns.Create(Chara:TChara; Item:TEquippableItem; Data:TReinforceBeItemStonePacketRec)
005AFBF0    push        ebp
 005AFBF1    mov         ebp,esp
 005AFBF3    push        ecx
 005AFBF4    push        ebx
 005AFBF5    push        esi
 005AFBF6    push        edi
 005AFBF7    test        dl,dl
>005AFBF9    je          005AFC03
 005AFBFB    add         esp,0FFFFFFF0
 005AFBFE    call        @ClassCreate
 005AFC03    mov         ebx,ecx
 005AFC05    mov         byte ptr [ebp-1],dl
 005AFC08    mov         esi,eax
 005AFC0A    mov         dx,79F0
 005AFC0E    mov         eax,esi
 005AFC10    call        TYgPacket.WriteID
 005AFC15    mov         edx,1
 005AFC1A    mov         eax,e
```

---

## Opcode 0x79FB (31227): `MsgUnknown_0x79FB` [販売スキルリスト通知]

* **Original Japanese DMS Title**: `販売スキルリスト通知`
* **Raw DMS Script**: [`31227.dms`](../dms/31227.dms)
* **Legacy Delphi Class**: `TMsgGameNpcDialogSaleSkillListNtf` (Address: `005B0000`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31227.dms
function parse(packet){
	with(packet){
		setTitle("販売スキルリスト通知");
		readInt32("idNpc");
		var cnt = readWord("vecSkillCount");
		for(var i = 1; i <= cnt; i++){
			readInt32("typeItem");
			readInt64("seed");
			readInt32();
		}
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameNpcDialogSaleSkillListNtf.Create(NpcId:Integer; SkillList:TNpcSkillList; Chara:TChara)
005B0000    push        ebp
 005B0001    mov         ebp,esp
 005B0003    add         esp,0FFFFFFF4
 005B0006    push        ebx
 005B0007    push        esi
 005B0008    push        edi
 005B0009    test        dl,dl
>005B000B    je          005B0015
 005B000D    add         esp,0FFFFFFF0
 005B0010    call        @ClassCreate
 005B0015    mov         ebx,ecx
 005B0017    mov         byte ptr [ebp-5],dl
 005B001A    mov         dword ptr [ebp-4],eax
 005B001D    mov         esi,dword ptr [ebp+8]
 005B0020    mov         dx,79FB
 005B0024    mov         eax,dword ptr [ebp-4]
 005B0027    call  
```

---

## Opcode 0x79FC (31228): `MsgUnknown_0x79FC` [スキルホットキー設定通知]

* **Original Japanese DMS Title**: `スキルホットキー設定通知`
* **Raw DMS Script**: [`31228.dms`](../dms/31228.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x79FC` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31228.dms
function parse(packet){
	with(packet){
		setTitle("スキルホットキー設定通知");
		readWord("武器カテゴリ");
		readWord("インデックス");
		readInt32("攻撃スキルID");
	}
}
```

---

## Opcode 0x79FD (31229): `MsgUnknown_0x79FD` [MSG_GAME_PASSIVE_EFFECT_START_NTF(常時エフェクト表示開始？)]

* **Original Japanese DMS Title**: `MSG_GAME_PASSIVE_EFFECT_START_NTF(常時エフェクト表示開始？)`
* **Raw DMS Script**: [`31229.dms`](../dms/31229.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x79FD` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31229.dms
function parse(packet){
	with(packet){
		setTitle("MSG_GAME_PASSIVE_EFFECT_START_NTF(常時エフェクト表示開始？)");
		var mgpesCnt = readWord("エフェクト数");
		for(var i=1 ; i<=mgpesCnt ; i++) {
			readByte( "  Unknown" + i.toString() );
			readBinary( 3, "  (padding" + i.toString() + ")");
			readUInt32( "  Unknown" + i.toString());
		}
	}
}
```

---

## Opcode 0x7A00 (31232): `MsgGameMonDeadNtf` [モンスター所有権獲得通知]

* **Original Japanese DMS Title**: `モンスター所有権獲得通知`
* **Raw DMS Script**: [`31232.dms`](../dms/31232.dms)
* **Legacy Delphi Class**: `TMsgGameMonsterOwnershipAcquiredNtf` (Address: `005B0154`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31232.dms
function parse(packet){
	with(packet){
		setTitle("モンスター所有権獲得通知");
		readInt32("モンスターID");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameMonsterOwnershipAcquiredNtf.Create(Monster:TMonster)
005B0154    push        ebx
 005B0155    push        esi
 005B0156    push        edi
 005B0157    test        dl,dl
>005B0159    je          005B0163
 005B015B    add         esp,0FFFFFFF0
 005B015E    call        @ClassCreate
 005B0163    mov         esi,ecx
 005B0165    mov         ebx,edx
 005B0167    mov         edi,eax
 005B0169    mov         dx,7A00
 005B016D    mov         eax,edi
 005B016F    call        TYgPacket.WriteID
 005B0174    mov         edx,dword ptr [esi+10];TMonster.FId:Integer
 005B0177    mov         eax,edi
 005B0179    call        TYgPacket.WriteInt32
 005B017E    mov
```

---

## Opcode 0x7A01 (31233): `MsgUnknown_0x7A01` [モンスター所有権喪失通知]

* **Original Japanese DMS Title**: `モンスター所有権喪失通知`
* **Raw DMS Script**: [`31233.dms`](../dms/31233.dms)
* **Legacy Delphi Class**: `TMsgGameMonsterOwnershipLostNtf` (Address: `005B019C`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31233.dms
function parse(packet){
	with(packet){
		setTitle("モンスター所有権喪失通知");
		readInt32("モンスターID");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameMonsterOwnershipLostNtf.Create(Monster:TMonster)
005B019C    push        ebx
 005B019D    push        esi
 005B019E    push        edi
 005B019F    test        dl,dl
>005B01A1    je          005B01AB
 005B01A3    add         esp,0FFFFFFF0
 005B01A6    call        @ClassCreate
 005B01AB    mov         esi,ecx
 005B01AD    mov         ebx,edx
 005B01AF    mov         edi,eax
 005B01B1    mov         dx,7A01
 005B01B5    mov         eax,edi
 005B01B7    call        TYgPacket.WriteID
 005B01BC    mov         edx,dword ptr [esi+10];TMonster.FId:Integer
 005B01BF    mov         eax,edi
 005B01C1    call        TYgPacket.WriteInt32
 005B01C6    mov
```

---

## Opcode 0x7A04 (31236): `MsgUnknown_0x7A04` [アイテム廃棄通知]

* **Original Japanese DMS Title**: `アイテム廃棄通知`
* **Raw DMS Script**: [`31236.dms`](../dms/31236.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x7A04` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/31236.dms
function parse(packet){
	with(packet){
		setTitle("アイテム廃棄通知");
		readInt32("typeItem+typeNum");
		var dim1 = readWord("idBeItemDim1Index");
		var dim2 = readWord("idBeItemDim2Index");
		readInt32("idBeItem[" + dim1.toString() + "][" + dim2.toString() + "]");
	}
}
```

---

## Opcode 0xA029 (41001): `MsgUnknown_0xA029` [ロッカーENTER通知]

* **Original Japanese DMS Title**: `ロッカーENTER通知`
* **Raw DMS Script**: [`41001.dms`](../dms/41001.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0xA029` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/41001.dms
function parse(packet){
	with(packet){
		setTitle("ロッカーENTER通知");
		var cnt = readWord("vecLockerCount");
		for(var i = 1; i <= cnt; i++){
		
		}
	}
}
```

---

## Opcode 0xA02A (41002): `MsgUnknown_0xA02A` [ロッカー終了通知]

* **Original Japanese DMS Title**: `ロッカー終了通知`
* **Raw DMS Script**: [`41002.dms`](../dms/41002.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0xA02A` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/41002.dms
function parse(packet){
	with(packet){
		setTitle("ロッカー終了通知");
	}
}
```

---

## Opcode 0xA02B (41003): `MsgUnknown_0xA02B` [ロッカーオープン要求]

* **Original Japanese DMS Title**: `ロッカーオープン要求`
* **Raw DMS Script**: [`41003.dms`](../dms/41003.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0xA02B` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/41003.dms
function parse(packet){
	with(packet){
		setTitle("ロッカーオープン要求");
		readInt32("lockerID");
	}
}
```

---

## Opcode 0xA02C (41004): `MsgUnknown_0xA02C` [ロッカーオープン返答]

* **Original Japanese DMS Title**: `ロッカーオープン返答`
* **Raw DMS Script**: [`41004.dms`](../dms/41004.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0xA02C` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/41004.dms
function parse(packet){
	with(packet){
		setTitle("ロッカーオープン返答");
		readInt32("open");
		readInt32("lockerID");
	}
}
```

---

## Opcode 0xA02D (41005): `MsgUnknown_0xA02D` [ロッカーアイテム情報通知]

* **Original Japanese DMS Title**: `ロッカーアイテム情報通知`
* **Raw DMS Script**: [`41005.dms`](../dms/41005.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0xA02D` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/41005.dms
function parse(packet){
	with(packet){
		setTitle("ロッカーアイテム情報通知");
		readInt32("lockerID");
		var cnt = readWord("vecBeItemCount");
		for(var i = 1; i <= cnt; i++){
			readWord("  dim1Index" + i.toString());
			readWord("  dim2Index" + i.toString());
			readInt32("  idItem" + i.toString());
			readInt32("  typeBeItem" + i.toString());
			readInt32("   reinforceslot[0]" + i.toString());
			readInt32("   reinforceslot[1]" + i.toString());
			readInt32("   reinforceslot[2]" + i.toString());
			readInt32("   reinforceslot[3]" + i.toString());
			readInt32("   reinforceslot[4]" + i.toString());
		}
		var cnt = readWord("vecCoItemCount");
		for(var i = 1; i <= cnt; i++){
			readInt32("  typeCoItem" + i.toString());
			readInt32("  count" + i.toString());
		}
		var cnt = readWord("vecEnItemCount");
		for(var i = 1; i <= cnt; i++){
			readInt32("  typeEnItem" + i.toString());
			readInt32("  count" + i.toString());
		}
	}
}
```

---

## Opcode 0xA02F (41007): `MsgUnknown_0xA02F` [ロッカーアイテム移動通知]

* **Original Japanese DMS Title**: `ロッカーアイテム移動通知`
* **Raw DMS Script**: [`41007.dms`](../dms/41007.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0xA02F` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/41007.dms
function parse(packet){
	with(packet){
		setTitle("ロッカーアイテム移動通知");
		readInt32("lockerID");
		readByte("direct");
		readBinary(3, "(padding)");
		switch(readInt32("typeItem+typeNum") & 0xFF000000){
			case 0x02000000: //BeItem
				readWord("├dim1Index");
				readWord("├dim2Index");
				readInt32("├idItem");
				readInt32("├reinforceslot[0]");
				readInt32("├reinforceslot[1]");
				readInt32("├reinforceslot[2]");
				readInt32("├reinforceslot[3]");
				readInt32("└reinforceslot[4]");
				break;
			case 0x03000000: //CoItem
				readInt64("└count");
				readBinary(20, "(invalid)");
				break;
			case 0x05000000: //EnItem
				readInt64("└count");
				readBinary(20, "(invalid)");
				break;
		}
	}
}
```

---

## Opcode 0xA030 (41008): `MsgUnknown_0xA030` [ロッカーアイテム移動完了通知]

* **Original Japanese DMS Title**: `ロッカーアイテム移動完了通知`
* **Raw DMS Script**: [`41008.dms`](../dms/41008.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0xA030` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/41008.dms
function parse(packet){
	with(packet){
		setTitle("ロッカーアイテム移動完了通知");
		readInt32("戻り値");
		readInt32("lockerID");
		readByte("direct");
		readBinary(3, "(padding)");
		switch(readInt32("typeItem+typeNum") & 0xFF000000){
			case 0x02000000: //BeItem
				readWord("├dim1Index");
				readWord("├dim2Index");
				readInt32("├idItem");
				readInt32("├reinforceslot[0]");
				readInt32("├reinforceslot[1]");
				readInt32("├reinforceslot[2]");
				readInt32("├reinforceslot[3]");
				readInt32("└reinforceslot[4]");
				break;
			case 0x03000000: //CoItem
				readInt64("└count");
				readBinary(20, "(invalid)");
				break;
			case 0x05000000: //EnItem
				readInt64("└count");
				readBinary(20, "(invalid)");
				break;
		}
	}
}
```

---
