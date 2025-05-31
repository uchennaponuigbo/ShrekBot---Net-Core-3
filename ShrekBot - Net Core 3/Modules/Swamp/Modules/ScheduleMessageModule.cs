using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ShrekBot.Modules.Swamp.Services;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace ShrekBot.Modules.Swamp.Modules
{
    [RequireOwner]
    //[Custom_ModuleAlias("MessageTimer")]
    public class ScheduleMessageModule : ModuleBase<SocketCommandContext>
    {
        private readonly TimerService _service;

        public ScheduleMessageModule(TimerService service) // Make sure to configure your DI with your TimerService instance
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
            double nextMinutes = _service.MinutesUntilNextMessage();
            double repeatMinutes = _service.RepeatingMessageInMinutes;
            string hourConversion = "";
            string repeatingHourConversion = "";

            FormatSingularOrPluralMessage(nextMinutes, repeatMinutes, 
                ref hourConversion, ref repeatingHourConversion);

            string message = "Donkey! I will shout something at" +
                $" {_service.RepeatingIntervalTimeUTC} UTC ({_service.MessageTimeInPST()} PST). " +
                $"Aka in {nextMinutes} minute{(nextMinutes == 60 ? "" : "s")}{hourConversion}. " +
                $"Expect another message {repeatMinutes} minute{(repeatMinutes == 60 ? "" : "s")}{repeatingHourConversion}" +
                $" after that. And after that too!";

            await ReplyAsync(message);
        }

        [Command("setchannel")]
        [Alias("set")]
        public async Task SetTextChannel(ulong id)
        {
            _service.SetTextChannel(id);
            SocketChannel textChannel = Context.Client.GetChannel(_service.GuildChnlID);
            if (textChannel != null)
                await ReplyAsync($"What!? My swamp's been moved to {textChannel}!?");
            else
            {
                await ReplyAsync("DONKEY!! WHERE IS MY SWAMP!?");
                _service.SetTextChannel();
            }
                
        }

        [Command("assignnewtime")]
        [Alias("newtime", "ant")]
        public async Task SetTimeInterval(double newRepeatingInterval, string newIntervalTimeInUTC)
        {
            if(newRepeatingInterval < 0)
            {
                await ReplyAsync("Donkey! How does a negative repeat time makes sense!!");
                return;
            }
                
            if (!IsValidTimeFormat(newIntervalTimeInUTC))
            {
                await ReplyAsync("Donkey! What clock on what planet have you been reading!? " +
                    "You better not of forgotten the AM/PM too...");
                return;
            }
            //TODO
            //if this format is correct, but it's in the past...


            _service.SetMessageTimes(newRepeatingInterval, newIntervalTimeInUTC);
            _service.Restart();
            await ReplyAsync("Fine, Donkey! I will shout my messages at a different time!");
        }

        [Command("hosttimezone")]
        [Summary("The timezone the bot is running in, with respect to the time zone the user is running in.")]
        [Alias("htz")]
        public async Task TimeZoneBotIsRunningIn()
        {
            EmbedBuilder build = new EmbedBuilder();
            TimeZoneInfo localZone = TimeZoneInfo.Local;
            
            build.Color = Color.Blue;
            build.WithTitle("UTC - "+ DateTime.UtcNow.ToString());
            build.WithDescription(localZone.DisplayName);           
            build.WithFooter(localZone.StandardName);
            await ReplyAsync("", false, build.Build());
        }

        [Command("nextmessagetime")]
        [Summary("Tells the user when the next message will be relayed in minutes")]
        [Alias("nextmessage", "nmt")]
        public async Task NextMessageTime()
        {
            if(_service.isPaused)
            {
                await ReplyAsync("Yo Donkey! You told me to shut up!");
                return;
            }

            double nextMinutes = _service.MinutesUntilNextMessage();
            double repeatMinutes = _service.RepeatingMessageInMinutes;
            string hourConversion = "";
            string repeatingHourConversion = "";

            FormatSingularOrPluralMessage(nextMinutes, repeatMinutes, 
                ref hourConversion, ref repeatingHourConversion);

            string message = $"I shout the next message every " +
                $"{repeatMinutes} minute{(repeatMinutes == 1 ? "" : "s")}{repeatingHourConversion}. " +
                $"My next message will be in {nextMinutes} minute{(nextMinutes == 60 ? "" : "s")}{hourConversion}, " +
                $"at {_service.RepeatingIntervalTimeUTC} UTC. ({_service.MessageTimeInPST()} PST)" +
                $" at {_service.MessageSentDateTime}"; // ({_service.MessageTimeinPST()} PST

            await ReplyAsync(message);
        }

        //Convoluted code just to make sure the singular or plural is correct
        private void FormatSingularOrPluralMessage(double nextMinutes, double repeatMinutes, 
            ref string hourConversion, ref string repeatingHourConversion)
        {
            if (nextMinutes > 59)
                hourConversion = $" ({Math.Round(nextMinutes / 60, 2)} hour{(nextMinutes == 60 ? "" : "s")})";
            if (repeatMinutes > 59)
                repeatingHourConversion = $" ({Math.Round(repeatMinutes / 60, 2)} " +
                    $"hour{(repeatMinutes == 60 ? "" : "s")})";
        }

        private bool IsValidTimeFormat(string input)
        {
            DateTime dummyOutput;

            bool correctTime = DateTime.TryParseExact(input, "h:mm tt",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out dummyOutput);
            bool correctTime2 = DateTime.TryParseExact(input, "h:mmtt",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out dummyOutput);

            if (correctTime || correctTime2)
                return true;
            else
                return false;
        }
    }
}
