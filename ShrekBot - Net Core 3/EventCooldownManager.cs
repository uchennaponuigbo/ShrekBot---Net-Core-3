using System;
using System.Collections.Concurrent;

namespace ShrekBot
{
    internal class EventCooldownManager
    {
        private const double MessageCoolDown = 10.0;
        private const double ImageCoolDown = 1.0;

        private struct Cooldown
        {
            public DateTimeOffset message;
            public DateTimeOffset image;

            public Cooldown()
            {
                message = new DateTimeOffset(DateTime.MinValue, TimeSpan.Zero);
                image = new DateTimeOffset(DateTime.MinValue, TimeSpan.Zero);
            }
        }

        private readonly ConcurrentDictionary<ulong, Cooldown> _executionTimestamps;

        public EventCooldownManager()
        {
            _executionTimestamps = new ConcurrentDictionary<ulong, Cooldown>();
        }

        private bool CheckCooldown(ulong userId, double cooldownTime)
        {
            return _executionTimestamps.TryGetValue(userId,
                        out var lastExecution) && (DateTimeOffset.Now - lastExecution.message)
                                                < TimeSpan.FromSeconds(cooldownTime);
        }

        public bool IsMessageOnCooldown(ulong userId)
        {
            if(CheckCooldown(userId, MessageCoolDown))
            {
                return true;
            }

            Cooldown modified = new Cooldown();
            modified.message = DateTimeOffset.Now;

            //the server is small, I'm not deleting any keys
            if(_executionTimestamps.TryGetValue(userId, out Cooldown _))
                modified.image = _executionTimestamps[userId].image;
            

            _executionTimestamps[userId] = modified;
            return false;
        }

        public bool IsImageOnCooldown(ulong userId)
        {
            if (CheckCooldown(userId, ImageCoolDown))
            {
                return true;
            }

            Cooldown modified = new Cooldown();
            modified.image = DateTimeOffset.Now;

            //the server is small, I'm not deleting any keys
            if (_executionTimestamps.TryGetValue(userId, out Cooldown _))
                modified.message = _executionTimestamps[userId].message;


            _executionTimestamps[userId] = modified;
            return false;
        }
    }
}
