using System;
using Discord;
using System.Threading;
using Discord.WebSocket;
using ShrekBot.Modules.Data_Files_and_Management;
using System.Globalization;

namespace ShrekBot.Modules.Swamp.Services
{
    //TODO:
    //1. Assign the correct default time of 6:00pm pst for sending a message
    //2. Allow the owner to change this time whenever he so chooses
    //3. other...
    public class TimerService
    {
        private readonly Timer _timer;
        //private readonly TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
        
        public ulong GuildChnlID { get; private set; }

        public double StartingMessageInMinutes { get; private set; }
        public double RepeatingMessageInMinutes { get; private set; }
        //public string RepeatingIntervalTimePST { get; private set; }
        public string RepeatingIntervalTimeUTC { get; private set; }
        public DateTime MessageSentDateTime { get; private set; }

        public bool isPaused { get; private set; }
        public TimerService(DiscordSocketClient client)
        {          
            SetTextChannel();
            isPaused = false;
            //StartingMessageInMinutes = 1; // 4) Time that message should fire after the timer is created
            //RepeatingMessageInMinutes = 1; //const int Repeating = 24 * 60;
            //Start = DateTime.UtcNow.TimeOfDay.TotalMinutes; End = 24 * 60;

            //SetMessageTimes(1, DateTime.UtcNow.Add(new TimeSpan(0,1,0)).ToString("h:mm tt"));
            //SetMessageTimes(1440 * 2, DateTime.UtcNow.Add(new TimeSpan(0, 1, 0)).ToString("h:mm tt"));
            //SetMessageTimes();

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

                AssignNewDate();
            },
            null,
            TimeSpan.FromMinutes(StartingMessageInMinutes),
            TimeSpan.FromMinutes(RepeatingMessageInMinutes));
        }

        public void Stop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            isPaused = true;
        }

        public void Restart()
        {
            AssignNewDate();
            StartingMessageInMinutes = MinutesUntilNextMessage();          
            _timer.Change(TimeSpan.FromMinutes(StartingMessageInMinutes), TimeSpan.FromMinutes(RepeatingMessageInMinutes));
            isPaused = false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="newId"> Default value is the channel id for shrekbot testing</param>
        public void SetTextChannel(ulong newId = 653106031731408896) => GuildChnlID = newId;

        //6:00PM pst is 10am utc or 11am on daylight savings time
       
        /// <summary>
        /// 
        /// </summary>
        /// <param name="repeatIntervalInMinutes">This value represent the time until the first message fires</param>
        /// <param name="intervalTimeInUTC"></param>
        public void SetMessageTimes(double repeatIntervalInMinutes = 1440, string intervalTimeInUTC = "6:00PM")
        {
            RepeatingIntervalTimeUTC = intervalTimeInUTC;
            RepeatingMessageInMinutes = repeatIntervalInMinutes < 1.0 ? 1.0 : repeatIntervalInMinutes;
            AssignNewDate();
            StartingMessageInMinutes = MinutesUntilNextMessage(); 
            
        }
        //UTC is 8 hours ahead of PST during Daylight Savings Time or 7 without it
        public string MessageTimeInPST()
        {
            TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            DateTime nextMessageTime = DateTime.SpecifyKind(DateTime.Parse(RepeatingIntervalTimeUTC,
                new System.Globalization.CultureInfo("en-US")), DateTimeKind.Utc);
            bool daylightSaving = TimeZoneInfo.ConvertTime(nextMessageTime, timeZone).IsDaylightSavingTime();

            if(!daylightSaving)
                nextMessageTime = nextMessageTime.Subtract(new TimeSpan(8, 0, 0));
            else
                nextMessageTime = nextMessageTime.Subtract(new TimeSpan(7, 0, 0));
            return nextMessageTime.ToString("h:mmtt");
            //subtract 8 hours, return new time, profit
        }

        public double MinutesUntilNextMessage()
        {
            DateTime currentTime = DateTime.UtcNow;
            DateTime nextMessageTime = DateTime.SpecifyKind(DateTime.Parse(RepeatingIntervalTimeUTC,
                new System.Globalization.CultureInfo("en-US")), DateTimeKind.Utc);

            if (nextMessageTime < currentTime)
                nextMessageTime = nextMessageTime.AddDays(1);

            TimeSpan ts = nextMessageTime - currentTime;

            double minutes = Math.Round(ts.TotalMinutes, 2);
            if (minutes < 0.0)
                minutes += RepeatingMessageInMinutes; //1440

            return minutes;
        }

        //public double MinutesUntilNextMessage()
        //{
        //    DateTime currentTime = DateTime.UtcNow;

        //    TimeSpan ts = MessageSentDateTime - currentTime;

        //    double minutes = Math.Round(ts.TotalMinutes, 2);

        //    return minutes;
        //}

        private void AssignNewDate()
        {
            DateTime currentTime = DateTime.UtcNow;
            currentTime = currentTime.AddMinutes(RepeatingMessageInMinutes)/*.AddSeconds(-currentTime.Second)*/;
            RepeatingIntervalTimeUTC = currentTime.ToString("h:mmtt");
            MessageSentDateTime = currentTime;

            //DateTime nextMessageTime = DateTime.SpecifyKind(DateTime.Parse(RepeatingIntervalTimeUTC,
            //    new System.Globalization.CultureInfo("en-US")), DateTimeKind.Utc);

            //if (nextMessageTime < currentTime)
            //    nextMessageTime = nextMessageTime.AddDays(1);
        }

        public string PrintTime()
        {
            return "";
        }
        /*
         * Should correlate to my time zone regardless of where this bot is hosted.
         */
        //public double MinutesUntilNextMessage()
        //{
        //    DateTime interval = DateTime.SpecifyKind(DateTime.Parse(RepeatingIntervalTimePST,
        //        new System.Globalization.CultureInfo("en-US")), DateTimeKind.Unspecified);  
        //    DateTime current = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
        //    //Converting between 2 timezones
        //    DateTime currentForeignTime = TimeZoneInfo.ConvertTime(current, timeZone);
        //    DateTime intervalForeignTime = TimeZoneInfo.ConvertTime(interval, timeZone);

        //    //if this bot is run after the interval (say, 6:00 pm) but before midnight,
        //    //I'll get a negative number
        //    //which tells me of how much time has past since 6:00pm

        //    //To fix this, we check if the time is beyond the repeating interval (6:00pm), 
        //    //if it is, then we move to the next day to get the amount of minutes
        //    //until the interval in the next day
        //    //    if (current > interval)
        //    //        interval = interval.AddDays(1);

        //    // 9/3/2024
        //    //above if statement gives wrong times for frequency of message output
        //    // e.g. 1 minute repeat at 4:06pm will say 1440 minutes at 4:05pm
        //    //bottom method would fix this but won't allow users to enter a time in the past

        //    //the interval adds by a minute
        //    //intervalForeignTime = intervalForeignTime.AddMinutes(RepeatingMessageInMinutes);

        //    if (currentForeignTime > intervalForeignTime)
        //        intervalForeignTime = intervalForeignTime.AddMinutes(RepeatingMessageInMinutes); //intervalForeignTime.AddDays(1) .AddMinutes(RepeatingMessageInMinutes)
        //    TimeSpan ts = intervalForeignTime - currentForeignTime;

        //    double minutes = Math.Round(ts.TotalMinutes, 2);
        //    return minutes;
        //}
    }
}
