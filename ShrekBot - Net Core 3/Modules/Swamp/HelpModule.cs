using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using ShrekBot.Modules.Data_Files_and_Management;

namespace ShrekBot.Modules.Swamp
{
    public class HelpModule : ModuleBase<SocketCommandContext>
    {


        [Command("help")]
        [Summary("Lists this bot's general user commands.")]
        public async Task Help()
        {
            EmbedBuilder build = new EmbedBuilder();
            build.Color = Color.Red;
            build.Description = TextFile.UserCommands();
            build.WithFooter("The bot will periodically send a random message every day at 6:00pm pst");
            await ReplyAsync("", false, build.Build());

        }

        [Group("helpowner")]
        [RequireOwner]
        public class OwnerHelpModule : ModuleBase<SocketCommandContext>
        {
            [Command("detail")]
            [Summary("Lists the owner only commands in detail.")]
            public async Task HelpOwner1()
            {
                EmbedBuilder build = new EmbedBuilder();
                build.Color = Color.Red;
                build.Description = TextFile.OwnerCommands();
                build.WithFooter("The bot will periodically send a random message every day at 6:00pm pst");

                IDMChannel dmChannel = await Context.User.CreateDMChannelAsync();
                await dmChannel.SendMessageAsync("", false, build.Build());
            }

            [Command("compact")]
            [Summary("Lists all the commands without detail.")]
            public async Task HelpOwner2()
            {
                EmbedBuilder build = new EmbedBuilder();
                build.Color = Color.Red;
                build.Description = TextFile.CompactCommands();
                IDMChannel dmChannel = await Context.User.CreateDMChannelAsync();
                await dmChannel.SendMessageAsync("", false, build.Build());
                
            }
        }
    }
}
