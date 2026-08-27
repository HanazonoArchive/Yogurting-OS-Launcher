# Volume 9: Series 30300 & 30400: EpisodeServer, Lobby Matchmaking & Waiting Rooms

This chapter provides the complete, byte-for-byte specifications for all 42 opcodes in this range.

---

## Opcode 0x765D (30301): `MsgUnknown_0x765D` [ロビー参加通知]

* **Original Japanese DMS Title**: `ロビー参加通知`
* **Raw DMS Script**: [`30301.dms`](../dms/30301.dms)
* **Legacy Delphi Class**: `TMsgLobbyEnterNtf` (Address: `005AB1C4`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30301.dms
function parse(packet){
	with(packet){
		setTitle("ロビー参加通知");
		var cnt = readWord("vecAvailEpisodeTypeCount");
		for(var i = 1; i <= cnt; i++){
			readInt32("  AvailEpisodeType" + i.toString());
			readInt32("  AvailEpisodeType" + i.toString());
			readSingle("  AvailEpisodeType" + i.toString());
			readSingle("  AvailEpisodeType" + i.toString());
		}
		readWord("cntMaxRoom");
		readInt32("epSN");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgLobbyEnterNtf.Create(Lobby:TLobby)
005AB1C4    push        ebp
 005AB1C5    mov         ebp,esp
 005AB1C7    add         esp,0FFFFFFF0
 005AB1CA    push        ebx
 005AB1CB    test        dl,dl
>005AB1CD    je          005AB1D7
 005AB1CF    add         esp,0FFFFFFF0
 005AB1D2    call        @ClassCreate
 005AB1D7    mov         dword ptr [ebp-0C],ecx
 005AB1DA    mov         byte ptr [ebp-5],dl
 005AB1DD    mov         dword ptr [ebp-4],eax
 005AB1E0    mov         dx,765D
 005AB1E4    mov         eax,dword ptr [ebp-4]
 005AB1E7    call        TYgPacket.WriteID
 005AB1EC    mov         eax,dword ptr [ebp-0C]
 005AB1EF    mov  
```

---

## Opcode 0x765E (30302): `MsgLobbyEnterReq` [ロビー退出通知]

* **Original Japanese DMS Title**: `ロビー退出通知`
* **Raw DMS Script**: [`30302.dms`](../dms/30302.dms)
* **Legacy Delphi Class**: `TMsgLobbyEnterReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30302.dms
function parse(packet){
	with(packet){
		setTitle("ロビー退出通知");
	}
}
```

---

## Opcode 0x765F (30303): `MsgUnknown_0x765F` [ロビー部屋情報通知]

* **Original Japanese DMS Title**: `ロビー部屋情報通知`
* **Raw DMS Script**: [`30303.dms`](../dms/30303.dms)
* **Legacy Delphi Class**: `TMsgLobbyRoomInfoNtf` (Address: `005AB328`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30303.dms
import struct.*;
function parse(packet){
	packet.setTitle("ロビー部屋情報通知");
	room_info(packet);
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgLobbyRoomInfoNtf.Create(Room:TWaitRoom)
005AB328    push        ebx
 005AB329    push        esi
 005AB32A    push        edi
 005AB32B    test        dl,dl
>005AB32D    je          005AB337
 005AB32F    add         esp,0FFFFFFF0
 005AB332    call        @ClassCreate
 005AB337    mov         esi,ecx
 005AB339    mov         ebx,edx
 005AB33B    mov         edi,eax
 005AB33D    mov         dx,765F
 005AB341    mov         eax,edi
 005AB343    call        TYgPacket.WriteID
 005AB348    mov         edx,esi
 005AB34A    mov         eax,edi
 005AB34C    call        TYgPacket.WriteRoomInfo
 005AB351    mov         eax,edi
 005AB353    tes
```

---

## Opcode 0x7660 (30304): `MsgLobbyLeaveReq` [ロビーページ選択要求]

* **Original Japanese DMS Title**: `ロビーページ選択要求`
* **Raw DMS Script**: [`30304.dms`](../dms/30304.dms)
* **Legacy Delphi Class**: `TMsgLobbyLeaveReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30304.dms
function parse(packet){
	with(packet){
		setTitle("ロビーページ選択要求");
		readInt32("pageNum");
	}
}
```

---

## Opcode 0x7662 (30306): `MsgUnknown_0x7662` [ロビーページ情報通知]

* **Original Japanese DMS Title**: `ロビーページ情報通知`
* **Raw DMS Script**: [`30306.dms`](../dms/30306.dms)
* **Legacy Delphi Class**: `TMsgLobbyPageInfoNtf` (Address: `005AB3B8`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30306.dms
import struct.*;
function parse(packet){
	with(packet){
		setTitle("ロビーページ情報通知");
		readInt32("pageNum");
		var cnt = readWord("rooms");
		for (var i = 0; i < cnt; i++){
			room_info(packet);
		}
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgLobbyPageInfoNtf.Create(Chara:TChara)
005AB3B8    push        ebp
 005AB3B9    mov         ebp,esp
 005AB3BB    add         esp,0FFFFFFF4
 005AB3BE    push        ebx
 005AB3BF    test        dl,dl
>005AB3C1    je          005AB3CB
 005AB3C3    add         esp,0FFFFFFF0
 005AB3C6    call        @ClassCreate
 005AB3CB    mov         ebx,ecx
 005AB3CD    mov         byte ptr [ebp-5],dl
 005AB3D0    mov         dword ptr [ebp-4],eax
 005AB3D3    mov         dx,7662
 005AB3D7    mov         eax,dword ptr [ebp-4]
 005AB3DA    call        TYgPacket.WriteID
 005AB3DF    mov         edx,dword ptr [ebx+0C];TChara.FLobbyPage:Integer
 005AB3
```

---

## Opcode 0x7663 (30307): `MsgUnknown_0x7663` [エピソードページ状態通知]

* **Original Japanese DMS Title**: `エピソードページ状態通知`
* **Raw DMS Script**: [`30307.dms`](../dms/30307.dms)
* **Legacy Delphi Class**: `TMsgLobbyEpisodePageStatusNtf` (Address: `005AB488`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30307.dms
function parse(packet){
	with(packet){
		setTitle("エピソードページ状態通知");
		readInt32("snEpisode");
		readInt32("exist");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgLobbyEpisodePageStatusNtf.Create(Episode:TEpisode)
005AB488    push        ebx
 005AB489    push        esi
 005AB48A    push        edi
 005AB48B    test        dl,dl
>005AB48D    je          005AB497
 005AB48F    add         esp,0FFFFFFF0
 005AB492    call        @ClassCreate
 005AB497    mov         esi,ecx
 005AB499    mov         ebx,edx
 005AB49B    mov         edi,eax
 005AB49D    mov         dx,7663
 005AB4A1    mov         eax,edi
 005AB4A3    call        TYgPacket.WriteID
 005AB4A8    mov         edx,dword ptr [esi+8];TEpisode.ID:Integer
 005AB4AB    mov         eax,edi
 005AB4AD    call        TYgPacket.WriteInt32
 005AB4B2    mov  
```

---

## Opcode 0x7664 (30308): `MsgUnknown_0x7664` [ロビー・エピソード選択要求]

* **Original Japanese DMS Title**: `ロビー・エピソード選択要求`
* **Raw DMS Script**: [`30308.dms`](../dms/30308.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x7664` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30308.dms
function parse(packet){
	with(packet){
		setTitle("ロビー・エピソード選択要求");
		readInt32("snEpisode");
	}
}
```

---

## Opcode 0x7665 (30309): `MsgUnknown_0x7665` [ロビー・エピソード選択返答]

* **Original Japanese DMS Title**: `ロビー・エピソード選択返答`
* **Raw DMS Script**: [`30309.dms`](../dms/30309.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x7665` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30309.dms
function parse(packet){
	with(packet){
		setTitle("ロビー・エピソード選択返答");
		readInt32("戻り値");
		readInt32("snEpisode");
	}
}
```

---

## Opcode 0x7666 (30310): `MsgUnknown_0x7666` [ロビー参加キャラ通知]

* **Original Japanese DMS Title**: `ロビー参加キャラ通知`
* **Raw DMS Script**: [`30310.dms`](../dms/30310.dms)
* **Legacy Delphi Class**: `TMsgLobbyEnterPcNtf` (Address: `005AB4E0`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30310.dms
import struct.*;
function parse(packet){
	packet.setTitle("ロビー参加キャラ通知");
	pc_info(packet);
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgLobbyEnterPcNtf.Create(Chara:TChara)
005AB4E0    push        ebx
 005AB4E1    push        esi
 005AB4E2    push        edi
 005AB4E3    test        dl,dl
>005AB4E5    je          005AB4EF
 005AB4E7    add         esp,0FFFFFFF0
 005AB4EA    call        @ClassCreate
 005AB4EF    mov         esi,ecx
 005AB4F1    mov         ebx,edx
 005AB4F3    mov         edi,eax
 005AB4F5    mov         dx,7666
 005AB4F9    mov         eax,edi
 005AB4FB    call        TYgPacket.WriteID
 005AB500    mov         edx,esi
 005AB502    mov         eax,edi
 005AB504    call        TYgPacket.WritePCInfo
 005AB509    mov         eax,edi
 005AB50B    test 
```

---

## Opcode 0x7667 (30311): `MsgUnknown_0x7667` [ロビー退出キャラ通知]

* **Original Japanese DMS Title**: `ロビー退出キャラ通知`
* **Raw DMS Script**: [`30311.dms`](../dms/30311.dms)
* **Legacy Delphi Class**: `TMsgLobbyLeavePcNtf` (Address: `005AB524`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30311.dms
function parse(packet){
	packet.setTitle("ロビー退出キャラ通知");
	packet.readInt32("キャラID");
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgLobbyLeavePcNtf.Create(Chara:TChara)
005AB524    push        ebx
 005AB525    push        esi
 005AB526    push        edi
 005AB527    test        dl,dl
>005AB529    je          005AB533
 005AB52B    add         esp,0FFFFFFF0
 005AB52E    call        @ClassCreate
 005AB533    mov         esi,ecx
 005AB535    mov         ebx,edx
 005AB537    mov         edi,eax
 005AB539    mov         dx,7667
 005AB53D    mov         eax,edi
 005AB53F    call        TYgPacket.WriteID
 005AB544    mov         edx,dword ptr [esi+0E0];TChara.ID:Integer
 005AB54A    mov         eax,edi
 005AB54C    call        TYgPacket.WriteInt32
 005AB551    mov  
```

---

## Opcode 0x7668 (30312): `MsgLobbyRoomListReq` [ルーム作成予約要求]

* **Original Japanese DMS Title**: `ルーム作成予約要求`
* **Raw DMS Script**: [`30312.dms`](../dms/30312.dms)
* **Legacy Delphi Class**: `TMsgLobbyRoomListReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30312.dms
function parse(packet){
	with(packet){
		setTitle("ルーム作成予約要求");
	}
}
```

---

## Opcode 0x7669 (30313): `MsgUnknown_0x7669` [ルーム作成予約返答]

* **Original Japanese DMS Title**: `ルーム作成予約返答`
* **Raw DMS Script**: [`30313.dms`](../dms/30313.dms)
* **Legacy Delphi Class**: `TMsgLobbyReserveMakeRoomAns` (Address: `005AB56C`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30313.dms
function parse(packet){
	with(packet){
		setTitle("ルーム作成予約返答");
		readInt32("戻り値");
		readWord("snRoom");
		readWord("idLobby");
		readByte("bAuthSecret");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgLobbyReserveMakeRoomAns.Create(Chara:TChara; Succeed:Boolean)
005AB56C    push        ebp
 005AB56D    mov         ebp,esp
 005AB56F    push        ebx
 005AB570    push        esi
 005AB571    push        edi
 005AB572    test        dl,dl
>005AB574    je          005AB57E
 005AB576    add         esp,0FFFFFFF0
 005AB579    call        @ClassCreate
 005AB57E    mov         esi,ecx
 005AB580    mov         ebx,edx
 005AB582    mov         edi,eax
 005AB584    mov         dx,7669
 005AB588    mov         eax,edi
 005AB58A    call        TYgPacket.WriteID
 005AB58F    cmp         byte ptr [ebp+8],0
>005AB593    je          005AB5DF
 005AB595    cmp        
```

---

## Opcode 0x766A (30314): `MsgLobbyCreateRoomReq` [ルーム作成要求]

* **Original Japanese DMS Title**: `ルーム作成要求`
* **Raw DMS Script**: [`30314.dms`](../dms/30314.dms)
* **Legacy Delphi Class**: `TMsgLobbyCreateRoomReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30314.dms
function parse(packet){
	with(packet){
		setTitle("ルーム作成要求");
		readWStr(0x2C, "タイトル");
		readInt32("idEpisodeType");
		readByte("bPKMode");
		readByte("bLimitMilk");
		readByte("bWaitRoom");
		readByte("cntTeam");
		readWStr(18, "password");
		readWord();
	}
}
```

---

## Opcode 0x766B (30315): `MsgUnknown_0x766B` [ルーム作成返答]

* **Original Japanese DMS Title**: `ルーム作成返答`
* **Raw DMS Script**: [`30315.dms`](../dms/30315.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x766B` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30315.dms
function parse(packet){
	with(packet){
		setTitle("ルーム作成返答");
		readInt32("戻り値");
	}
}
```

---

## Opcode 0x766C (30316): `MsgLobbyPageInfoNtf` [ロビー・部屋作成キャンセル通知]

* **Original Japanese DMS Title**: `ロビー・部屋作成キャンセル通知`
* **Raw DMS Script**: [`30316.dms`](../dms/30316.dms)
* **Legacy Delphi Class**: `TMsgLobbyPageInfoNtf` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30316.dms
function parse(packet){
	with(packet){
		setTitle("ロビー・部屋作成キャンセル通知");
	}
}
```

---

## Opcode 0x766D (30317): `MsgLobbyReserveJoinRoomReq` [ロビー・待機室参加予約要求]

* **Original Japanese DMS Title**: `ロビー・待機室参加予約要求`
* **Raw DMS Script**: [`30317.dms`](../dms/30317.dms)
* **Legacy Delphi Class**: `TMsgLobbyReserveJoinRoomReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30317.dms
function parse(packet){
	with(packet){
		setTitle("ロビー・待機室参加予約要求");
		readWord("部屋ID");
		readWord("ロビーID");
	}
}
```

---

## Opcode 0x766E (30318): `MsgUnknown_0x766E` [ロビー・待機室参加予約返答]

* **Original Japanese DMS Title**: `ロビー・待機室参加予約返答`
* **Raw DMS Script**: [`30318.dms`](../dms/30318.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x766E` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30318.dms
function parse(packet){
	with(packet){
		setTitle("ロビー・待機室参加予約返答");
		readInt32("戻り値");
		readWord("部屋ID");
		readWord("ロビーID");
		readWord();
		readInt32("キャラID");
		readWStr(26, "キャラ名");
		readByte("性別");
		readByte("学年");
		readWord("武器タイプ");
		readWord("チームID");
		readInt32("電話番号");
		readInt32("idPromotion");
	}
}
```

---

## Opcode 0x766F (30319): `MsgLobbyJoinRoomReq` [ロビー・待機室参加要求]

* **Original Japanese DMS Title**: `ロビー・待機室参加要求`
* **Raw DMS Script**: [`30319.dms`](../dms/30319.dms)
* **Legacy Delphi Class**: `TMsgLobbyJoinRoomReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30319.dms
function parse(packet){
	with(packet){
		setTitle("ロビー・待機室参加要求");
		readWStr(18, "password");
	}
}
```

---

## Opcode 0x7670 (30320): `MsgUnknown_0x7670` [ロビー・待機室参加返答]

* **Original Japanese DMS Title**: `ロビー・待機室参加返答`
* **Raw DMS Script**: [`30320.dms`](../dms/30320.dms)
* **Legacy Delphi Class**: `TMsgLobbyReserveJoinRoomAns` (Address: `005AB7EC`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30320.dms
function parse(packet){
	with(packet){
		setTitle("ロビー・待機室参加返答");
		readInt32("戻り値");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgLobbyReserveJoinRoomAns.Create(?:?)
005AB7EC    push        ebx
 005AB7ED    push        esi
 005AB7EE    push        edi
 005AB7EF    test        dl,dl
>005AB7F1    je          005AB7FB
 005AB7F3    add         esp,0FFFFFFF0
 005AB7F6    call        @ClassCreate
 005AB7FB    mov         esi,ecx
 005AB7FD    mov         ebx,edx
 005AB7FF    mov         edi,eax
 005AB801    mov         dx,7670
 005AB805    mov         eax,edi
 005AB807    call        TYgPacket.WriteID
 005AB80C    mov         edx,esi
 005AB80E    mov         eax,edi
 005AB810    call        TYgPacket.WriteRC
 005AB815    mov         eax,edi
 005AB817    test     
```

---

## Opcode 0x7671 (30321): `MsgUnknown_0x7671` [ロビー・待機室参加キャンセル通知]

* **Original Japanese DMS Title**: `ロビー・待機室参加キャンセル通知`
* **Raw DMS Script**: [`30321.dms`](../dms/30321.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x7671` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30321.dms
function parse(packet){
	with(packet){
		setTitle("ロビー・待機室参加キャンセル通知");
	}
}
```

---

## Opcode 0x7672 (30322): `MsgLobbyQuickJoinReq` [即時ルーム参加要求]

* **Original Japanese DMS Title**: `即時ルーム参加要求`
* **Raw DMS Script**: [`30322.dms`](../dms/30322.dms)
* **Legacy Delphi Class**: `TMsgLobbyQuickJoinReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30322.dms
function parse(packet){
	with(packet){
		setTitle("即時ルーム参加要求");
	}
}
```

---

## Opcode 0x7673 (30323): `MsgUnknown_0x7673` [即時ルーム参加返答]

* **Original Japanese DMS Title**: `即時ルーム参加返答`
* **Raw DMS Script**: [`30323.dms`](../dms/30323.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x7673` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30323.dms
function parse(packet){
	with(packet){
		setTitle("即時ルーム参加返答");
		readInt32("戻り値");
	}
}
```

---

## Opcode 0x7676 (30326): `MsgUnknown_0x7676` [利用可能エピソード情報通知]

* **Original Japanese DMS Title**: `利用可能エピソード情報通知`
* **Raw DMS Script**: [`30326.dms`](../dms/30326.dms)
* **Legacy Delphi Class**: `TMsgLobbyAvailableEpisodeInfoNtf` (Address: `005AB874`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30326.dms
import struct.*;
function parse(packet){
	with(packet){
		setTitle("利用可能エピソード情報通知");
		var cnt = readWord("vecAvailEpisodeType");
		episode_info(packet, cnt);
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgLobbyAvailableEpisodeInfoNtf.Create(EpisodeList:TEpisodeList)
005AB874    push        ebp
 005AB875    mov         ebp,esp
 005AB877    add         esp,0FFFFFFF8
 005AB87A    push        ebx
 005AB87B    push        esi
 005AB87C    push        edi
 005AB87D    test        dl,dl
>005AB87F    je          005AB889
 005AB881    add         esp,0FFFFFFF0
 005AB884    call        @ClassCreate
 005AB889    mov         esi,ecx
 005AB88B    mov         byte ptr [ebp-5],dl
 005AB88E    mov         dword ptr [ebp-4],eax
 005AB891    mov         dx,7676
 005AB895    mov         eax,dword ptr [ebp-4]
 005AB898    call        TYgPacket.WriteID
 005AB89D    movzx     
```

---

## Opcode 0x7677 (30327): `MsgUnknown_0x7677` [待機室参加通知]

* **Original Japanese DMS Title**: `待機室参加通知`
* **Raw DMS Script**: [`30327.dms`](../dms/30327.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x7677` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30327.dms
function parse(packet){
	with(packet){
		setTitle("待機室参加通知");
	}
}
```

---

## Opcode 0x7678 (30328): `MsgWaitRoomInfoReq` [待機室退出通知]

* **Original Japanese DMS Title**: `待機室退出通知`
* **Raw DMS Script**: [`30328.dms`](../dms/30328.dms)
* **Legacy Delphi Class**: `TMsgWaitRoomInfoReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30328.dms
function parse(packet){
	with(packet){
		setTitle("待機室退出通知");
	}
}
```

---

## Opcode 0x7679 (30329): `MsgUnknown_0x7679` [待機室情報通知]

* **Original Japanese DMS Title**: `待機室情報通知`
* **Raw DMS Script**: [`30329.dms`](../dms/30329.dms)
* **Legacy Delphi Class**: `TMsgWaitRoomInfoNtf` (Address: `005AB968`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30329.dms
function parse(packet){
	packet.setTitle("待機室情報通知");
	packet.readWord("部屋ID");
	packet.readWord("ロビーID");
	packet.readWStr(0x2A,"部屋名");
	packet.readBinary(2, "padding");
	packet.readInt32("idEpisodeType");
	packet.readByte("bPKMode");
	packet.readByte("bLimitMilk");
	packet.readByte("cntMinChar");
	packet.readByte("cntMaxChar");
	packet.readByte("cntTeam");
	packet.readByte("clearRate");
	packet.readWStr(0x12,"password");
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgWaitRoomInfoNtf.Create(Room:TWaitRoom)
005AB968    push        ebx
 005AB969    push        esi
 005AB96A    push        edi
 005AB96B    test        dl,dl
>005AB96D    je          005AB977
 005AB96F    add         esp,0FFFFFFF0
 005AB972    call        @ClassCreate
 005AB977    mov         esi,ecx
 005AB979    mov         ebx,edx
 005AB97B    mov         edi,eax
 005AB97D    mov         dx,7679
 005AB981    mov         eax,edi
 005AB983    call        TYgPacket.WriteID
 005AB988    movzx       edx,word ptr [esi+8];TWaitRoom.FID:Integer
 005AB98C    mov         eax,edi
 005AB98E    call        TYgPacket.WriteWord
 005AB993    mov  
```

---

## Opcode 0x767A (30330): `MsgWaitRoomEditReq` [待機室・エディット要求]

* **Original Japanese DMS Title**: `待機室・エディット要求`
* **Raw DMS Script**: [`30330.dms`](../dms/30330.dms)
* **Legacy Delphi Class**: `TMsgWaitRoomEditReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30330.dms
function parse(packet){
	with(packet){
		setTitle("待機室・エディット要求");
		readInt32("snEpisode");
		readWStr(42, "title");
		readByte("bPKMode");
		readByte("bLimitMilk");
		readByte("cntTeam");
		readBinary(1, "(padding)");
		readWStr(18, "password");
	}
}
```

---

## Opcode 0x767B (30331): `MsgUnknown_0x767B` [待機室・エディット返答]

* **Original Japanese DMS Title**: `待機室・エディット返答`
* **Raw DMS Script**: [`30331.dms`](../dms/30331.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x767B` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30331.dms
function parse(packet){
	with(packet){
		setTitle("待機室・エディット返答");
		readInt32("戻り値");
	}
}
```

---

## Opcode 0x767C (30332): `MsgUnknown_0x767C` [ルームキャラ情報]

* **Original Japanese DMS Title**: `ルームキャラ情報`
* **Raw DMS Script**: [`30332.dms`](../dms/30332.dms)
* **Legacy Delphi Class**: `TMsgWaitRoomEnterPcNtf` (Address: `005ABA94`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30332.dms
import struct.*;
function parse(packet){
	packet.setTitle("ルームキャラ情報");
	pc_info(packet);
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgWaitRoomEnterPcNtf.Create(Chara:TChara)
005ABA94    push        ebx
 005ABA95    push        esi
 005ABA96    push        edi
 005ABA97    test        dl,dl
>005ABA99    je          005ABAA3
 005ABA9B    add         esp,0FFFFFFF0
 005ABA9E    call        @ClassCreate
 005ABAA3    mov         esi,ecx
 005ABAA5    mov         ebx,edx
 005ABAA7    mov         edi,eax
 005ABAA9    mov         dx,767C
 005ABAAD    mov         eax,edi
 005ABAAF    call        TYgPacket.WriteID
 005ABAB4    mov         edx,esi
 005ABAB6    mov         eax,edi
 005ABAB8    call        TYgPacket.WritePCInfo
 005ABABD    mov         eax,edi
 005ABABF    test 
```

---

## Opcode 0x767D (30333): `MsgUnknown_0x767D` [待機室・キャラ退出通知]

* **Original Japanese DMS Title**: `待機室・キャラ退出通知`
* **Raw DMS Script**: [`30333.dms`](../dms/30333.dms)
* **Legacy Delphi Class**: `TMsgWaitRoomLeavePcNtf` (Address: `005ABAD8`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30333.dms
function parse(packet){
	with(packet){
		setTitle("待機室・キャラ退出通知");
		readInt32("idChar");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgWaitRoomLeavePcNtf.Create(ID:Integer)
005ABAD8    push        ebx
 005ABAD9    push        esi
 005ABADA    push        edi
 005ABADB    test        dl,dl
>005ABADD    je          005ABAE7
 005ABADF    add         esp,0FFFFFFF0
 005ABAE2    call        @ClassCreate
 005ABAE7    mov         esi,ecx
 005ABAE9    mov         ebx,edx
 005ABAEB    mov         edi,eax
 005ABAED    mov         dx,767D
 005ABAF1    mov         eax,edi
 005ABAF3    call        TYgPacket.WriteID
 005ABAF8    mov         edx,esi
 005ABAFA    mov         eax,edi
 005ABAFC    call        TYgPacket.WriteInt32
 005ABB01    mov         eax,edi
 005ABB03    test  
```

---

## Opcode 0x767E (30334): `MsgUnknown_0x767E` [待機室キャラ状態通知]

* **Original Japanese DMS Title**: `待機室キャラ状態通知`
* **Raw DMS Script**: [`30334.dms`](../dms/30334.dms)
* **Legacy Delphi Class**: `TMsgWaitRoomPcStatusNtf` (Address: `005ABB1C`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30334.dms
function parse(packet){
	with(packet){
		setTitle("待機室キャラ状態通知");
		readInt32("idChar");
		readByte("status");
		readBinary(3, "(padding)");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgWaitRoomPcStatusNtf.Create(Chara:TChara)
005ABB1C    push        ebx
 005ABB1D    push        esi
 005ABB1E    push        edi
 005ABB1F    test        dl,dl
>005ABB21    je          005ABB2B
 005ABB23    add         esp,0FFFFFFF0
 005ABB26    call        @ClassCreate
 005ABB2B    mov         esi,ecx
 005ABB2D    mov         ebx,edx
 005ABB2F    mov         edi,eax
 005ABB31    mov         dx,767E
 005ABB35    mov         eax,edi
 005ABB37    call        TYgPacket.WriteID
 005ABB3C    mov         edx,dword ptr [esi+0E0];TChara.ID:Integer
 005ABB42    mov         eax,edi
 005ABB44    call        TYgPacket.WriteInt32
 005ABB49    cmp  
```

---

## Opcode 0x767F (30335): `MsgUnknown_0x767F` [待機室長通知]

* **Original Japanese DMS Title**: `待機室長通知`
* **Raw DMS Script**: [`30335.dms`](../dms/30335.dms)
* **Legacy Delphi Class**: `TMsgWaitRoomSelectBossNtf` (Address: `005ABBA0`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30335.dms
function parse(packet){
	with(packet){
		setTitle("待機室長通知");
		readInt32("idChar");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgWaitRoomSelectBossNtf.Create(Chara:TChara)
005ABBA0    push        ebx
 005ABBA1    push        esi
 005ABBA2    push        edi
 005ABBA3    test        dl,dl
>005ABBA5    je          005ABBAF
 005ABBA7    add         esp,0FFFFFFF0
 005ABBAA    call        @ClassCreate
 005ABBAF    mov         esi,ecx
 005ABBB1    mov         ebx,edx
 005ABBB3    mov         edi,eax
 005ABBB5    mov         dx,767F
 005ABBB9    mov         eax,edi
 005ABBBB    call        TYgPacket.WriteID
 005ABBC0    mov         edx,dword ptr [esi+0E0];TChara.ID:Integer
 005ABBC6    mov         eax,edi
 005ABBC8    call        TYgPacket.WriteInt32
 005ABBCD    mov  
```

---

## Opcode 0x7680 (30336): `MsgWaitRoomSelectTeamReq` [待機室・チーム選択]

* **Original Japanese DMS Title**: `待機室・チーム選択`
* **Raw DMS Script**: [`30336.dms`](../dms/30336.dms)
* **Legacy Delphi Class**: `TMsgWaitRoomSelectTeamNtf` (Address: `005ABBE8`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30336.dms
function parse(packet){
	with(packet){
		setTitle("待機室・チーム選択");
		readUInt32("キャラID");
		readChar("チーム番号");
		readBinary(3,"(padding)");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgWaitRoomSelectTeamNtf.Create(Chara:TChara)
005ABBE8    push        ebx
 005ABBE9    push        esi
 005ABBEA    push        edi
 005ABBEB    test        dl,dl
>005ABBED    je          005ABBF7
 005ABBEF    add         esp,0FFFFFFF0
 005ABBF2    call        @ClassCreate
 005ABBF7    mov         esi,ecx
 005ABBF9    mov         ebx,edx
 005ABBFB    mov         edi,eax
 005ABBFD    mov         dx,7680
 005ABC01    mov         eax,edi
 005ABC03    call        TYgPacket.WriteID
 005ABC08    mov         edx,dword ptr [esi+0E0];TChara.ID:Integer
 005ABC0E    mov         eax,edi
 005ABC10    call        TYgPacket.WriteInt32
 005ABC15    movzx
```

---

## Opcode 0x7681 (30337): `MsgUnknown_0x7681` [待機室・アイテム変更通知]

* **Original Japanese DMS Title**: `待機室・アイテム変更通知`
* **Raw DMS Script**: [`30337.dms`](../dms/30337.dms)
* **Legacy Delphi Class**: `TMsgWaitRoomItemChangeNtf` (Address: `005ABC4C`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30337.dms
function parse(packet){
	with(packet){
		setTitle("待機室・アイテム変更通知");
		readInt32("idChar");
		readByte("weapon");
		readBinary(3, "(padding)");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgWaitRoomItemChangeNtf.Create(Chara:TChara)
005ABC4C    push        ebp
 005ABC4D    mov         ebp,esp
 005ABC4F    push        ecx
 005ABC50    push        ebx
 005ABC51    push        esi
 005ABC52    push        edi
 005ABC53    test        dl,dl
>005ABC55    je          005ABC5F
 005ABC57    add         esp,0FFFFFFF0
 005ABC5A    call        @ClassCreate
 005ABC5F    mov         esi,ecx
 005ABC61    mov         byte ptr [ebp-1],dl
 005ABC64    mov         ebx,eax
 005ABC66    mov         dx,7681
 005ABC6A    mov         eax,ebx
 005ABC6C    call        TYgPacket.WriteID
 005ABC71    mov         edx,dword ptr [esi+0E0];TChara.ID:In
```

---

## Opcode 0x7682 (30338): `MsgWaitRoomInviteReq` [待機室・招待検証通知]

* **Original Japanese DMS Title**: `待機室・招待検証通知`
* **Raw DMS Script**: [`30338.dms`](../dms/30338.dms)
* **Legacy Delphi Class**: `TMsgWaitRoomInviteReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30338.dms
function parse(packet){
	with(packet){
		setTitle("待機室・招待検証通知");
		readInt32("type");
		readInt32("idChar");
	}
}
```

---

## Opcode 0x7683 (30339): `MsgUnknown_0x7683` [待機室・招待検証返答]

* **Original Japanese DMS Title**: `待機室・招待検証返答`
* **Raw DMS Script**: [`30339.dms`](../dms/30339.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x7683` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30339.dms
function parse(packet){
	with(packet){
		setTitle("待機室・招待検証返答");
		readInt32("キャラID");
		readByte("戻り値");
		readBinary(3, "(padding)");
	}
}
```

---

## Opcode 0x7684 (30340): `MsgUnknown_0x7684` [待機室・招待通知]

* **Original Japanese DMS Title**: `待機室・招待通知`
* **Raw DMS Script**: [`30340.dms`](../dms/30340.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x7684` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30340.dms
function parse(packet){
	with(packet){
		setTitle("待機室・招待通知");
		readInt32("type");
		readInt32("idChar");
	}
}
```

---

## Opcode 0x7686 (30342): `MsgUnknown_0x7686` [待機室・招待申請通知]

* **Original Japanese DMS Title**: `待機室・招待申請通知`
* **Raw DMS Script**: [`30342.dms`](../dms/30342.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x7686` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30342.dms
import struct.*;
function parse(packet){
	with(packet){
		setTitle("待機室・招待申請通知");
		pc_info(packet);
		room_info(packet);
		readWStr(18,"password");
		readBinary(2, "(padding)");
	}
}
```

---

## Opcode 0x7687 (30343): `MsgUnknown_0x7687` [待機室・招待申請通知]

* **Original Japanese DMS Title**: `待機室・招待申請通知`
* **Raw DMS Script**: [`30343.dms`](../dms/30343.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0x7687` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30343.dms
function parse(packet){
	with(packet){
		setTitle("待機室・招待申請通知");
		readByte("accept");
	}
}
```

---

## Opcode 0x768C (30348): `MsgWaitRoomReadyStartReq` [待機室・準備通知]

* **Original Japanese DMS Title**: `待機室・準備通知`
* **Raw DMS Script**: [`30348.dms`](../dms/30348.dms)
* **Legacy Delphi Class**: `TMsgWaitRoomReadyStartReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30348.dms
function parse(packet){
	with(packet){
		setTitle("待機室・準備通知");
		readInt32("idChar");
		readInt32("bReady");
	}
}
```

---

## Opcode 0x768D (30349): `MsgWaitRoomLeaveReq` [待機室・スタート通知]

* **Original Japanese DMS Title**: `待機室・スタート通知`
* **Raw DMS Script**: [`30349.dms`](../dms/30349.dms)
* **Legacy Delphi Class**: `TMsgWaitRoomLeaveReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30349.dms
function parse(packet){
	with(packet){
		setTitle("待機室・スタート通知");
	}
}
```

---

## Opcode 0x76D4 (30420): `MsgUnknown_0x76D4` [アトラクションプレイ開始通知]

* **Original Japanese DMS Title**: `アトラクションプレイ開始通知`
* **Raw DMS Script**: [`30420.dms`](../dms/30420.dms)
* **Legacy Delphi Class**: `TMsgWaitRoomTestInviteAns` (Address: `005ABD24`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/30420.dms
function parse(packet){
	with(packet){
		setTitle("アトラクションプレイ開始通知");
		readInt32("キャラID");
		readWord("X");
		readWord("Y");
		readByte("チーム数");
		readBinary(3, "(padding)");
	}
}
```

### Delphi Disassembly Cross-Reference

```pascal
// TMsgWaitRoomTestInviteAns.Create(?:?)
005ABD24    push        ebp
 005ABD25    mov         ebp,esp
 005ABD27    push        ebx
 005ABD28    push        esi
 005ABD29    push        edi
 005ABD2A    test        dl,dl
>005ABD2C    je          005ABD36
 005ABD2E    add         esp,0FFFFFFF0
 005ABD31    call        @ClassCreate
 005ABD36    mov         esi,ecx
 005ABD38    mov         ebx,edx
 005ABD3A    mov         edi,eax
 005ABD3C    mov         dx,76D4
 005ABD40    mov         eax,edi
 005ABD42    call        TYgPacket.WriteID
 005ABD47    mov         edx,dword ptr [esi+0E0];TChara.ID:Integer
 005ABD4D    mov         eax,edi
 0
```

---
