using System;
using System.Collections.Generic;
using System.Text;

namespace ShrekBot.Modules.Data_Files_and_Management
{
    public class ShrekSongs : JSONManagement
    {
        public ShrekSongs() : base() => Initialize(@"..\music.json", "music.json");
    }
}
