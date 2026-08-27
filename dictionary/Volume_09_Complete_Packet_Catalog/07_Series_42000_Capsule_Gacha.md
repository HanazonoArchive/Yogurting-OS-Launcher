# Volume 9: Series 42000: Capsule Gacha Vending Machines

This chapter provides the complete, byte-for-byte specifications for all 7 opcodes in this range.

---

## Opcode 0xA411 (42001): `MsgUnknown_0xA411` [カプセル販売機参加通知]

* **Original Japanese DMS Title**: `カプセル販売機参加通知`
* **Raw DMS Script**: [`42001.dms`](../dms/42001.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0xA411` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/42001.dms
function parse(packet){
	with(packet){
		setTitle("カプセル販売機参加通知");
		readWord("id");
	}
}
```

---

## Opcode 0xA412 (42002): `MsgUnknown_0xA412` [カプセル販売機商品情報通知]

* **Original Japanese DMS Title**: `カプセル販売機商品情報通知`
* **Raw DMS Script**: [`42002.dms`](../dms/42002.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0xA412` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/42002.dms
function parse(packet){
	with(packet){
		setTitle("カプセル販売機商品情報通知");
		readInt32("idMachine");
		readInt64("price");
		var cnt = readWord("listProductsCount");
		for(var i = 1; i <= cnt; i++){
			readInt32("  bSecret" + i.toString());
			readInt32("  typeItem+typeNum" + i.toString());
			readInt64("  amount" + i.toString());
		}
		readInt32("totalAmount");
	}
}
```

---

## Opcode 0xA413 (42003): `MsgGameCapsuleBuyReq` [カプセル販売機購入要求]

* **Original Japanese DMS Title**: `カプセル販売機購入要求`
* **Raw DMS Script**: [`42003.dms`](../dms/42003.dms)
* **Legacy Delphi Class**: `TMsgGameCapsuleBuyReq` (Address: `N/A`)
* **Direction**: `Client -> Server`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/42003.dms
function parse(packet){
	with(packet){
		setTitle("カプセル販売機購入要求");
		readInt32("machineSN");
	}
}
```

---

## Opcode 0xA414 (42004): `MsgGameCapsuleBuyAns` [カプセル販売機購入返答]

* **Original Japanese DMS Title**: `カプセル販売機購入返答`
* **Raw DMS Script**: [`42004.dms`](../dms/42004.dms)
* **Legacy Delphi Class**: `TMsgGameCapsuleBuyAns` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/42004.dms
function parse(packet){
	with(packet){
		setTitle("カプセル販売機購入返答");
		readInt32("戻り値");
		readInt32("unknown");
		readInt32("typeItem+typeNum");
		readWord("count or Dim1Index");
		readWord("invalid or Dim2Index");
		readInt32("invalid or idItem");
		readInt64("price");
		readInt64("resultMoney");
		readInt64("totalAmount");
	}
}
```

---

## Opcode 0xA415 (42005): `MsgGameCapsuleExitNtf` [カプセル販売機退出通知]

* **Original Japanese DMS Title**: `カプセル販売機退出通知`
* **Raw DMS Script**: [`42005.dms`](../dms/42005.dms)
* **Legacy Delphi Class**: `TMsgGameCapsuleExitNtf` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/42005.dms
function parse(packet){
	with(packet){
		setTitle("カプセル販売機退出通知");
		readWord("id");
	}
}
```

---

## Opcode 0xA416 (42006): `MsgUnknown_0xA416` [カプセル販売機退出返答]

* **Original Japanese DMS Title**: `カプセル販売機退出返答`
* **Raw DMS Script**: [`42006.dms`](../dms/42006.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0xA416` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/42006.dms
function parse(packet){
	with(packet){
		setTitle("カプセル販売機退出返答");
		readInt32("戻り値");
		readWord("id");
		readBinary(2, "(padding)");
	}
}
```

---

## Opcode 0xA419 (42009): `MsgUnknown_0xA419` [ダンスアイテム使用通知]

* **Original Japanese DMS Title**: `ダンスアイテム使用通知`
* **Raw DMS Script**: [`42009.dms`](../dms/42009.dms)
* **Legacy Delphi Class**: `TMsgUnknown_0xA419` (Address: `N/A`)
* **Direction**: `Server -> Client`

### Raw AnaYg Parser Script

```javascript
// Source: server_modern/dictionary/dms/42009.dms
function parse(packet){
	with(packet){
		setTitle("ダンスアイテム使用通知");
		readInt32("キャラID");
		readInt32("typeCoItem");
	}
}
```

---
