
namespace ShrekBot.Modules.Data_Files_and_Management.Database
{
    internal enum WebDomain
    {
        None = 0,
        YouTube = 1,
        Twitter = 2,
        Reddit = 3
        //bluesky?
    }

    internal enum Media
    {
        None = 0,
        Image = 1,
        Video = 2
    }

    internal struct UrlDetails
    {
        /// <summary>
        /// Unique set of characters and/or numbers that define the website url. 
        /// </summary>
        public string UrlId;
        /// <summary>
        /// Twitter username or name of subreddit, or nothing if its YouTube
        /// </summary>
        public string Name;

        /// <summary>
        /// Represented as "ServerId/ChannelId/MessageId"
        /// </summary>
        public string DiscordMessageLinkIds;

        public UrlDetails(string uid, string name, string dmli)
        {
            UrlId = uid;
            this.Name = name;
            DiscordMessageLinkIds = dmli;
            timestamp_created = "";
        }

        //public UrlDetails()
        //{

        //}

        public bool isIdEmpty() => string.IsNullOrEmpty(UrlId);
        public bool isNameEmpty() => string.IsNullOrEmpty(Name);

        public bool isIdAndNameEmpty() => isIdEmpty() && isNameEmpty();

        public override string ToString()
        {
            return $"{Name} | {UrlId} | {DiscordMessageLinkIds} | {timestamp_created}";
        }
        /// <summary>
        /// Retrieved from Database
        /// </summary>
        internal string timestamp_created { get; }
    }

    internal struct MediaDetails
    {
        internal ulong Hash { get; }
        internal string DiscordMessageLinkIds { get; }

        public MediaDetails(ulong h, string dmli)
        {
            Hash = h;
            DiscordMessageLinkIds = dmli;
            timestamp_created = "";
        }
        /// <summary>
        /// Retrieved from Database
        /// </summary>
        internal string timestamp_created { get; }

        public override string ToString()
        {
            return $"Hash Value: {Hash} | {DiscordMessageLinkIds} | {timestamp_created}";
        }
    }    
}
