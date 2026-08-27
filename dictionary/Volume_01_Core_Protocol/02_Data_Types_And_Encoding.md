# Volume 1, Chapter 2: Data Types, Coordinate Geometry & String Encodings

## 1. Primitive Binary Types

Yogurting's network serialization operates exclusively in **Little-Endian (x86 standard)** format:

| Type Name | Wire Size | DMS Method | C# Equivalent | Description / Value Range |
| :--- | :--- | :--- | :--- | :--- |
| **`Byte`** | 1 Byte | `readByte` | `byte` | 8-bit unsigned integer (`0` to `255`). |
| **`Char`** | 1 Byte | `readChar` | `sbyte` | 8-bit signed integer (`-128` to `127`). |
| **`Word`** | 2 Bytes | `readWord` | `ushort` | 16-bit unsigned integer (`0` to `65,535`). |
| **`Short`** | 2 Bytes | `readShort` | `short` | 16-bit signed integer (`-32,768` to `32,767`). |
| **`Int32`** | 4 Bytes | `readInt32` | `int` | 32-bit signed integer. Primary type for Entity IDs, Monster Types, Item Types, and HP. |
| **`UInt32`** | 4 Bytes | `readUInt32` | `uint` | 32-bit unsigned integer. Used for timestamps, version numbers, and phone numbers. |
| **`Int64`** | 8 Bytes | `readInt64` | `long` | 64-bit signed integer. Used for Money (Sod), Bank Balance, Item Serial IDs, and Taff. |
| **`Single`** | 4 Bytes | `readSingle` | `float` | 32-bit IEEE-754 floating point. Used for Attack/Movement speed multipliers and Calorie rates. |
| **`LongBool`** | 4 Bytes | `readInt32` | `bool` (4B) | 32-bit boolean value: `0x00000000` = False, `0x00000001` = True. |

---

## 2. String Encodings & Serialization Patterns

Yogurting uses **two distinct string serialization patterns**:

```text
Pattern A: Fixed-Length Buffer (readWStr / WriteWStr)
+-------------------------------------------------------------+
| UTF-16LE Character Bytes (2 bytes per char) + Null Padding |
| Size is always exactly (CharCount * 2) bytes               |
+-------------------------------------------------------------+

Pattern B: Length-Prefixed String (readWStrWithLen / WriteWStrWithLen)
+------------------------+------------------------------------+
| Length Prefix (UInt16) | UTF-16LE Character Bytes           |
| [2 Bytes in chars]     | [Length * 2 bytes]                 |
+------------------------+------------------------------------+
```

### 1. Fixed-Length Unicode Strings (`WriteWStr(charCount)`)
* **Encoding**: `UTF-16LE` (2 bytes per character).
* **Wire Size**: Exactly $\text{charCount} \times 2$ bytes.
* **Behavior**: If the text is shorter than `charCount`, the remaining space is padded with `0x0000` null bytes.
* **Common Uses**:
  * Character Name in Login/CharDisp: `WriteWStr(13)` $\rightarrow$ **26 bytes**.
  * World Name: `WriteWStr(11)` $\rightarrow$ **22 bytes**.
  * School Name: `WriteWStr(33)` $\rightarrow$ **66 bytes**.
  * Episode Room Title: `WriteWStr(21)` $\rightarrow$ **42 bytes**.
  * Club/Guild Name: `WriteWStr(13)` $\rightarrow$ **26 bytes**.

### 2. Length-Prefixed Unicode Strings (`WriteWStrWithLen`)
* **Encoding**: `UTF-16LE` preceded by a 2-byte (`UInt16`) character count.
* **Wire Layout**: `[UInt16 LengthInChars] [UTF-16LE Bytes (Length * 2)]`
* **Common Uses**:
  * In-game Chat Messages (`0x7963`)
  * System Notices and Announcements (`0x7964`)
  * NPC Dialogue strings (`0x5229`)

---

## 3. Coordinate Systems & World Geometry

Yogurting maps use two coordinate representations:

### 1. Grid Units (`MapPoint` - 4 Bytes total)
* **Wire Layout**: `[UInt16 X] [UInt16 Y]`
* **Definition**: Represents character, monster, and waypoint positions in native 2D grid units.
* **Range**: `X: 0..65535`, `Y: 0..65535`.
* **Example**: Gate 5 in Estiva Plaza is at Grid `X: 220, Y: 99`.

### 2. Floating-Point World Units (`Vector3` - 12 Bytes total)
* **Wire Layout**: `[Float32 X] [Float32 Y] [Float32 Z]`
* **Conversion Formula**:
  $$\text{World } X = \text{Grid } X \times 4.0f$$
  $$\text{World } Y = \text{Grid } Y \times 4.0f$$
* **Usage**: Object and Kiosk terminal positioning (`0x521B`).

### 3. Direction Representations
* **Vector Direction (`TPoint` - 8 Bytes)**: `[Int32 DirX] [Int32 DirY]` (Values typically `-1`, `0`, `1`).
* **Heading Index (`Byte Dir` - 1 Byte)**: 8-way compass direction indexed from `0` to `7`:
  * `0` = North, `1` = North-East, `2` = East, `3` = South-East, `4` = South, `5` = South-West, `6` = West, `7` = North-West.

---

## 4. Universal Error Code Convention (`WriteEC`)

Standard response packets in Yogurting (`*Ans`) contain a 4-byte Error Code field at offset `0x00` (`+0` of payload):

```text
ErrorCode = 0x00000000 (0) -> SUCCESS (Operation succeeded, payload follows)
ErrorCode > 0x00000000     -> FAILURE (Error ID / Reason Code)
```

Common Error Code Values:
* `EC = 0`: Success
* `EC = 1`: General / Unknown Error (Client displays modal dialog)
* `EC = 2`: Insufficient Funds / Money
* `EC = 3`: Inventory Full
* `EC = 4`: Invalid Level / Grade Requirement
