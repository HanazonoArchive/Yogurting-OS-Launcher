# Volume 1, Chapter 1: Packet Header & Framing Specification

## 1. Physical Wire Protocol

All communication between the Yogurting game client (`Yogurting.exe` / `LuGameEngineDX9.dll`) and the server daemon processes is transported over persistent, asynchronous **TCP streams**.

Every packet on the wire follows a strict **Prefix-Framed Binary Structure**:

```text
+-------------------------+-------------------+------------------------------------------+
|  Length Prefix (4B)     |   Opcode (2B)     |   Payload Data (N Bytes)                 |
|  [Little-Endian UInt32] |   [Little-Endian] |   [Length Prefix value determines size]  |
+-------------------------+-------------------+------------------------------------------+
| <---------------- Total Message Size = Length Prefix + 6 Bytes ---------------------> |
```

---

## 2. Framing Fields Breakdown

### Field 1: Length Prefix (`UInt32` - 4 Bytes)
* **Byte Offset**: `0x00` – `0x03`
* **Data Type**: `UInt32` (32-bit unsigned integer, Little-Endian)
* **Definition**: Represents the **length of the Payload Data ONLY** (excluding the 4-byte prefix and excluding the 2-byte opcode).
* **Formula**:
  $$\text{Total Raw TCP Stream Bytes} = \text{Length Prefix} + 6$$
  $$\text{Payload Length} = \text{Length Prefix}$$

> [!IMPORTANT]
> A Length Prefix of `0x00000000` (4 null bytes) indicates a **Zero-Payload Packet** containing ONLY the 2-byte Opcode (Total wire size = 6 bytes). Examples include `MsgEnterScsNtf` (`0x5212`), `MsgGameEmoteReq` (`0x795B`), and `MsgGameByulShopBeginReq` (`0x5233`).

---

### Field 2: Opcode (`UInt16` - 2 Bytes)
* **Byte Offset**: `0x04` – `0x05`
* **Data Type**: `UInt16` (16-bit unsigned integer, Little-Endian)
* **Definition**: Unique 16-bit identifier identifying the message type and handler routine.
* **Format & Hex/Decimal Notation**:
  * In AnaYg `.dms` scripts, opcodes are identified by their **Decimal Value** (e.g. `31001.dms`).
  * In Delphi and C# source code, opcodes are identified in **Hexadecimal** (e.g. `0x7919`).
  * Example: Decimal `31001` $\equiv$ Hex `0x7919` $\rightarrow$ Wire bytes: `19 79` (Little-Endian).

---

### Field 3: Payload (`Byte[Length Prefix]` - N Bytes)
* **Byte Offset**: `0x06` to `0x06 + Length Prefix - 1`
* **Data Type**: Raw structured binary data defined by the specific opcode specification.

---

## 3. Wire Stream Examples

### Example A: Zero-Payload Handshake Packet (`0x5212 / 21010: MsgEnterScsNtf`)
```hex
00 00 00 00  -> Length Prefix = 0 bytes payload
12 52        -> Opcode = 0x5212 (21010)
```
*Total wire bytes*: `6 bytes`.

---

### Example B: Fixed 4-Byte Payload (`0x5234 / 21044: MsgGameByulShopBeginAns`)
```hex
04 00 00 00  -> Length Prefix = 4 bytes payload
34 52        -> Opcode = 0x5234 (21044)
00 00 00 00  -> Payload: ErrorCode = 0 (Success)
```
*Total wire bytes*: `10 bytes`.

---

### Example C: Variable Length Packet (`0x7596 / 30102: MsgLoginAuthReq`)
```hex
58 00 00 00  -> Length Prefix = 88 bytes (0x58) payload
96 75        -> Opcode = 0x7596 (30102)
[88 bytes of username, MD5 password hash, and client token data...]
```
*Total wire bytes*: `94 bytes`.

---

## 4. Legacy Delphi Implementation Reference (`TYgPacket`)

In the legacy Delphi server (`server_legacy/DELPHI PROJECT/_Unit47.pas`), packet construction is managed by `TYgPacket`:

```pascal
procedure TYgPacket.WriteID(ID: Word);
begin
    Self.FBuffer.WriteWord(ID); // Writes 2-byte Opcode
end;

function TYgPacket.GetBuffer: PAnsiChar;
begin
    // The length prefix (Payload Size) is automatically computed:
    // LengthPrefix = BufferLength - 2 (subtracting the 2-byte opcode)
    Result := Self.FBuffer.Memory;
end;
```

---

## 5. Modern C# Pipeline Implementation (`PacketWriter` / `PacketReader`)

In `server_modern` (`src/Yogurting.Core/Network/PacketWriter.cs`):

```csharp
public sealed class PacketWriter : IDisposable
{
    private readonly MemoryStream _stream = new();
    private readonly BinaryWriter _writer;

    public static PacketWriter Create(PacketOpcode opcode)
    {
        var pw = new PacketWriter();
        pw._writer.Write((uint)0); // Placeholder for 4-byte Length Prefix
        pw._writer.Write((ushort)opcode); // 2-byte Opcode
        return pw;
    }

    public byte[] Build()
    {
        _writer.Flush();
        byte[] buffer = _stream.ToArray();
        uint payloadLength = (uint)(buffer.Length - 6);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(0, 4), payloadLength);
        return buffer;
    }
}
```
