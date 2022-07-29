namespace MiHoYoAssets.Formats
{
    public class CB1 : Format
    {
        public CB1()
        {
            Name = "CB1";
            DisplayName = "Genshin Impact - CB1 Beta";
            Pattern = ("*.asb", "*.unity3d");
            Extension = (".unity3d", ".asb");
        }

        protected override void Decrypt(string input, string output)
        {
            var reader = new EndianReader(input);
            var buffer = Mark.Decrypt(ref reader);
            WriteOutput(output, buffer);
        }

        protected override void Encrypt(string input, string output)
        {
            var buffer = File.ReadAllBytes(input);
            buffer = Mark.Encrypt(buffer);
            WriteOutput(output, buffer);
        }
    }
}
