using System;
using System.IO;
using System.Text;

namespace Yogurting.Core.Network
{
    /// <summary>
    /// Binary packet reader with bounds checking and multi-encoding string support.
    /// </summary>
    public sealed class PacketReader
    {
        private readonly byte[] _data;
        private int _position;

        public PacketReader(byte[] data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _position = 0;
        }

        public int Position => _position;
        public int Length => _data.Length;
        public int Remaining => _data.Length - _position;

        public byte ReadByte()
        {
            EnsureBytes(1);
            return _data[_position++];
        }

        public ReadOnlySpan<byte> ReadBytes(int count)
        {
            EnsureBytes(count);
            var span = _data.AsSpan(_position, count);
            _position += count;
            return span;
        }

        public short ReadInt16()
        {
            EnsureBytes(2);
            short value = BitConverter.ToInt16(_data, _position);
            _position += 2;
            return value;
        }

        public ushort ReadUInt16()
        {
            EnsureBytes(2);
            ushort value = BitConverter.ToUInt16(_data, _position);
            _position += 2;
            return value;
        }

        public int ReadInt32()
        {
            EnsureBytes(4);
            int value = BitConverter.ToInt32(_data, _position);
            _position += 4;
            return value;
        }

        public uint ReadUInt32()
        {
            EnsureBytes(4);
            uint value = BitConverter.ToUInt32(_data, _position);
            _position += 4;
            return value;
        }

        public long ReadInt64()
        {
            EnsureBytes(8);
            long value = BitConverter.ToInt64(_data, _position);
            _position += 8;
            return value;
        }

        public ulong ReadUInt64()
        {
            EnsureBytes(8);
            ulong value = BitConverter.ToUInt64(_data, _position);
            _position += 8;
            return value;
        }

        public float ReadSingle()
        {
            EnsureBytes(4);
            float value = BitConverter.ToSingle(_data, _position);
            _position += 4;
            return value;
        }

        public bool ReadBoolean() => ReadByte() != 0;

        public string ReadUnicodeString(int maxCharCount = 50)
        {
            int maxByteCount = maxCharCount * 2;
            int available = Math.Min(Remaining, maxByteCount);
            if (available < 2) return string.Empty;

            int nullIdx = 0;
            for (int i = 0; i < available - 1; i += 2)
            {
                if (_data[_position + i] == 0 && _data[_position + i + 1] == 0)
                    break;
                nullIdx = i + 2;
            }

            string result = Encoding.Unicode.GetString(_data, _position, nullIdx);
            _position += available;
            return result;
        }

        public string ReadFixedString(int length, Encoding? encoding = null)
        {
            EnsureBytes(length);
            encoding ??= Encoding.UTF8;
            
            int actualLength = 0;
            for (int i = 0; i < length; i++)
            {
                if (_data[_position + i] == 0) break;
                actualLength++;
            }
            
            string result = encoding.GetString(_data, _position, actualLength);
            _position += length;
            return result;
        }

        public string ReadNullTerminatedString(Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;
            int start = _position;
            while (_position < _data.Length && _data[_position] != 0)
            {
                _position++;
            }

            int length = _position - start;
            string result = encoding.GetString(_data, start, length);
            
            if (_position < _data.Length)
            {
                _position++; // Skip null terminator
            }
            
            return result;
        }

        public void Skip(int count)
        {
            EnsureBytes(count);
            _position += count;
        }

        public void Seek(int position)
        {
            if (position < 0 || position > _data.Length)
                throw new ArgumentOutOfRangeException(nameof(position));
            _position = position;
        }

        private void EnsureBytes(int count)
        {
            if (_position + count > _data.Length)
            {
                throw new EndOfStreamException($"Attempted to read {count} bytes past buffer length {_data.Length} at position {_position}.");
            }
        }
    }
}
