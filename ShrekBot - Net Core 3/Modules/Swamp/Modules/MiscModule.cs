using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using ShrekBot.Modules.Data_Files_and_Management;
using Interactivity;

namespace ShrekBot.Modules.Swamp.Modules
{
    public class MiscModule : ModuleBase<SocketCommandContext>
    {
        //[Command("test")]
        //public async Task Test(string keyword)
        //{
        //    ShrekGIFs gifs = new ShrekGIFs();
        //    await ReplyAsync(gifs.GetValue("shrek"));
        //}

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
        [Summary("Shrek's companion to yell at.")]
        public async Task DonkeyYell()
        {
            using (Context.Channel.EnterTypingState())
            {
                ShrekMessage swamp = new ShrekMessage(true);
                await ReplyAsync($"{swamp.GetValue("2")}");
            }
        }

        [Command("random")]
        [Alias("r", "rand", "randmessage", "randommessage")]
        [Summary("I could say WHATEVER I want without you telling me otherwise!")]
        public async Task RandomessAsync()
        {
            using (Context.Channel.EnterTypingState())
            {
                ShrekMessage swamp = new ShrekMessage();
                Random rand = new Random();
                int index = rand.Next(1, swamp.PairCount + 1);
                await ReplyAsync(swamp.GetValue(index.ToString()));
            }
        }

        [Group("quote")]
        [Summary("Modify randomized messages used for output")]
        [RequireOwner]
        public class ManageFile : ModuleBase<SocketCommandContext>
        {
            private readonly InteractivityService _interactivity;
            public ManageFile(InteractivityService service) => _interactivity = service;

            [Command("add")]
            [Alias("addrandom")]
            [Remarks("There are no commands to remove messages. You can only modify content programatically.")]
            public async Task TestCall(string newQuote = "")
            {
                if (newQuote == "")
                {
                    await ReplyAsync("You can't give me nothing to add, Donkey!");
                    return;
                }

                ShrekMessage swamp = new ShrekMessage();

                swamp.AddQuote(newQuote);
                EmbedBuilder build = new EmbedBuilder();

                build.Description = $"{swamp.PairCount}: {newQuote}";
                build.Color = Color.Green;
                await ReplyAsync($"New quote added and saved!", false, build.Build());
            }

            [Command("modify", RunMode = RunMode.Async)]
            public async Task ModifyAsync(int index)
            {
                var channel = Context.Client.GetChannel(Context.Channel.Id) as IMessageChannel;
                if (index == 1 || index == 2)
                {
                    await channel.SendMessageAsync("Cannot modify quotes used in Commands");
                    return;
                }

                ShrekMessage swamp = new ShrekMessage();
                if (!swamp.DoesKeyExist(index.ToString()))
                {
                    await channel.SendMessageAsync("This message does not exist in file.");
                    return;
                }

                await channel.SendMessageAsync($"Enter the quote to replace the value of Index {index}. Reply in {_interactivity.DefaultTimeout.Seconds} seconds");
                var nextResult = await _interactivity.NextMessageAsync(x => x.Author.Id == Context.User.Id);
                if (nextResult.IsSuccess)
                {
                    swamp.EditValue(index.ToString(), nextResult.Value.Content);
                    EmbedBuilder build = new EmbedBuilder();
                    build.Description = nextResult.Value.Content;
                    build.Color = Color.Green;
                    await channel.SendMessageAsync($"Index {index} has new value.", false, build.Build());
                }
                else
                {
                    await channel.SendMessageAsync("You ran out of time Donkey!");
                }

            }
        }

        [Command("exit")]
        [RequireOwner]
        //[RequireContext(ContextType.Guild, ErrorMessage = "Shut down command cannot be used in Direct Message Channel.")]
        public async Task ExitAsync()
        {
            string s = "";
            if (Context.Guild != null)
                s = $"Initiating shut down command from the guild, {Context.Guild.Name}, in the text channel " +
                    $"{Context.Channel.Name}. {DateTime.Now}";
            else
                s = $"Initiating shut down command from this DM. {DateTime.Now}.";
            IDMChannel dmChannel = await Context.User.CreateDMChannelAsync();
            await dmChannel.SendMessageAsync(s);
            Environment.Exit(0);
        }
    }
}
