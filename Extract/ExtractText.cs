using System.Text;
using DaveCsharp.Common;

namespace DaveCsharp.Extract
{
    static class ExtractText
    {
        const uint TITLE_ADDRESS = 0x2643f;
        const uint TITLE_MAX_LENGTH = 0xe;
        const uint SUBTITLE_ADDRESS = 0x26451;
        const uint SUBTITLE_MAX_LENGTH = 0x17;
        const uint HELP_PROMPT_ADDRESS = 0x2646b;
        const uint HELP_PROMPT_MAX_LENGTH = 25;

        private static void WriteTextToFile(BinaryFileReader reader, uint addr, uint maxLength, string filename)
        {
            const string path = "assets/text";
            Directory.CreateDirectory(path);
            using var fOut = File.OpenWrite(Path.Combine(path, filename));
            fOut.Write(Encoding.ASCII.GetBytes(reader.Seek(addr).ReadString(maxLength)));
        }

        public static void Extract() => Extract(Path.Combine(Environment.CurrentDirectory, "../original-game/DAVE.EXENEW"));
        public static void Extract(string exeFilePath)
        {
            using BinaryFileReader reader = new(exeFilePath);
            reader.Open();
            WriteTextToFile(reader, TITLE_ADDRESS, TITLE_MAX_LENGTH, "title.txt");
            WriteTextToFile(reader, SUBTITLE_ADDRESS, SUBTITLE_MAX_LENGTH, "subtitle.txt");
            WriteTextToFile(reader, HELP_PROMPT_ADDRESS, HELP_PROMPT_MAX_LENGTH, "helpprompt.txt");
        }
    }
}