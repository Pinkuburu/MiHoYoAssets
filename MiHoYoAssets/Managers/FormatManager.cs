namespace MiHoYoAssets.Managers
{
    public static class FormatManager
    {
        private static Dictionary<string, Format> Formats = new();
        static FormatManager()
        {
            foreach (Type type in
                Assembly.GetAssembly(typeof(Format)).GetTypes()
                .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(Format))))
            {
                var format = (Format)Activator.CreateInstance(type);
                Formats.Add(format.Name, format);
            }
        }
        public static Format GetFormat(string name)
        {
            if (!Formats.TryGetValue(name, out var format))
            {
                throw new ArgumentException("Invalid Format !!");
            }

            return format;
        }
        public static string GetFormats() => string.Join("\n", Formats.Values);
    }
    public abstract class Format
    {
        public string Name;
        public string DisplayName;
        protected (string, string) Pattern;
        protected (string, string) Extension;
        protected abstract void Decrypt(string input, string output);
        protected abstract void Encrypt(string input, string output);
        protected virtual (string, string)[] CollectPaths(string input, string output, bool isEncrypt)
        {
            if (File.Exists(input))
            {
                return new (string, string)[] { (input, output) };
            }
            var files = Directory.GetFiles(input, isEncrypt ? Pattern.Item2 : Pattern.Item1, SearchOption.AllDirectories);
            var paths = new List<(string, string)>();
            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(input, file);
                relativePath = Path.ChangeExtension(relativePath, isEncrypt ? Extension.Item2 : Extension.Item1);
                var outPath = Path.Combine(output, relativePath);
                paths.Add((file, outPath));
            }
            return paths.ToArray();

        }
        public void Process(string input, string output, bool isEncrypt)
        {
            if (!IsSupported(isEncrypt))
            {
                throw new NotImplementedException();
            }
            var paths = CollectPaths(input, output, isEncrypt);
            var count = paths.Length;
            Console.WriteLine($"Found {count} file(s).");
            for (int i = 0; i < count; i++)
            {
                var pair = paths[i];

                if (isEncrypt)
                    Encrypt(pair.Item1, pair.Item2);
                else
                    Decrypt(pair.Item1, pair.Item2);

                Console.WriteLine($"Processed [{i + 1}/{count}] file(s).");
            }
        }
        protected void WriteOutput(string output, byte[] buffer)
        {
            var directory = Path.GetDirectoryName(output);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllBytes(output, buffer);
        }
        public override string ToString() => $"{Name} ({DisplayName})";
        private bool IsSupported(bool isEncrypt) => !string.IsNullOrEmpty(isEncrypt ? Pattern.Item2 : Pattern.Item1) || !string.IsNullOrEmpty(isEncrypt ? Extension.Item2 : Extension.Item1);
    }
}
