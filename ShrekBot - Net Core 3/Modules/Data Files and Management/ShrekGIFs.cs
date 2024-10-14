using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShrekBot.Modules.Data_Files_and_Management
{
    public sealed class ShrekGIFs : JSONManagement
    {
        public string[] SearchKeys { get; private set; }
        public ShrekGIFs() : base() 
        {           
            Initialize(@"..\gifs.json", "gifs.json");
            if (!DoesFileExist)
                return;
            SearchKeys = GetKeys().ToArray();
        }

        public void AddGif(string name, string gifLink)
        {
            pairs.Add(name, gifLink);
            SaveDataToFile();
            SearchKeys = GetKeys().ToArray();
        }
    }
}
