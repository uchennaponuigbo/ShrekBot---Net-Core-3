﻿using System;
using Discord;
using System.Threading;
using Discord.WebSocket;
using ShrekBot.Modules.Data_Files_and_Management;

namespace ShrekBot.Modules.Swamp.Services
{
    //TODO:
    //1. Assign the correct default time of 6:00pm pst for sending a message
    //2. Allow the owner to change this time whenever he so chooses
    //3. other...
    public class TimerService
    {
        private readonly Timer _timer;
        private readonly TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
        
        public ulong GuildChnlID { get; private set; }

        public double StartingMessageInMinutes { get; private set; }
        public double RepeatingMessageInMinutes { get; private set; }
        public string RepeatingIntervalTimePST { get; private set; }

        public bool isPaused { get; private set; }
        //https://gist.github.com/Joe4evr/967949a477ed0c6c841407f0f25fa730
        public TimerService(DiscordSocketClient client)
        {          
            SetTextChannel();
            isPaused = false;
            //StartingMessageTime = 1; // 4) Time that message should fire after the timer is created
            //RepeatingMessageTime = 1; //const int Repeating = 24 * 60;
            //Start = DateTime.UtcNow.TimeOfDay.TotalMinutes; End = 24 * 60;

            SetMessageTimes(/*1, DateTime.Now.AddMinutes(1.0).ToString("h:mm tt")*/);

            

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
             
                DateTime currentTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                //next, convert it to pst
                currentTime = TimeZoneInfo.ConvertTime(currentTime, timeZone);
                //calculate the minutes until the next message using the known repeating interval value
                double nextMessageMinutes = MinutesUntilNextMessage(2);

                //add the difference into the interval datetime
                /*
                 
                 //could send in repeating message in minutes
                //or calculate the different in the interval to the next message
                //seems like the 1st option is better because it does less work and get same result
                 
                edit, nvm, just got this output
                I shout the next message every 1 minute(s). My next message will be in -1.13 minutes, at 11:12 PM PST.
                 */

                DateTime nextMessage = currentTime.AddMinutes(nextMessageMinutes);
                //result
                RepeatingIntervalTimePST = nextMessage.ToString("h:mm tt");

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
            StartingMessageInMinutes = MinutesUntilNextMessage();
            _timer.Change(TimeSpan.FromMinutes(StartingMessageInMinutes), TimeSpan.FromMinutes(RepeatingMessageInMinutes));
            isPaused = false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="newId"> Default value is the channel id for shrekbot testing</param>
        public void SetTextChannel(ulong newId = 653106031731408896) => GuildChnlID = newId;

        //6:00PM pst is 11am utc
       
        /// <summary>
        /// 
        /// </summary>
        /// <param name="repeatIntervalInMinutes">This value represent the time until the first message fires</param>
        /// <param name="intervalTimeInPST"></param>
        public void SetMessageTimes(double repeatIntervalInMinutes = 1440, string intervalTimeInPST = "6:00 PM")
        {
            RepeatingIntervalTimePST = intervalTimeInPST;
            RepeatingMessageInMinutes = repeatIntervalInMinutes < 1.0 ? 1.0 : repeatIntervalInMinutes;
            StartingMessageInMinutes = MinutesUntilNextMessage();                
        }

        //user should enter the pst time. the conversion from pst should be on the user, not the program
        //public double MinutesUntilNextMessage(int roundDigits = 2)
        //{
        //    //utc is 7 hours ahead from pst
        //    DateTime interval = DateTime.Parse(RepeatingIntervalTimePST,
        //        new System.Globalization.CultureInfo("en-US"));

        //    DateTime current = DateTime.Now.ToUniversalTime();

        //    //if this bot is run after the interval (say, 6:00 pm) but before midnight,
        //    //I'll get a negative number
        //    //which tells me of how much time has past since 6:00pm

        //    //To fix this, we check if the time is beyond the repeating interval (6:00pm), 
        //    //if it is, then we move to the next day to get the amount of minutes
        //    //until the interval in the next day
        //    if (current > interval)
        //        interval = interval.AddDays(1);
        //    TimeSpan ts = interval - current;

        //    return Math.Round(ts.TotalMinutes, roundDigits);
        //}

        /*
         * Should correlate to my time zone regardless of where this bot is hosted.
         */
        public double MinutesUntilNextMessage(int digits = 2)
        {
            DateTime interval = DateTime.SpecifyKind(DateTime.Parse(RepeatingIntervalTimePST,
                new System.Globalization.CultureInfo("en-US")), DateTimeKind.Unspecified);  
            DateTime current = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            //Converting between 2 timezones
            DateTime currentForeignTime = TimeZoneInfo.ConvertTime(current, timeZone);
            DateTime intervalForeignTime = TimeZoneInfo.ConvertTime(interval, timeZone);

            //if this bot is run after the interval (say, 6:00 pm) but before midnight,
            //I'll get a negative number
            //which tells me of how much time has past since 6:00pm

            //To fix this, we check if the time is beyond the repeating interval (6:00pm), 
            //if it is, then we move to the next day to get the amount of minutes
            //until the interval in the next day
            //    if (current > interval)
            //        interval = interval.AddDays(1);

            // 9/3/2024
            //above if statement gives wrong times for frequency of message output
            // e.g. 1 minute repeat at 4:06pm will say 1440 minutes at 4:05pm
            //bottom method would fix this but won't allow users to enter a time in the past

            //the interval adds by a minute
            //intervalForeignTime = intervalForeignTime.AddMinutes(RepeatingMessageInMinutes);

            if (currentForeignTime > intervalForeignTime)
                intervalForeignTime = intervalForeignTime.AddMinutes(RepeatingMessageInMinutes); //intervalForeignTime.AddDays(1) .AddMinutes(RepeatingMessageInMinutes)
            TimeSpan ts = intervalForeignTime - currentForeignTime;

            double minutes = Math.Round(ts.TotalMinutes, digits);
            return minutes;
        }
    }
}
