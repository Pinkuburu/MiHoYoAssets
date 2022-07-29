namespace MiHoYoAssets.Containers
{
    public class Blk
    {
        private const int KeySize = 0x1000;
        private const int SeedBlockSize = 0x800;

        private byte[] XORPad = new byte[KeySize];
        private byte[] Buffer;
        private byte[] Key;
        private int SeedSize;
        private int DataSize;
        private bool IsEc2b;

        private byte[] ExpansionKey;
        private byte[] SBox;
        private byte[] ConstKey;
        private ulong Const;

        public Blk(ref EndianReader reader, byte[] expansionKey, byte[] constKey, ulong key, byte[] sbox = null, bool isEc2b = false)
        {
            var count = reader.ReadInt32();
            Key = reader.ReadBytes(count);
            IsEc2b = isEc2b;
            if (IsEc2b)
            {
                SeedSize = reader.ReadInt32();
            }
            else
            {
                reader.Position += count;
                SeedSize = reader.ReadInt16();
            }
            SeedSize = Math.Min(SeedSize, SeedBlockSize);
            ExpansionKey = expansionKey;
            ConstKey = constKey;
            SBox = sbox;
            Const = key;
        }

        public Blk(int dataSize, byte[] expansionKey, byte[] constKey, ulong key, byte[] sbox = null, bool isEc2b = false)
        {
            Key = new byte[0x10];
            IsEc2b = isEc2b;
            DataSize = dataSize;
            SeedSize = Math.Min(DataSize / 8 * 8, SeedBlockSize);
            ExpansionKey = expansionKey;
            ConstKey = constKey;
            SBox = sbox;
            Const = key;
        }

        public byte[] Decrypt(ref EndianReader reader)
        {
            ReadBlock(ref reader);
            var keySeed = CalculateSeed();
            var seed = DecryptKey(keySeed);
            GenerateXorpad(seed);

            var buffer = CreateBuffer();
            var writer = new EndianWriter(buffer);
            if (IsEc2b)
            {
                writer.Write(XORPad);
            }
            else
            {
                XORBlock();
                writer.Write(Buffer);
            }
            return buffer;
        }

        public void Encrypt(ulong seed, EndianWriter writer, EndianReader reader = default)
        {
            if (IsEc2b)
            {
                GenerateData();
            }
            else
            {
                ReadBlock(ref reader);
            }

            GenerateXorpad(seed);

            if (!IsEc2b)
            {
                XORBlock();
            }

            var keySeed = CalculateSeed();
            GenerateKey(seed, keySeed);

            if (IsEc2b)
            {
                writer.WriteString("Ec2b");
                writer.WriteInt32(0x10);
                writer.Write(Key);
                writer.WriteInt32(SeedSize);
            }
            else
            {
                writer.WriteString("blk", true);
                writer.WriteInt32(0x10);
                writer.Write(Key);
                writer.Write(0xFF, 0x10);
                writer.WriteInt16((short)SeedSize);
            }
            writer.Write(Buffer);
        }

        public void ReadBlock(ref EndianReader reader)
        {
            DataSize = reader.Length - reader.Position;
            Buffer = reader.ReadBytes(DataSize);
        }

        public ulong CalculateSeed()
        {
            ulong keySeed = ulong.MaxValue;
            var seedReader = new EndianReader(Buffer);
            for (int i = 0; i < SeedSize / 8; i++)
            {
                keySeed ^= seedReader.ReadUInt64();
            }

            return keySeed;
        }

        public ulong DecryptKey(ulong keySeed)
        {
            DecryptKey();
            var keyReader = new EndianReader(Key);
            var keyLow = keyReader.ReadUInt64();
            var keyHigh = keyReader.ReadUInt64();
            var seed = keyLow ^ keyHigh ^ keySeed ^ Const;

            return seed;
        }

        public ulong EncryptKey(ulong keySeed)
        {
            EncryptKey();
            var keyReader = new EndianReader(Key);
            var keyLow = keyReader.ReadUInt64();
            var keyHigh = keyReader.ReadUInt64();
            var seed = keyLow ^ keyHigh ^ keySeed ^ Const;

            return seed;
        }

        public void DecryptKey()
        {
            if (SBox != null)
            {
                for (int i = 0; i < 0x10; i++)
                {
                    Key[i] = SBox[(i % 4 * 0x100) | Key[i]];
                }
            }

            AES.Decrypt(Key, ExpansionKey);

            for (int i = 0; i < 0x10; i++)
            {
                Key[i] ^= ConstKey[i];
            }
        }

        public void EncryptKey()
        {
            for (int i = 0; i < 0x10; i++)
            {
                Key[i] ^= ConstKey[i];
            }

            AES.Encrypt(Key, ExpansionKey);

            if (SBox != null)
            {
                for (int i = 0; i < 0x10; i++)
                {
                    Key[i] = (byte)(Array.IndexOf(SBox, Key[i], i % 4 * 0x100, 0x100) % 0x100);
                }
            }
        }

        public void GenerateData()
        {
            Buffer = new byte[DataSize];
            var writer = new EndianWriter(Buffer);
            for (int i = 0; i < DataSize / 8; i++)
            {
                writer.WriteInt64(Random.Shared.NextInt64());
            }     
        }

        public void GenerateXorpad(ulong seed)
        {
            var rand = new MT19937_64(seed);
            var xorpadWriter = new EndianWriter(XORPad);
            for (int i = 0; i < KeySize / 8; i++)
            {
                xorpadWriter.WriteUInt64(rand.Int64());
            }      
        }

        public void GenerateKey(ulong seed, ulong keySeed)
        {
            var keyLow = (ulong)Random.Shared.NextInt64();
            var keyHigh = keyLow ^ seed ^ keySeed ^ Const;

            var keyWriter = new EndianWriter(Key);
            keyWriter.WriteUInt64(keyLow);
            keyWriter.WriteUInt64(keyHigh);
            EncryptKey();
        }

        public void XORBlock()
        {
            for (int i = 0; i < DataSize; i++)
            {
                Buffer[i] ^= XORPad[i % KeySize];
            }  
        }

        public byte[] CreateBuffer() => IsEc2b ? new byte[KeySize] : new byte[DataSize];
    }
}
