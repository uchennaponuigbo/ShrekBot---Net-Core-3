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
            var anger = new Emoji("\uD83D\uDCA2");
            //await Task.Delay(100);
            using (Context.Channel.EnterTypingState())
            {
                await ReplyAsync($"{anger}{JSONUtilities.GetAlert("swamp")}{anger}");
            }            
        }
        [Command("exit")]
        [RequireOwner]
        [RequireContext(ContextType.Guild, ErrorMessage = "Shut down command cannot be used in Direct Messages.")]
        public async Task ExitAsync()
        {
            string s = $"Initiating shut down command from the guild, {Context.Guild.Name}, in the text channel {Context.Channel.Name}. {DateTime.Now.ToString()}";
            IDMChannel dmChannel = await Context.User.GetOrCreateDMChannelAsync();            
            await dmChannel.SendMessageAsync(s);            
            Environment.Exit(0);
        }
    }
}
