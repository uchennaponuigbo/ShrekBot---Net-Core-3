using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

namespace ShrekBot.Modules.Data_Files_and_Management
{
    class JSONUtilities
    {
        private static Dictionary<string, string> alerts;

        static JSONUtilities()
        {
            string json = File.ReadAllText("quotes.json");
            var data = JsonConvert.DeserializeObject<dynamic>(json);
            alerts = data.ToObject<Dictionary<string, string>>();
        }

        public static string GetAlert(string key)
        {
            if (alerts.ContainsKey(key))
                return alerts[key];
            return "";
        }
    }
}
