# Volume 9: Series 20000: Core Engine, Ping, Version & Heartbeat

This chapter provides the complete, byte-for-byte specifications for all 5 opcodes in this range.

---

## Opcode 0x4E21 (20001): `MsgCheckVersionNtf` [バージョンチェック通知]

* **Original Japanese DMS Title**: `バージョンチェック通知`
* **Raw DMS Script**: [`20001.dms`](../dms/20001.dms)
* **Legacy Delphi Class**: `TMsgCheckVersionNtf` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/20001.dms
function parse(packet){
	with(packet){
		setTitle("バージョンチェック通知");
		readUInt32("Version");
	}
}
```

---

## Opcode 0x4E22 (20002): `MsgUnknown_0x4E22` [エラーメッセージ通知]

* **Original Japanese DMS Title**: `エラーメッセージ通知`
* **Raw DMS Script**: [`20002.dms`](../dms/20002.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x4E22` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/20002.dms
function parse(packet){
	with(packet){
		setTitle("エラーメッセージ通知");
		readInt32("フォーマット番号");
		var cnt = readWord("パラメータ数");
		var len;
		for(var i = 0; i < cnt; i++){
			len = readWord("パラメータ文字長");
			readWStr(len, "パラメータ文字列");
		}
		len = readWord("文字長");
		readWStr(len, "エラーメッセージ");
	}
}
```

---

## Opcode 0x4E24 (20004): `MsgUnknown_0x4E24` [アラートメッセージ通知]

* **Original Japanese DMS Title**: `アラートメッセージ通知`
* **Raw DMS Script**: [`20004.dms`](../dms/20004.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x4E24` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/20004.dms
function parse(packet){
	with(packet){
		setTitle("アラートメッセージ通知");
		readInt32("msgInfoTitle1");
		readWord("msgInfoTitle2");
		readInt32("msgInfoText1");
		readWord("msgInfoText2");
		readWStr(readWord("titleLen"), "title");
		readWStr(readWord("textLen"), "text");
	}
}
```

---

## Opcode 0x4E25 (20005): `MsgWorldTimeNtf` [ワールド時間通知]

* **Original Japanese DMS Title**: `ワールド時間通知`
* **Raw DMS Script**: [`20005.dms`](../dms/20005.dms)
* **Legacy Delphi Class**: `TMsgAlertMsgNtf` (Address: `005A910C`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/20005.dms
function parse(packet){
	packet.setTitle("ワールド時間通知");
	packet.readByte("season");
	packet.readByte("clock");
	packet.readBinary(2, "(padding)");
	packet.readUInt32("time");
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgAlertMsgNtf.Create(TitleFormat:Integer; TextMsg:string; TitleMsg:string; ?:?; TextArgs:string; TextFormat:Integer; ?:?; TitleArgs:string)
005A910C    push        ebp
 005A910D    mov         ebp,esp
 005A910F    push        ecx
 005A9110    push        ebx
 005A9111    push        esi
 005A9112    test        dl,dl
>005A9114    je          005A911E
 005A9116    add         esp,0FFFFFFF0
 005A9119    call        @ClassCreate
 005A911E    mov         ebx,ecx
 005A9120    mov         byte ptr [ebp-1],dl
 005A9123    mov         esi,eax
 005A9125    mov         dx,4E25
 005A9129    mov         eax,esi
 005A912B    call        TYgPacket.WriteID
 005A9130    mov         edx,ebx
 005A9132    mov         eax,esi
 005A9134    call       
```

---

## Opcode 0x4E26 (20006): `MsgTimeNtf` [時間通知]

* **Original Japanese DMS Title**: `時間通知`
* **Raw DMS Script**: [`20006.dms`](../dms/20006.dms)
* **Legacy Delphi Class**: `TMsgTimeNtf` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/20006.dms
function parse(packet){
	packet.setTitle("時間通知");
	packet.readUInt32("時間");
}
```

---
