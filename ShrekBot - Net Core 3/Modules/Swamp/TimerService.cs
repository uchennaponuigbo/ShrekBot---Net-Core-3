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

        public double StartingMessageTime { get; private set; }
        public double RepeatingMessageTime { get; private set; }
        //https://gist.github.com/Joe4evr/967949a477ed0c6c841407f0f25fa730
        public TimerService(DiscordSocketClient client)
        {
            GuildChnlID = 653106031731408896; //default channel id for shrekbot testing

            //StartingMessageTime = 1; // 4) Time that message should fire after the timer is created
            //RepeatingMessageTime = 1; //const int Repeating = 24 * 60;
            //Start = DateTime.UtcNow.TimeOfDay.TotalMinutes; End = 24 * 60;

            SetMessageTimes();

            Random rand = new Random();
            _timer = new Timer(async _ =>
            {
                //Any code you want to periodically run goes here
                IMessageChannel chnl = client.GetChannel(GuildChnlID) as IMessageChannel;
                if (chnl != null)
                {
                    ShrekMessage randShrekMessage = new ShrekMessage();
                    int index = rand.Next(1, randShrekMessage.PairCount + 1); //quote 1 to quote n
                    await chnl.SendMessageAsync($"{randShrekMessage.GetValue(index.ToString())}");
                }
                    
            },
            null,
            TimeSpan.FromMinutes(StartingMessageTime), 
            TimeSpan.FromMinutes(RepeatingMessageTime));
            
        }

        public void Stop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void Restart() 
        {
            SetMessageTimes();
            _timer.Change(TimeSpan.FromMinutes(StartingMessageTime), TimeSpan.FromMinutes(RepeatingMessageTime));
        }
       
        public void ChangeMessageTimes(double newStart, double newRepeat)
        {
            StartingMessageTime = newStart;
            RepeatingMessageTime = newRepeat;
        }

        public void SetTextChannel(ulong newId) => GuildChnlID = newId;

        public void SetMessageTimes(double repeatIntervalInMinutes = 24 * 60, string intervalTimePST = "6:00 PM")
        {
            DateTime interval = DateTime.Parse(intervalTimePST, 
                new System.Globalization.CultureInfo("en-US")).ToUniversalTime();

            //only need the time variable once, so it's fine to get the exact instant of time rather than
            //store the past in a variable. Will minimize the window of error
            //Edit 5/26/22, a variable is needed now
            DateTime current = DateTime.Now.ToUniversalTime();

            //if this bot is run after 6:00pm but before midnight, I'll get a negative number
            //which tells me of how much time has past since 6:00pm

            //To fix this, we check if the time is beyond the repeating interval (6:00pm), 
            //if it is, then we move to the next day to get the amount of minutes
            //until the interval in the next day
            if (current > interval)
                interval = interval.AddDays(1);
            TimeSpan ts = interval - current;

            StartingMessageTime = Math.Round(ts.TotalMinutes, 2);
            RepeatingMessageTime = repeatIntervalInMinutes;           
        }
    }
}
