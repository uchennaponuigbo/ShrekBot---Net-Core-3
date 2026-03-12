using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp.ColorSpaces.Companding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShrekBot.Modules.Data_Files_and_Management
{
    public sealed class ShrekGIFs : JSONManagement
    {
        public static string[] SearchKeys { get; private set; }
        public ShrekGIFs() : base() 
        {           
            Initialize(@"..\gifs.json", "gifs.json");
            //if (!DoesFileExist)
            //    return;
            SearchKeys = GetKeys().ToArray();
        }

        static ShrekGIFs()
        {
            using (StreamReader sr = File.OpenText(@"..\gifs.json"))
            using (JsonTextReader reader = new JsonTextReader(sr))
            {
                JObject json = (JObject)JToken.ReadFrom(reader);
                SearchKeys = json.Properties().Select(p => p.Name).ToArray();
            }
        }
    }
}
