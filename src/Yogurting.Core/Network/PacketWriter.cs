using System;
using System.IO;
using System.Text;

namespace Yogurting.Core.Network
{
    /// <summary>
    /// Binary packet writer for serializing Yogurting network packets.
    /// Bit-for-bit exact reproduction of Delphi TYgPacket layout:
    /// [4 bytes: PayloadLength (StreamSize - 6)][2 bytes: Opcode][Payload bytes...].
    /// </summary>
    public sealed class PacketWriter : IDisposable
    {
        private readonly MemoryStream _stream;
        private readonly BinaryWriter _writer;

        public PacketWriter()
        {
            _stream = new MemoryStream();
            _writer = new BinaryWriter(_stream, Encoding.UTF8);
        }

        public PacketWriter(ushort opcode) : this()
        {
            // Allocate 4-byte length placeholder + 2-byte opcode
            WriteInt32(0);
            WriteUInt16(opcode);
        }

        public PacketWriter(PacketOpcode opcode) : this((ushort)opcode)
        {
        }

        public static PacketWriter Create(PacketOpcode opcode) => new PacketWriter(opcode);
        public static PacketWriter Create(ushort opcode) => new PacketWriter(opcode);

        public void WriteByte(byte value) => _writer.Write(value);
        public void WriteBytes(byte[] value) => _writer.Write(value);
        public void WriteInt16(short value) => _writer.Write(value);
        public void WriteUInt16(ushort value) => _writer.Write(value);
        public void WriteInt32(int value) => _writer.Write(value);
        public void WriteUInt32(uint value) => _writer.Write(value);
        public void WriteInt64(long value) => _writer.Write(value);
        public void WriteUInt64(ulong value) => _writer.Write(value);
        public void WriteSingle(float value) => _writer.Write(value);
        public void WriteFloat(float value) => _writer.Write(value);
        public void WriteDouble(double value) => _writer.Write(value);
        public void WriteBoolean(bool value) => _writer.Write(value);

        public void WriteUnicodeString(string value, int fixedChars = 0)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(value ?? string.Empty);
            if (fixedChars > 0)
            {
                byte[] padded = new byte[fixedChars * 2];
                Array.Copy(bytes, 0, padded, 0, Math.Min(bytes.Length, padded.Length));
                WriteBytes(padded);
            }
            else
            {
                WriteBytes(bytes);
                WriteUInt16(0); // Null terminator
            }
        }

        public void WriteUnicodeStringWithLength(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                WriteUInt16(2); // (0 + 1) * 2 = 2 bytes
                WriteUInt16(0); // Null terminator
                return;
            }
            ushort byteLen = (ushort)((value.Length + 1) * 2);
            WriteUInt16(byteLen);
            byte[] bytes = Encoding.Unicode.GetBytes(value);
            WriteBytes(bytes);
            WriteUInt16(0); // Null terminator
        }

        public void WriteAnsiString(string value, int fixedChars = 0)
        {
            byte[] bytes = Encoding.Latin1.GetBytes(value ?? string.Empty);
            if (fixedChars > 0)
            {
                byte[] padded = new byte[fixedChars];
                Array.Copy(bytes, 0, padded, 0, Math.Min(bytes.Length, padded.Length));
                WriteBytes(padded);
            }
            else
            {
                WriteBytes(bytes);
                WriteByte(0); // Null terminator
            }
        }

        public byte[] Build()
        {
            _writer.Flush();
            byte[] data = _stream.ToArray();
            if (data.Length >= 6)
            {
                // Write payload length (TotalSize - 6) at offset 0
                int payloadLen = data.Length - 6;
                byte[] lenBytes = BitConverter.GetBytes(payloadLen);
                Buffer.BlockCopy(lenBytes, 0, data, 0, 4);
            }
            return data;
        }

        public byte[] ToArray() => Build();

        public void Dispose()
        {
            _writer.Dispose();
            _stream.Dispose();
        }
    }
}
