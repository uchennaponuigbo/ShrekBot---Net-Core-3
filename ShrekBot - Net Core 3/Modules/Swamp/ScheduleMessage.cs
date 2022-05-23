using System;
using Discord.Commands;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ShrekBot.Modules.Swamp
{
    public class ScheduleMessage : ModuleBase<SocketCommandContext>
    {
        private readonly TimerService _service;

        public ScheduleMessage(TimerService service) // Make sure to configure your DI with your TimerService instance
        {
            _service = service;
        }

        // Example commands
        [Command("stoptimer")]
        public async Task StopCmd()
        {
            _service.Stop();
            await ReplyAsync("Timer stopped.");
        }

        [Command("starttimer")]
        public async Task RestartCmd()
        {
            _service.Restart();
            await ReplyAsync("Timer (re)started.");
        }
    }
}
