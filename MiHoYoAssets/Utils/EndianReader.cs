namespace MiHoYoAssets.Utils
{
    public ref struct EndianReader
    {
        private ReadOnlySpan<byte> Buffer { get; }
        public EndianType Endian { get; set; }
        public int Position { get; set; }

        private bool IsBigEndian => Endian == EndianType.BigEndian;
        public int Length => Buffer.Length;
        public int Remaining => Buffer.Length - Position;

        public EndianReader(string path, int position = 0, EndianType endian = EndianType.LittleEndian) : this(File.ReadAllBytes(path), position, endian) { }
        public EndianReader(ReadOnlySpan<byte> buffer, int position = 0, EndianType endian = EndianType.LittleEndian)
        {
            Buffer = buffer;
            Position = position;
            Endian = endian;
        }
        public ReadOnlySpan<byte> Read(int count, bool peek = false)
        {
            var slice = Buffer.Slice(Position, count);
            Position += peek ? 0 : count;

            return slice;
        }
        public int Read(Span<byte> buffer, int offset, int count, bool peek = false)
        {
            Read(count, peek).CopyTo(buffer.Slice(offset, count));
            return count;
        }
        public void Align(int alignment)
        {
            var mod = Position % alignment;
            if (mod != 0)
            {
                Position += alignment - mod;
            }
        }
        public byte ReadByte(bool peek = false) => Convert.ToByte(Read(1, peek)[0]);
        public bool ReadBoolean(bool peek = false) => Convert.ToBoolean(Read(1, peek)[0]);
        public short ReadInt16(bool peek = false) => IsBigEndian ? BinaryPrimitives.ReadInt16BigEndian(Read(sizeof(short), peek)) : BinaryPrimitives.ReadInt16LittleEndian(Read(sizeof(short), peek));
        public ushort ReadUInt16(bool peek = false) => IsBigEndian ? BinaryPrimitives.ReadUInt16BigEndian(Read(sizeof(ushort), peek)) : BinaryPrimitives.ReadUInt16LittleEndian(Read(sizeof(ushort), peek));
        public int ReadInt32(bool peek = false) => IsBigEndian ? BinaryPrimitives.ReadInt32BigEndian(Read(sizeof(int), peek)) : BinaryPrimitives.ReadInt32LittleEndian(Read(sizeof(int), peek));
        public uint ReadUInt32(bool peek = false) => IsBigEndian ? BinaryPrimitives.ReadUInt32BigEndian(Read(sizeof(uint), peek)) : BinaryPrimitives.ReadUInt32LittleEndian(Read(sizeof(uint), peek));
        public long ReadInt64(bool peek = false) => IsBigEndian ? BinaryPrimitives.ReadInt64BigEndian(Read(sizeof(long), peek)) : BinaryPrimitives.ReadInt64LittleEndian(Read(sizeof(long), peek));
        public ulong ReadUInt64(bool peek = false) => IsBigEndian ? BinaryPrimitives.ReadUInt64BigEndian(Read(sizeof(ulong), peek)) : BinaryPrimitives.ReadUInt64LittleEndian(Read(sizeof(ulong), peek));
        public byte[] ReadBytes(int count, bool peek = false)
        {
            count = Math.Min(count, Remaining);
            var bytes = Read(count, peek).ToArray();
            return bytes;
        }
        public byte[] ReadAll(bool peek = false) => ReadBytes(Length, peek);
        public byte[] ReadRemaining(bool peek = false) => ReadBytes(Remaining, peek);

        public string ReadStringToNull(int maxLength = 0x7FFF, bool peek = false)
        {
            int count = 0;
            int pos = Position;
            while (Position != Length && count < maxLength)
            {
                if (ReadByte() == 0)
                {
                    break;
                }
                count++;
            }
            Position = pos;
            string str;
            if (count == maxLength)
            {
                str = Encoding.UTF8.GetString(Read(count, peek));
            }
            else
            {
                str = Encoding.UTF8.GetString(Read(count + 1, peek)[..^1]);
            }
            return str;
        }
        public int ReadMhy0Int1(bool peek = false)
        {
            var buffer = ReadBytes(7, peek);
            return buffer[1] | (buffer[6] << 8) | (buffer[3] << 0x10) | (buffer[2] << 0x18);
        }

        public int ReadMhy0Int2(bool peek = false)
        {
            var buffer = ReadBytes(6, peek);
            return buffer[2] | (buffer[4] << 8) | (buffer[0] << 0x10) | (buffer[5] << 0x18);
        }

        public string ReadMhy0String(bool peek = false)
        {
            var bytes = ReadBytes(0x100, peek);
            return Encoding.UTF8.GetString(bytes.TakeWhile(b => !b.Equals(0)).ToArray());
        }

        public bool ReadMhy0Bool(bool peek = false)
        {
            int value = ReadMhy0Int2(peek);
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            return BitConverter.ToBoolean(bytes);
        }
        
        public byte[] ReadUInt8Array(bool peek = false)
        {
            var pos = Position;
            var count = ReadInt32();
            var bytes = ReadBytes(count, peek);
            if (peek)
            {
                Position = pos;
            }
            return bytes;
        }
    }
}
