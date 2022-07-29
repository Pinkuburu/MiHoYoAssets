namespace MiHoYoAssets.Containers
{
    public class Bundle
    {
        private Header m_Header;
        private StorageBlock[] BlocksInfo;
        private Node[] DirectoryInfo;
        private StreamFile[] FileList;
        private MemoryStream BlocksStream;
        private bool HasPadding;

        private readonly byte[] ExtensionKey;
        private readonly byte[] Key;
        private readonly byte[] ConstKey;
        private readonly byte[] SBox;

        private int MaxCompressedSize => BlocksInfo.Max(x => x.CompressedSize);
        private int MaxUncompressedSize => BlocksInfo.Max(x => x.UncompressedSize);
        public Bundle(Header header, bool hasPadding, byte[] expansionKey = null, byte[] key = null, byte[] constKey = null, byte[] sbox = null)
        {
            m_Header = header;
            HasPadding = hasPadding;
            ExtensionKey = expansionKey;
            Key = key;
            ConstKey = constKey;
            SBox = sbox;
        }

        public void Process(ref EndianReader reader, string output)
        {
            ReadMetadata(ref reader);
            BlocksStream = CreateBlocksStream();
            ReadBlocks(ref reader);
            ReadNodes();
            WriteFiles(output);
        }

        public void ReadMetadata(ref EndianReader reader)
        {
            var buffer = ReadAndDecompressBlocksInfo(ref reader);
            ReadBlocksInfoAndDirectory(buffer);
        }

        private byte[] ReadAndDecompressBlocksInfo(ref EndianReader reader)
        {
            var compressedSize = m_Header.CompressedBlocksInfoSize;
            var compressedBytes = reader.ReadBytes(compressedSize);
            var uncompressedSize = m_Header.UncompressedBlocksInfoSize;
            var uncompressedBytes = new byte[uncompressedSize];
            int offset = 0;
            var compressionType = (CompressionType)(m_Header.Flags % 0x40);
            switch (compressionType)
            {
                case CompressionType.Lz4:
                case CompressionType.Lz4HC:
                    var numWrite = LZ4Codec.Decode(compressedBytes, offset, compressedSize, uncompressedBytes, 0, uncompressedSize);
                    if (numWrite != uncompressedSize)
                    {
                        throw new IOException($"Lz4 decompression error, write {numWrite} bytes but expected {uncompressedSize} bytes");
                    }
                    break;
                case CompressionType.Lz4Mr0k:
                    if (compressedSize > 0xFF)
                    {
                        offset = Mr0k.Decrypt(compressedBytes, ref compressedSize, ExtensionKey, Key, ConstKey, SBox);
                    }
                    if (compressedSize < 0x10)
                    {
                        throw new Exception($"Lz4 decompression error, wrong compressed length: {compressedSize}");
                    }
                    goto case CompressionType.Lz4;
            }
            return uncompressedBytes;
        }

        private void ReadBlocksInfoAndDirectory(byte[] blocksInfo)
        {
            var reader = new EndianReader(blocksInfo, 0, EndianType.BigEndian);
            if (HasPadding)
            {
                reader.Position += 0x10;
            }
            var blocksInfoCount = reader.ReadInt32();
            BlocksInfo = new StorageBlock[blocksInfoCount];
            for (int i = 0; i < blocksInfoCount; i++)
            {
                BlocksInfo[i] = new StorageBlock
                {
                    UncompressedSize = reader.ReadInt32(),
                    CompressedSize = reader.ReadInt32(),
                    Flags = reader.ReadInt16()
                };
            }
            var directoryInfoCount = reader.ReadInt32();
            DirectoryInfo = new Node[directoryInfoCount];
            for (int i = 0; i < directoryInfoCount; i++)
            {
                DirectoryInfo[i] = new Node
                {
                    Offset = reader.ReadInt64(),
                    Size = reader.ReadInt64(),
                    Flags = reader.ReadInt32(),
                    Path = reader.ReadStringToNull(),
                };
            }
        }

        private MemoryStream CreateBlocksStream()
        {
            var uncompressedSizeSum = BlocksInfo.Sum(x => x.UncompressedSize);
            return new MemoryStream(uncompressedSizeSum);
        }

        private void ReadBlocks(ref EndianReader reader)
        {
            var compressedBytes = new byte[MaxCompressedSize];
            var uncompressedBytes = new byte[MaxUncompressedSize];
            foreach (var blockInfo in BlocksInfo)
            {
                var compressedSize = blockInfo.CompressedSize;
                var uncompressedSize = blockInfo.UncompressedSize;
                reader.Read(compressedBytes, 0, compressedSize);
                var offset = 0;
                var compressionType = (CompressionType)(blockInfo.Flags % 0x40);
                switch (compressionType)
                {
                    case CompressionType.Lz4:
                    case CompressionType.Lz4HC:
                        var numWrite = LZ4Codec.Decode(compressedBytes, offset, compressedSize, uncompressedBytes, 0, uncompressedSize);
                        if (numWrite != uncompressedSize)
                        {
                            throw new IOException($"Lz4 decompression error, write {numWrite} bytes but expected {uncompressedSize} bytes");
                        }
                        break;
                    case CompressionType.Lz4Mr0k:
                        if (compressedSize > 0xFF)
                        {
                            offset = Mr0k.Decrypt(compressedBytes, ref compressedSize, ExtensionKey, Key, ConstKey, SBox);
                        }
                        if (compressedSize < 0x10)
                        {
                            throw new Exception($"Lz4 decompression error, wrong compressed length: {compressedSize}");
                        }
                        goto case CompressionType.Lz4;
                }
                BlocksStream.Write(uncompressedBytes, 0, uncompressedSize);
            }
        }

        private void ReadNodes()
        {
            FileList = new StreamFile[DirectoryInfo.Length];
            for (int i = 0; i < DirectoryInfo.Length; i++)
            {
                var node = DirectoryInfo[i];
                var file = new StreamFile();
                file.Path = node.Path;
                file.FileName = Path.GetFileName(node.Path);
                file.Stream = new MemoryStream();
                FileList[i] = file;

                BlocksStream.Position = node.Offset;
                BlocksStream.CopyTo(file.Stream, node.Size);
                file.Stream.Position = 0;
            }
            BlocksStream.Dispose();
        }

        private void WriteFiles(string outputPath)
        {
            foreach (var file in FileList)
            {
                var output = Path.Combine(outputPath, file.FileName);
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }
                using var outStream = File.Create(output);
                file.Stream.CopyTo(outStream);
                file.Stream.Dispose();
            }
        }

        public class Header
        {
            public long Size;
            public int CompressedBlocksInfoSize;
            public int UncompressedBlocksInfoSize;
            public int Flags;
        }

        public enum CompressionType
        {
            None,
            Lzma,
            Lz4,
            Lz4HC,
            LzHAM,
            Lz4Mr0k
        }

        public class StorageBlock
        {
            public int CompressedSize;
            public int UncompressedSize;
            public short Flags;
        }

        public class Node
        {
            public long Offset;
            public long Size;
            public int Flags;
            public string Path;
        }
        public class StreamFile
        {
            public string Path;
            public string FileName;
            public Stream Stream;
        }
    }
}
