# Volume 9: Series 30100: LoginServer, Authentication, Character Select & Creation

This chapter provides the complete, byte-for-byte specifications for all 20 opcodes in this range.

---

## Opcode 0x7595 (30101): `MsgAuthTypeNtf` [認証タイプ通知]

* **Original Japanese DMS Title**: `認証タイプ通知`
* **Raw DMS Script**: [`30101.dms`](../dms/30101.dms)
* **Legacy Delphi Class**: `TMsgGameRenewByulBeItemAns` (Address: `005AA8A8`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30101.dms
function parse(packet){
	packet.setTitle("認証タイプ通知");
	packet.readUInt32("認証タイプ");
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgGameRenewByulBeItemAns.Create(?:?; ?:?)
005AA8A8    push        ebx
 005AA8A9    push        esi
 005AA8AA    push        edi
 005AA8AB    test        dl,dl
>005AA8AD    je          005AA8B7
 005AA8AF    add         esp,0FFFFFFF0
 005AA8B2    call        @ClassCreate
 005AA8B7    mov         esi,ecx
 005AA8B9    mov         ebx,edx
 005AA8BB    mov         edi,eax
 005AA8BD    mov         dx,7595
 005AA8C1    mov         eax,edi
 005AA8C3    call        TYgPacket.WriteID
 005AA8C8    mov         edx,esi
 005AA8CA    mov         eax,edi
 005AA8CC    call        TYgPacket.WriteInt32
 005AA8D1    mov         eax,edi
 005AA8D3    test  
```

---

## Opcode 0x7596 (30102): `MsgLoginAuthReq` [アカウント認証要求]

* **Original Japanese DMS Title**: `アカウント認証要求`
* **Raw DMS Script**: [`30102.dms`](../dms/30102.dms)
* **Legacy Delphi Class**: `TMsgLoginAuthReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30102.dms
function parse(packet){
	packet.setTitle("アカウント認証要求");
	packet.readWStr(0x32, "ユーザーID");
	packet.readStr(0x22, "ユーザーパスワード(MD5)");
	packet.readInt32("エンコードタイプ");
}
```

---

## Opcode 0x7597 (30103): `MsgLoginAuthAns` [ログイン認証返答]

* **Original Japanese DMS Title**: `ログイン認証返答`
* **Raw DMS Script**: [`30103.dms`](../dms/30103.dms)
* **Legacy Delphi Class**: `TMsgLoginAuthAns` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30103.dms
import struct.*;
function parse(packet){
	packet.setTitle("ログイン認証返答");
	packet.readUInt32("戻り値");
	packet.readWStr(0x16, "ワールド名");
	packet.readWStr(0x42, "学校名");
	char_disp_info(packet);
	packet.readUInt32("cntWait");
}
```

---

## Opcode 0x759A (30106): `MsgLoginJoinGameReq` [ゲーム参加要求]

* **Original Japanese DMS Title**: `ゲーム参加要求`
* **Raw DMS Script**: [`30106.dms`](../dms/30106.dms)
* **Legacy Delphi Class**: `TMsgLoginJoinGameReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30106.dms
function parse(packet){
	packet.setTitle("ゲーム参加要求");
}
```

---

## Opcode 0x759B (30107): `MsgLoginJoinGameAns` [ゲーム参加返答]

* **Original Japanese DMS Title**: `ゲーム参加返答`
* **Raw DMS Script**: [`30107.dms`](../dms/30107.dms)
* **Legacy Delphi Class**: `TMsgLoginAuthenticationAns` (Address: `005AA9E0`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30107.dms
function parse(packet){
	packet.setTitle("ゲーム参加返答");
	packet.readUInt32("戻り値");
	packet.readUInt32("accountSN");
	packet.readUInt32("サーバー番号");
	packet.readWord("年");
	packet.readWord("月");
	packet.readWord("曜日");
	packet.readWord("日");
	packet.readWord("時");
	packet.readWord("分");
	packet.readWord("秒");
	packet.readWord("ミリ秒");
	packet.readUInt32();
	packet.readIPPort("スクールサーバー");
	packet.readIPPort("チャットサーバー");
	packet.readUInt32("ユーザーID");
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgLoginAuthenticationAns.Create(?:?; ?:?)
005AA9E0    push        ebp
 005AA9E1    mov         ebp,esp
 005AA9E3    add         esp,0FFFFFFF0
 005AA9E6    push        ebx
 005AA9E7    push        esi
 005AA9E8    push        edi
 005AA9E9    test        dl,dl
>005AA9EB    je          005AA9F5
 005AA9ED    add         esp,0FFFFFFF0
 005AA9F0    call        @ClassCreate
 005AA9F5    mov         esi,ecx
 005AA9F7    mov         ebx,edx
 005AA9F9    mov         edi,eax
 005AA9FB    call        Now
 005AAA00    add         esp,0FFFFFFF8
 005AAA03    fstp        qword ptr [esp]
 005AAA06    wait
 005AAA07    lea         eax,[ebp-10]
 005AAA
```

---

## Opcode 0x759F (30111): `MsgLoginWorldListReq` [ワールドリスト要求]

* **Original Japanese DMS Title**: `ワールドリスト要求`
* **Raw DMS Script**: [`30111.dms`](../dms/30111.dms)
* **Legacy Delphi Class**: `TMsgLoginWorldListReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30111.dms
function parse(packet){
	packet.setTitle("ワールドリスト要求");
}
```

---

## Opcode 0x75A0 (30112): `MsgLoginWorldListAns` [ワールドリスト返答]

* **Original Japanese DMS Title**: `ワールドリスト返答`
* **Raw DMS Script**: [`30112.dms`](../dms/30112.dms)
* **Legacy Delphi Class**: `TMsgLoginWorldListAns` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30112.dms
function parse(packet){
	packet.setTitle("ワールドリスト返答");
	packet.readUInt32("戻り値");
	var cnt = packet.readUInt32("ワールド数");
}
```

---

## Opcode 0x75A1 (30113): `MsgLoginWorldListNtf` [ワールドリスト通知]

* **Original Japanese DMS Title**: `ワールドリスト通知`
* **Raw DMS Script**: [`30113.dms`](../dms/30113.dms)
* **Legacy Delphi Class**: `TMsgLoginWorldListNtf` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30113.dms
function parse(packet){
	packet.setTitle("ワールドリスト通知");
	packet.readUInt32("bLast");
	var cnt = packet.readUInt32("ワールド数");
	for(var i = 1; i <= cnt; i++){
		packet.readWStr(0x18, "ワールド名" + i.toString());
		packet.readUInt32("ワールド番号" + i.toString());
		packet.readUInt32("通し番号？混雑度？" + i.toString());
	}
}
```

---

## Opcode 0x75A2 (30114): `MsgLoginSelectWorldReq` [ワールド選択通知]

* **Original Japanese DMS Title**: `ワールド選択通知`
* **Raw DMS Script**: [`30114.dms`](../dms/30114.dms)
* **Legacy Delphi Class**: `TMsgLoginSelectWorldReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30114.dms
function parse(packet){
	packet.setTitle("ワールド選択通知");
	packet.readUInt32("ワールドID");
}
```

---

## Opcode 0x75A3 (30115): `MsgLoginSchoolListNtf` [学校リスト通知]

* **Original Japanese DMS Title**: `学校リスト通知`
* **Raw DMS Script**: [`30115.dms`](../dms/30115.dms)
* **Legacy Delphi Class**: `TMsgLoginSchoolListNtf` (Address: `005AAC38`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30115.dms
function parse(packet){
	packet.setTitle("学校リスト通知");
	packet.readUInt32("bLast");
	packet.readUInt32("ワールドID");
	var cnt = packet.readUInt32("学校数");
	for(var i = 1; i <= cnt; i++){
		packet.readUInt32("学校ID" + i.toString());
		packet.readWStr(0x44, "学校名" + i.toString());
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgLoginSchoolListNtf.Create(WorldID:Integer)
005AAC38    push        ebx
 005AAC39    push        esi
 005AAC3A    push        edi
 005AAC3B    test        dl,dl
>005AAC3D    je          005AAC47
 005AAC3F    add         esp,0FFFFFFF0
 005AAC42    call        @ClassCreate
 005AAC47    mov         esi,ecx
 005AAC49    mov         ebx,edx
 005AAC4B    mov         edi,eax
 005AAC4D    mov         dx,75A3
 005AAC51    mov         eax,edi
 005AAC53    call        TYgPacket.WriteID
 005AAC58    mov         edx,1
 005AAC5D    mov         eax,edi
 005AAC5F    call        TYgPacket.WriteInt32
 005AAC64    mov         edx,esi
 005AAC66    mov     
```

---

## Opcode 0x75A4 (30116): `MsgLoginCheckNameReq` [名前重複チェック要求]

* **Original Japanese DMS Title**: `名前重複チェック要求`
* **Raw DMS Script**: [`30116.dms`](../dms/30116.dms)
* **Legacy Delphi Class**: `TMsgLoginCheckNameReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30116.dms
function parse(packet){
	packet.setTitle("名前重複チェック要求");
	packet.readWStr(0x1A, "キャラ名");
}
```

---

## Opcode 0x75A5 (30117): `MsgLoginCheckNameAns` [名前重複チェック返答]

* **Original Japanese DMS Title**: `名前重複チェック返答`
* **Raw DMS Script**: [`30117.dms`](../dms/30117.dms)
* **Legacy Delphi Class**: `TMsgLoginCheckNameAns` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30117.dms
function parse(packet){
	packet.setTitle("名前重複チェック返答");
	packet.readUInt32("戻り値");
	packet.readWStr(0x1C, "キャラ名");
}
```

---

## Opcode 0x75A6 (30118): `MsgLoginCheckPhoneReq` [電話番号重複確認]

* **Original Japanese DMS Title**: `電話番号重複確認`
* **Raw DMS Script**: [`30118.dms`](../dms/30118.dms)
* **Legacy Delphi Class**: `TMsgLoginCheckPhoneReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30118.dms
function parse(packet){
	packet.setTitle("電話番号重複確認");
	packet.readUInt32("電話番号(下4ケタ)");
}
```

---

## Opcode 0x75A7 (30119): `MsgLoginCheckPhoneAns` [電話番号重複確認返答]

* **Original Japanese DMS Title**: `電話番号重複確認返答`
* **Raw DMS Script**: [`30119.dms`](../dms/30119.dms)
* **Legacy Delphi Class**: `TMsgLoginCheckCharNameAns` (Address: `005AAD88`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30119.dms
function parse(packet){
	packet.setTitle("電話番号重複確認返答");
	packet.readUInt32("エラーコード");
	packet.readUInt32("電話番号(下4ケタ)");
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgLoginCheckCharNameAns.Create(?:?)
constructor TMsgLoginCheckPhoneAns.Create(?:?);
begin
 005AAD88    push        ebp
 005AAD89    mov         ebp,esp
 005AAD8B    push        ebx
 005AAD8C    push        esi
 005AAD8D    push        edi
 005AAD8E    test        dl,dl
>005AAD90    je          005AAD9A
 005AAD92    add         esp,0FFFFFFF0
 005AAD95    call        @ClassCreate
 005AAD9A    mov         esi,ecx
 005AAD9C    mov         ebx,edx
 005AAD9E    mov         edi,eax
 005AADA0    mov         dx,75A7
 005AADA4    mov         eax,edi
 005AADA6    call        TYgPacket.WriteID
 005AADAB    mov         edx,esi
 005AADAD    m
```

---

## Opcode 0x75A8 (30120): `MsgLoginMakeCharReq` [キャラ作成要求]

* **Original Japanese DMS Title**: `キャラ作成要求`
* **Raw DMS Script**: [`30120.dms`](../dms/30120.dms)
* **Legacy Delphi Class**: `TMsgLoginMakeCharReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30120.dms
function parse(packet){
	packet.setTitle("キャラ作成要求");
	packet.readUInt32("ワールドID");
	packet.readWStr(0x1C, "キャラ名");
	packet.readUInt32("電話番号(下4ケタ)");
	packet.readUInt32("性別");
	packet.readUInt32("学校");
	packet.readUInt32("顔パーツ");
	packet.readUInt32("髪パーツ");
	packet.readUInt32("肌の色");
	packet.readByte("誕生月");
	packet.readByte("誕生日");
	packet.readByte("血液型");
	packet.readByte("学年");
}
```

---

## Opcode 0x75A9 (30121): `MsgLoginMakeCharAns` [キャラ作成返答]

* **Original Japanese DMS Title**: `キャラ作成返答`
* **Raw DMS Script**: [`30121.dms`](../dms/30121.dms)
* **Legacy Delphi Class**: `TMsgLoginMakeCharAns` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30121.dms
import struct.*;
function parse(packet){
	packet.setTitle("キャラ作成返答");
	packet.readUInt32("戻り値");
	char_disp_info(packet);
}
```

---

## Opcode 0x75AA (30122): `MsgLoginDeleteCharReq` [キャラ削除要求]

* **Original Japanese DMS Title**: `キャラ削除要求`
* **Raw DMS Script**: [`30122.dms`](../dms/30122.dms)
* **Legacy Delphi Class**: `TMsgLoginDeleteCharReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30122.dms
function parse(packet){
	packet.setTitle("キャラ削除要求");
}
```

---

## Opcode 0x75AB (30123): `MsgLoginDeleteCharAns` [キャラ削除返答]

* **Original Japanese DMS Title**: `キャラ削除返答`
* **Raw DMS Script**: [`30123.dms`](../dms/30123.dms)
* **Legacy Delphi Class**: `TMsgLoginCreateCharAns` (Address: `005AAE30`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30123.dms
function parse(packet){
	packet.setTitle("キャラ削除返答");
	packet.readUInt32("戻り値");
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgLoginCreateCharAns.Create(?:?)
005AAE30    push        ebx
 005AAE31    push        esi
 005AAE32    push        edi
 005AAE33    test        dl,dl
>005AAE35    je          005AAE3F
 005AAE37    add         esp,0FFFFFFF0
 005AAE3A    call        @ClassCreate
 005AAE3F    mov         esi,ecx
 005AAE41    mov         ebx,edx
 005AAE43    mov         edi,eax
 005AAE45    mov         dx,75AB
 005AAE49    mov         eax,edi
 005AAE4B    call        TYgPacket.WriteID
 005AAE50    mov         edx,esi
 005AAE52    mov         eax,edi
 005AAE54    call        TYgPacket.WriteRC
 005AAE59    mov         eax,edi
 005AAE5B    test     
```

---

## Opcode 0x75AE (30126): `MsgLoginResumeNtf` [リジューム通知]

* **Original Japanese DMS Title**: `リジューム通知`
* **Raw DMS Script**: [`30126.dms`](../dms/30126.dms)
* **Legacy Delphi Class**: `TMsgLoginResumeNtf` (Address: `005AAE74`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30126.dms
function parse(packet){
	packet.setTitle("リジューム通知");
	packet.readUInt32("アカウントID");
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgLoginResumeNtf.Create(SessionID:Integer)
005AAE74    push        ebx
 005AAE75    push        esi
 005AAE76    push        edi
 005AAE77    test        dl,dl
>005AAE79    je          005AAE83
 005AAE7B    add         esp,0FFFFFFF0
 005AAE7E    call        @ClassCreate
 005AAE83    mov         esi,ecx
 005AAE85    mov         ebx,edx
 005AAE87    mov         edi,eax
 005AAE89    mov         dx,75AE
 005AAE8D    mov         eax,edi
 005AAE8F    call        TYgPacket.WriteID
 005AAE94    mov         edx,esi
 005AAE96    mov         eax,edi
 005AAE98    call        TYgPacket.WriteInt32
 005AAE9D    mov         eax,edi
 005AAE9F    test  
```

---

## Opcode 0x75AF (30127): `MsgUnknown_0x75AF` [タイムアウト通知]

* **Original Japanese DMS Title**: `タイムアウト通知`
* **Raw DMS Script**: [`30127.dms`](../dms/30127.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x75AF` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30127.dms
function parse(packet){
	packet.setTitle("タイムアウト通知");
	packet.readUInt32("アカウントID");
}
```

---
