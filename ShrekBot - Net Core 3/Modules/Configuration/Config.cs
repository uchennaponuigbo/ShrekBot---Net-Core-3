using System.IO;
using Newtonsoft.Json;

namespace ShrekBot.Modules.Configuration
{
    internal class Config
    {
        internal static BotConfig bot;
        static Config()
        {
            string json = File.ReadAllText("shrekbotconfig.json");
            bot = JsonConvert.DeserializeObject<BotConfig>(json);
        }

        internal struct BotConfig
        {
            public string Prefix;
            public string Token;
        }
    }
}
