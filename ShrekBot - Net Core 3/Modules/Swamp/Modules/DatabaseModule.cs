using Discord.Commands;
using ShrekBot.Modules.Data_Files_and_Management.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShrekBot.Modules.Swamp.Modules
{
    [RequireOwner]
    public class DatabaseModule : ModuleBase<SocketCommandContext>
    {

        [Command("dbrecords", RunMode = RunMode.Async)]
        public async Task DisplayDatabaseInfo()
        {
            //ImageComparison compare = new ImageComparison();
            //compare.AddFalsePositive(12729684797621830272, "kirbeeO.png");
            //await ReplyAsync("Gottem");
            SwampDB swamp = new SwampDB();
            string records = swamp.SelectCountOfRecordsFromAllTables();
            await ReplyAsync(records);
        }

        [Command("dbaddabomn", RunMode = RunMode.Async)]
        public async Task AddAbominationVariant(ulong abominationHashVariant, string name)
        {
            ImageComparison comparison = new ImageComparison();
            int recordsRemoved = comparison.AddAbominationVariant(abominationHashVariant, name);
            string message = "";
            if (recordsRemoved > 0)
                message = $"Donkey, {recordsRemoved} sneaky rat(s) snuck past me into the Swamp! I killed it though so I'll keep watch!";
            else
                message = "I'll keep watch of this new Onion rat!";
            await ReplyAsync(message);
        }

        [Command("dbremoveabomn", RunMode = RunMode.Async)]
        public async Task RemoveAbominationVariant(ulong abominationHashVariant)
        {
            ImageComparison comparison = new ImageComparison();
            //int recordsRemoved = comparison.AddAbominationVariant(abominationHashVariant, name);
            bool removed = comparison.RemoveAbominationVariant(abominationHashVariant);
            string message = "";
            if (removed)
                message = $"Donkey, I firmly believe you have too much onions in your eyes, but fine this is not a rat!";
            else
                message = "Oopsy, I can't unsee a rat heheh!";
            await ReplyAsync(message);
        }
    }
}
