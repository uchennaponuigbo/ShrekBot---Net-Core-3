using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using ShrekBot.Modules.Data_Files_and_Management;

namespace ShrekBot.Modules
{
    public class Misc : ModuleBase<SocketCommandContext>
    {
        [Command("swamp")]
        [Summary("Don't anger the orge.")]
        public async Task SwampYell()
        {
            //await Task.Delay(100);
            using (Context.Channel.EnterTypingState())
            {
                ShrekMessage swamp = new ShrekMessage(true);
                await ReplyAsync($"{swamp.GetValue("1")}");
            }            
        }

        [Command("donkey")]
        public async Task DonkeyYell()
        {   
            using(Context.Channel.EnterTypingState())
            {
                ShrekMessage swamp = new ShrekMessage(true);
                await ReplyAsync($"{swamp.GetValue("2")}");
            }
        }

        [Command("exit")]
        [RequireOwner]
        [RequireContext(ContextType.Guild, ErrorMessage = "Shut down command cannot be used in Direct Messages.")]
        public async Task ExitAsync()
        {
            string s = $"Initiating shut down command from the guild, {Context.Guild.Name}, in the text channel " +
                $"{Context.Channel.Name}. {DateTime.Now}";
            IDMChannel dmChannel = await Context.User.CreateDMChannelAsync();            
            await dmChannel.SendMessageAsync(s);            
            Environment.Exit(0);
        }
    }
}
