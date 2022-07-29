namespace MiHoYoAssets.Utils
{
    public ref struct EndianWriter
    {
        private Span<byte> Buffer { get; }
        private byte[] temp;
        public EndianType Endian { get; set; }
        public int Position { get; private set; }

        private bool IsBigEndian => Endian == EndianType.BigEndian;
        public int Length => Buffer.Length;

        public EndianWriter(Span<byte> buffer, int position = 0, EndianType endian = EndianType.LittleEndian)
        {
            Endian = endian;
            Position = position;
            Buffer = buffer;
            temp = new byte[8];
        }

        public void Write(Span<byte> buffer) => Write(buffer, 0, buffer.Length);
        public void Write(Span<byte> buffer, int offset, int count)
        {
            buffer.Slice(offset, count).CopyTo(Buffer.Slice(Position, count));
            Position += count;
        }

        public void WriteInt16(short value)
        {
            if (IsBigEndian)
            {
                BinaryPrimitives.WriteInt16BigEndian(temp, value); 
            }
            else
            {
                BinaryPrimitives.WriteInt16LittleEndian(temp, value);
            }
            Write(temp, 0, 2);
        }
        public void WriteUInt16(ushort value)
        {
            if (IsBigEndian)
            {
                BinaryPrimitives.WriteUInt16BigEndian(temp, value);
            }
            else
            {
                BinaryPrimitives.WriteUInt16LittleEndian(temp, value);
            }
            Write(temp, 0, 2);
        }

        public void WriteInt32(int value)
        {
            if (IsBigEndian)
            {
                BinaryPrimitives.WriteInt32BigEndian(temp, value);
            }
            else
            {
                BinaryPrimitives.WriteInt32LittleEndian(temp, value);
            }
            Write(temp, 0, 4);
        }

        public void WriteUInt32(uint value)
        {
            if (IsBigEndian)
            {
                BinaryPrimitives.WriteUInt32BigEndian(temp, value);
            }
            else
            {
                BinaryPrimitives.WriteUInt32LittleEndian(temp, value);
            }
            Write(temp, 0, 4);
        }

        public void WriteInt64(long value)
        {
            if (IsBigEndian)
            {
                BinaryPrimitives.WriteInt64BigEndian(temp, value);
            }
            else
            {
                BinaryPrimitives.WriteInt64LittleEndian(temp, value);
            }
            Write(temp, 0, 8);
        }

        public void WriteUInt64(ulong value)
        {
            if (IsBigEndian)
            {
                BinaryPrimitives.WriteUInt64BigEndian(temp, value);
            }
            else
            {
                BinaryPrimitives.WriteUInt64LittleEndian(temp, value);
            }
            Write(temp, 0, 8);
        }

        public void WriteString(string str, bool hasTirmenator = false)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            Write(bytes);
            if (hasTirmenator)
            {
                Buffer[Position++] = 0;
            } 
        }

        public void WriteUInt8Array(byte[] data)
        {
            WriteInt32(data.Length);
            Write(data);
        }

        public void Write(byte value, int count)
        {
            var bytes = new byte[count];
            for (int i = 0; i < count; i++)
            {
                bytes[i] = value;
            }

            Write(bytes);
        }
        public void WriteMhy0Int1(int value)
        {
            var bytes = BitConverter.GetBytes(value);
            temp[0] = (byte)(bytes[0] + 0);
            temp[1] = bytes[0];
            temp[2] = bytes[3];
            temp[3] = bytes[2];
            temp[4] = (byte)(bytes[0] + 4);
            temp[5] = (byte)(bytes[0] + 5);
            temp[6] = bytes[1];

            Write(temp, 0, 7);
        }

        public void WriteMhy0Int2(int value)
        {
            var bytes = BitConverter.GetBytes(value);
            temp[0] = bytes[2];
            temp[1] = (byte)(bytes[0] + 1);
            temp[2] = bytes[0];
            temp[3] = (byte)(bytes[0] + 3);
            temp[4] = bytes[1];
            temp[5] = bytes[3];

            Write(temp, 0, 6);
        }

        public void WriteMhy0String(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            Array.Resize(ref bytes, 0x100);
            Write(bytes);
        }

        public void WriteMhy0Bool(bool value)
        {
            var intValue = Convert.ToInt32(value);
            var bytes = BitConverter.GetBytes(intValue);
            if (!IsBigEndian)
            {
                Array.Reverse(bytes);
            }

            temp[0] = bytes[2];
            temp[1] = bytes[0];
            temp[2] = bytes[0];
            temp[3] = bytes[0];
            temp[4] = bytes[1];
            temp[5] = bytes[3];

            Write(temp, 0, 6);
        }
    }
}
