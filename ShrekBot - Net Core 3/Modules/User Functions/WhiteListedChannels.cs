namespace Shrekbot
{
    internal class WhiteListedChannels
    {
        /// <summary>
        /// ulong is channel id
        /// string is name of channel
        /// </summary>
        private System.Collections.Concurrent.ConcurrentDictionary<ulong, string> _channelWhiteList;
        internal WhiteListedChannels() 
        {
            _channelWhiteList = new System.Collections.Concurrent.ConcurrentDictionary<ulong, string>();
            _channelWhiteList.GetOrAdd(653106031731408896, "shrekbot-testing"); //start with testing text channel
            //uncomment when I eventually release this bot
            //_channelWhiteList.GetOrAdd(553092290990571535, "general-swamp");
            //_channelWhiteList.GetOrAdd(553090651957493760, "shrek-meme-storage");
            //_channelWhiteList.GetOrAdd(563113644066340895, "link-vids");
        }

        internal bool ContainsId(ulong channelId) => _channelWhiteList.ContainsKey(channelId);

        internal void Add(ulong channelId, string channelName)
        {
            if(!ContainsId(channelId))
                _channelWhiteList.GetOrAdd(channelId, channelName);
        }

        internal void Remove(ulong channelId)
        {
            _channelWhiteList.TryRemove(channelId, out _);
        }
    }
}
