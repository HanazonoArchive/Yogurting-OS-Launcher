# Volume 9: Series 30200 & 30500: CommServer, Friends, Whispers & Club Roster

This chapter provides the complete, byte-for-byte specifications for all 16 opcodes in this range.

---

## Opcode 0x7604 (30212): `MsgTransJoinCmsAns` [チャット鯖参加返答]

* **Original Japanese DMS Title**: `チャット鯖参加返答`
* **Raw DMS Script**: [`30212.dms`](../dms/30212.dms)
* **Legacy Delphi Class**: `TMsgTransJoinCmsAns` (Address: `005AAEB8`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30212.dms
import struct.*;
import struct35.*;
function parse(packet){
	packet.setTitle("チャット鯖参加返答");
	packet.readUInt32("isReg");
	var fricnt = packet.readWord("友達リスト数");
	for(var i = 1; i <= fricnt; i++){
		packet.readUInt32("  キャラID" + i.toString());
		packet.readUInt32("  電話番号" + i.toString());
		packet.readWStr(0x44, "  キャラ名" + i.toString());
		packet.readUInt32("  下校/登校状態" + i.toString());
	}
	var memocnt = packet.readWord("メッセージ記録数");
	for(var i =1; i <= memocnt ; i++) {
		packet.readWord("  Unknown" + i.toString());
		packet.readWord("  Unknown" + i.toString());
		packet.readUInt32("  Unknown" + i.toString());
		packet.readBinary(4, "  Unknown" + i.toString());
		packet.readUInt32("  キャラID" + i.toString());
		packet.readUInt32("  電話番号" + i.toString());
		packet.readBinary(64,"  キャラ名" + i.toString());
		packet.readBinary(2, "  (padding)" + i.toString());
		packet.readWStr(202, "  メッセージ内容" + i.toString());
	}
	var cnt = packet.readWord("通話記録数");
	for(var i = 1; i <= cnt; i++){
		packet.readUInt32("  発信/着信タイプ" + i.toString());
		packet.readWord("  Unknown" + i.toString());
		packet.readWord("  Unknown" + i.toString());
		packet.readUInt32("  Unknown" + i.toString());
		packet.readUInt32("  キャラID" + i.toString());
		packet.readUInt32("  電話番号" + i.toString());
		packet.readWStr(0x44, "  キャラ名" + i.toString());
	}
	packet.readWord("受信拒否リスト数");
	for(var i = 1; i <= cnt; i++){
	
	}
	packet.readUInt32();
	packet.readUInt32();
	packet.readUInt32();
	packet.readUInt32();
	packet.readUInt32();
	packet.readUInt32();
	packet.readUInt32();
	packet.readWord();
	packet.readWord();
	packet.readUInt32();
	packet.readInt32();
	packet.readByte();
	packet.readBinary(3, "(padding)");
	packet.readUInt32("同好会結成年月日");
	packet.readUInt32();
	packet.readInt32("キャラID");
	packet.readInt32("電話番号");
	packet.readWStr(0x44, "キャラ名");
	packet.readUInt32();
	packet.readUInt32();
	packet.readUInt32();
	packet.readUInt32();
	char_brief_info(packet);
	packet.readWord("年");
	packet.readWord("月");
	packet.readWord("曜日");
	packet.readWord("日");
	packet.readWord("時");
	packet.readWord("分");
	packet.readWord("秒");
	packet.readWord("ミリ秒");
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgTransJoinCmsAns.Create(Chara:TChara)
005AAEB8    push        ebp
 005AAEB9    mov         ebp,esp
 005AAEBB    add         esp,0FFFFFFD8
 005AAEBE    push        ebx
 005AAEBF    push        esi
 005AAEC0    test        dl,dl
>005AAEC2    je          005AAECC
 005AAEC4    add         esp,0FFFFFFF0
 005AAEC7    call        @ClassCreate
 005AAECC    mov         dword ptr [ebp-0C],ecx
 005AAECF    mov         byte ptr [ebp-5],dl
 005AAED2    mov         dword ptr [ebp-4],eax
 005AAED5    call        Now
 005AAEDA    add         esp,0FFFFFFF8
 005AAEDD    fstp        qword ptr [esp]
 005AAEE0    wait
 005AAEE1    lea         eax,[ebp
```

---

## Opcode 0x772B (30507): `MsgUnknown_0x772B` [友達登録申請要求]

* **Original Japanese DMS Title**: `友達登録申請要求`
* **Raw DMS Script**: [`30507.dms`](../dms/30507.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x772B` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30507.dms
function parse(packet){
	with(packet){
		setTitle("友達登録申請要求");
		readInt32("requesterID");
		readInt32("requesterPhone");
		readWStr(0x40, "requesterName");
		readBinary(4, "(padding)");
	}
}
```

---

## Opcode 0x772C (30508): `MsgUnknown_0x772C` [友達登録申請返答]

* **Original Japanese DMS Title**: `友達登録申請返答`
* **Raw DMS Script**: [`30508.dms`](../dms/30508.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x772C` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30508.dms
function parse(packet){
	with(packet){
		setTitle("友達登録申請返答");
		readInt32("戻り値");
		readInt32("requesterID");
	}
}
```

---

## Opcode 0x772D (30509): `MsgUnknown_0x772D` [友達登録通知]

* **Original Japanese DMS Title**: `友達登録通知`
* **Raw DMS Script**: [`30509.dms`](../dms/30509.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x772D` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30509.dms
function parse(packet){
	with(packet){
		setTitle("友達登録通知");
		readInt32("キャラID");
		readInt32("電話番号");
		readWStr(0x42, "キャラ名");
		readBinary(2, "(padding)");
		readInt32("bOnline?");
	}
}
```

---

## Opcode 0x7732 (30514): `MsgUnknown_0x7732` [友達登校通知]

* **Original Japanese DMS Title**: `友達登校通知`
* **Raw DMS Script**: [`30514.dms`](../dms/30514.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x7732` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30514.dms
function parse(packet){
	with(packet){
		setTitle("友達登校通知");
		readUInt32("キャラID");
	}
}
```

---

## Opcode 0x7733 (30515): `MsgUnknown_0x7733` [友達下校通知]

* **Original Japanese DMS Title**: `友達下校通知`
* **Raw DMS Script**: [`30515.dms`](../dms/30515.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x7733` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30515.dms
function parse(packet){
	with(packet){
		setTitle("友達下校通知");
		readUInt32("キャラID");
	}
}
```

---

## Opcode 0x7735 (30517): `MsgUnknown_0x7735` [メール(memo)送信]

* **Original Japanese DMS Title**: `メール(memo)送信`
* **Raw DMS Script**: [`30517.dms`](../dms/30517.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x7735` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30517.dms
function parse(packet){
	with(packet){
		setTitle("メール(memo)送信");
		readUInt32("送信先電話番号");
		readWStr(202, "メッセージ");
		readBinary( 2, "(padding)");
	}
}
```

---

## Opcode 0x7736 (30518): `MsgUnknown_0x7736` [メール(memo)受信]

* **Original Japanese DMS Title**: `メール(memo)受信`
* **Raw DMS Script**: [`30518.dms`](../dms/30518.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x7736` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30518.dms
import struct35.*;
function parse(packet){
	packet.setTitle("メール(memo)受信");
	packet.readWord();
	packet.readWord();
	packet.readUInt32();
	packet.readUInt32();
	phone_call_from_char_info(packet);
	packet.readWStr(200, "メッセージ");
	packet.readBinary(2, "(padding)");
}
```

---

## Opcode 0x7740 (30528): `MsgUnknown_0x7740` [電話発信]

* **Original Japanese DMS Title**: `電話発信`
* **Raw DMS Script**: [`30528.dms`](../dms/30528.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x7740` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30528.dms
function parse(packet){
	with(packet){
		setTitle("電話発信");
		readUInt32("電話番号");
	}
}
```

---

## Opcode 0x7741 (30529): `MsgUnknown_0x7741` [電話発信返答]

* **Original Japanese DMS Title**: `電話発信返答`
* **Raw DMS Script**: [`30529.dms`](../dms/30529.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x7741` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30529.dms
import struct35.*;
function parse(packet){
	packet.setTitle("電話発信返答");
	packet.readUInt32("受話(1外で拒否？)");
	phone_call_info(packet);
	phone_call_from_char_info(packet);
}
```

---

## Opcode 0x7742 (30530): `MsgUnknown_0x7742` [電話着信]

* **Original Japanese DMS Title**: `電話着信`
* **Raw DMS Script**: [`30530.dms`](../dms/30530.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x7742` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30530.dms
import struct35.*;
function parse(packet){
	packet.setTitle("電話着信");
	phone_call_from_char_info(packet);
}
```

---

## Opcode 0x7743 (30531): `MsgUnknown_0x7743` [通話応答]

* **Original Japanese DMS Title**: `通話応答`
* **Raw DMS Script**: [`30531.dms`](../dms/30531.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x7743` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30531.dms
function parse(packet){
	with(packet){
		setTitle("通話応答");
		readUInt32("受話(1以外で拒否？)");
	}
}
```

---

## Opcode 0x7744 (30532): `MsgUnknown_0x7744` [電話通話開始]

* **Original Japanese DMS Title**: `電話通話開始`
* **Raw DMS Script**: [`30532.dms`](../dms/30532.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x7744` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30532.dms
import struct35.*;
function parse(packet){
	packet.setTitle("電話通話開始");
	phone_call_info(packet);
	phone_call_from_char_info(packet);
}
```

---

## Opcode 0x7746 (30534): `MsgUnknown_0x7746` [電話メッセージ送信]

* **Original Japanese DMS Title**: `電話メッセージ送信`
* **Raw DMS Script**: [`30534.dms`](../dms/30534.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x7746` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30534.dms
function parse(packet){
	with(packet){
		setTitle("電話メッセージ送信");
		readUInt32("通話ID");
		readWStr( 200, "メッセージ");
		readBinary( 2, "(padding)");
		readBinary( 2);
	}
}
```

---

## Opcode 0x7747 (30535): `MsgUnknown_0x7747` [電話切断]

* **Original Japanese DMS Title**: `電話切断`
* **Raw DMS Script**: [`30535.dms`](../dms/30535.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x7747` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30535.dms
function parse(packet){
	with(packet){
		setTitle("電話切断");
		readUInt32("通話ID");
	}
}
```

---

## Opcode 0x7759 (30553): `MsgCommEchoNtf` [チャットサーバーエコー通知]

* **Original Japanese DMS Title**: `チャットサーバーエコー通知`
* **Raw DMS Script**: [`30553.dms`](../dms/30553.dms)
* **Legacy Delphi Class**: `TMsgCommEchoNtf` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30553.dms
function parse(packet){
	packet.setTitle("チャットサーバーエコー通知");
	packet.readInt32("seqNum");
}
```

---
