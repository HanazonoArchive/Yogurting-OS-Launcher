using System;
using System.IO;
using System.Text;

namespace Yogurting.Core.Network
{
    /// <summary>
    /// High-performance, zero-allocation binary packet reader with bounds checking and multi-encoding string support.
    /// Operates over <see cref="ReadOnlyMemory{Byte}"/> slices directly from network buffers.
    /// </summary>
    public sealed class PacketReader
    {
        private readonly ReadOnlyMemory<byte> _memory;
        private int _position;

        public PacketReader(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            _memory = data;
            _position = 0;
        }

        public PacketReader(byte[] data, int offset, int count)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            _memory = new ReadOnlyMemory<byte>(data, offset, count);
            _position = 0;
        }

        public PacketReader(ReadOnlyMemory<byte> memory)
        {
            _memory = memory;
            _position = 0;
        }

        public int Position => _position;
        public int Length => _memory.Length;
        public int Remaining => _memory.Length - _position;
        public ReadOnlyMemory<byte> Memory => _memory;
        public ReadOnlySpan<byte> Span => _memory.Span;

        public byte ReadByte()
        {
            EnsureBytes(1);
            return _memory.Span[_position++];
        }

        public ReadOnlySpan<byte> ReadBytes(int count)
        {
            EnsureBytes(count);
            var span = _memory.Span.Slice(_position, count);
            _position += count;
            return span;
        }

        public short ReadInt16()
        {
            EnsureBytes(2);
            short value = System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(_memory.Span.Slice(_position, 2));
            _position += 2;
            return value;
        }

        public ushort ReadUInt16()
        {
            EnsureBytes(2);
            ushort value = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(_memory.Span.Slice(_position, 2));
            _position += 2;
            return value;
        }

        public int ReadInt32()
        {
            EnsureBytes(4);
            int value = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(_memory.Span.Slice(_position, 4));
            _position += 4;
            return value;
        }

        public uint ReadUInt32()
        {
            EnsureBytes(4);
            uint value = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(_memory.Span.Slice(_position, 4));
            _position += 4;
            return value;
        }

        public long ReadInt64()
        {
            EnsureBytes(8);
            long value = System.Buffers.Binary.BinaryPrimitives.ReadInt64LittleEndian(_memory.Span.Slice(_position, 8));
            _position += 8;
            return value;
        }

        public ulong ReadUInt64()
        {
            EnsureBytes(8);
            ulong value = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(_memory.Span.Slice(_position, 8));
            _position += 8;
            return value;
        }

        public float ReadSingle()
        {
            EnsureBytes(4);
            float value = System.Buffers.Binary.BinaryPrimitives.ReadSingleLittleEndian(_memory.Span.Slice(_position, 4));
            _position += 4;
            return value;
        }

        public bool ReadBoolean() => ReadByte() != 0;

        public string ReadUnicodeString(int maxCharCount = 50)
        {
            int maxByteCount = maxCharCount * 2;
            int available = Math.Min(Remaining, maxByteCount);
            if (available < 2) return string.Empty;

            ReadOnlySpan<byte> span = _memory.Span.Slice(_position, available);
            int nullIdx = 0;
            for (int i = 0; i < available - 1; i += 2)
            {
                if (span[i] == 0 && span[i + 1] == 0)
                    break;
                nullIdx = i + 2;
            }

            string result = Encoding.Unicode.GetString(span.Slice(0, nullIdx));
            _position += available;
            return result;
        }

        public string ReadFixedString(int length, Encoding? encoding = null)
        {
            EnsureBytes(length);
            encoding ??= Encoding.UTF8;

            ReadOnlySpan<byte> span = _memory.Span.Slice(_position, length);
            int actualLength = 0;
            for (int i = 0; i < length; i++)
            {
                if (span[i] == 0) break;
                actualLength++;
            }

            string result = encoding.GetString(span.Slice(0, actualLength));
            _position += length;
            return result;
        }

        public string ReadNullTerminatedString(Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;
            int start = _position;
            ReadOnlySpan<byte> span = _memory.Span;
            while (_position < span.Length && span[_position] != 0)
            {
                _position++;
            }

            int length = _position - start;
            string result = encoding.GetString(span.Slice(start, length));

            if (_position < span.Length)
            {
                _position++; // Skip null terminator
            }

            return result;
        }

        public static string ReadFixedWString(byte[] data, int offset, int byteCount)
        {
            if (data == null || offset < 0 || offset >= data.Length || byteCount <= 0)
                return string.Empty;

            int available = Math.Min(byteCount, data.Length - offset);
            int nullIdx = 0;
            for (int i = 0; i < available - 1; i += 2)
            {
                if (data[offset + i] == 0 && data[offset + i + 1] == 0)
                    break;
                nullIdx = i + 2;
            }

            return Encoding.Unicode.GetString(data, offset, nullIdx);
        }

        public static string ReadFixedAnsiString(byte[] data, int offset, int byteCount, Encoding? encoding = null)
        {
            if (data == null || offset < 0 || offset >= data.Length || byteCount <= 0)
                return string.Empty;

            encoding ??= Encoding.ASCII;
            int available = Math.Min(byteCount, data.Length - offset);
            int actualLength = 0;
            for (int i = 0; i < available; i++)
            {
                if (data[offset + i] == 0) break;
                actualLength++;
            }

            return encoding.GetString(data, offset, actualLength);
        }

        public void Skip(int count)
        {
            EnsureBytes(count);
            _position += count;
        }

        public void Seek(int position)
        {
            if (position < 0 || position > _memory.Length)
                throw new ArgumentOutOfRangeException(nameof(position));
            _position = position;
        }

        private void EnsureBytes(int count)
        {
            if (_position + count > _memory.Length)
            {
                throw new EndOfStreamException($"Attempted to read {count} bytes past buffer length {_memory.Length} at position {_position}.");
            }
        }
    }
}
