using System.IO;
using Newtonsoft.Json;

namespace ShrekBot.Modules.Configuration
{
    public class Config
    {
        private const string Path = "shrekbotconfig.json";
        public static BotConfig bot;
        static Config()
        {
            string json = File.ReadAllText(Path);
            bot = JsonConvert.DeserializeObject<BotConfig>(json);
        }

        public struct BotConfig
        {
            public string Prefix;
            public string Token;
        }
    }
}
