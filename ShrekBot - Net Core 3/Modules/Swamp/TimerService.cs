using System;
using Discord;
using System.Threading;
using Discord.WebSocket;
using ShrekBot.Modules.Data_Files_and_Management;

namespace ShrekBot.Modules
{  
    public class TimerService
    {
        private readonly Timer _timer;
        
        public ulong GuildChnlID { get; set; }

        public double StartingMessageTime { get; set; } //calculate current time from midnight
        public double RepeatingMessageTime { get; set; } //should be midnight
        //https://gist.github.com/Joe4evr/967949a477ed0c6c841407f0f25fa730
        public TimerService(DiscordSocketClient client)
        {
            GuildChnlID = 653106031731408896; //default channel id for shrekbot testing

            StartingMessageTime = 1; // 4) Time that message should fire after the timer is created
            RepeatingMessageTime = 1;
            //Start = DateTime.UtcNow.TimeOfDay.TotalMinutes; End = 24 * 60;
            //EmbedColor embed = new EmbedColor(Color.Green);
            Random rand = new Random();
            _timer = new Timer(async _ =>
            {
                // 3) Any code you want to periodically run goes here, for example:
                IMessageChannel chnl = client.GetChannel(GuildChnlID) as IMessageChannel;
                if (chnl != null)
                {
                    ShrekMessage randShrekMessage = new ShrekMessage();
                    int index = rand.Next(1, randShrekMessage.PairCount + 1); //quote 1 to quote n
                    await chnl.SendMessageAsync($"{index}");
                }
                    
            },
            null,
            TimeSpan.FromMinutes(StartingMessageTime), 
            TimeSpan.FromMinutes(RepeatingMessageTime));
            
        }

        public void Stop() // 6) Example to make the timer stop running
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void Restart() // 7) Example to restart the timer
        {
            _timer.Change(TimeSpan.FromMinutes(StartingMessageTime), TimeSpan.FromMinutes(RepeatingMessageTime));
        }
       
        public void ChangeMessageTimes(double newStart, double newRepeat)
        {
            StartingMessageTime = newStart;
            RepeatingMessageTime = newRepeat;
        }

        public void SetTextChannel(ulong newId) => GuildChnlID = newId;

    }
}
