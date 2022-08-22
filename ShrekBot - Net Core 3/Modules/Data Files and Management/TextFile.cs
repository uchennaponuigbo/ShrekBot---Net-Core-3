using System.IO;

namespace ShrekBot.Modules.Data_Files_and_Management
{
    internal static class TextFile
    {
        public static string CompactCommands() => File.ReadAllText("help.txt");
        public static string OwnerCommands() => File.ReadAllText("helpowner.txt");

        public static string UserCommands() => File.ReadAllText("helpuser.txt");
    }
}
