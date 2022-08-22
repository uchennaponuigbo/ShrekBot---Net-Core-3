using Discord.Commands;
using System.Threading.Tasks;

namespace ShrekBot.Modules.Swamp
{
    [RequireOwner]
    public class ScheduleMessage : ModuleBase<SocketCommandContext>
    {
        private readonly TimerService _service;

        public ScheduleMessage(TimerService service) // Make sure to configure your DI with your TimerService instance
        {
            _service = service;
        }

        [Command("stoptimer")]
        [Alias("stop")]
        public async Task StopCmd()
        {
            _service.Stop();
            await ReplyAsync("What!? You want me to shut up!?!?");
        }

        [Command("starttimer")]
        [Alias("start")]
        [Summary("Or restart timer")]
        public async Task RestartCmd()
        {
            _service.Restart();
            await ReplyAsync($"Donkey! I need to shout something in {_service.StartingMessageTime} minutes. " +
                $"Expect another message {_service.RepeatingMessageTime} minutes after that. And after that too!");
        }

        [Command("setchannel")]
        [Alias("set")]
        public async Task SetTextChannel(ulong id)
        {
            _service.SetTextChannel(id);
            await ReplyAsync("What!? You want me to go to talk elsewhere!?");
        }

        [Command("assigntime")]
        [Alias("newtime")]
        public async Task SetTimeInterval()
        {
            await ReplyAsync("This command has not been implemented.");
        }

        [Command("nextmessagetime")]
        [Alias("nextmessage", "nmt")]
        public async Task NextMessageTime()
        {
            await ReplyAsync(
                $"I shout the next message every {_service.RepeatingMessageTime / 60} hour(s), at 6:00pm pst.");
        }
    }
}
