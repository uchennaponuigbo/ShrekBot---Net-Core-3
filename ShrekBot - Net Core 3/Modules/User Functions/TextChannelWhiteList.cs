using System.Text;

namespace Shrekbot
{
    /// <summary>
    /// If a channel is deleted and it's on the white list, then it'll be removed
    /// </summary>
    internal class TextChannelWhiteList
    {
        /// <summary>
        /// ulong is channel id
        /// string is name of channel
        /// </summary>
        private static System.Collections.Concurrent.ConcurrentDictionary<ulong, string> _channelWhiteList;
        //internal TextChannelWhiteList() 
        //{
        //    //_channelWhiteList = new System.Collections.Concurrent.ConcurrentDictionary<ulong, string>();
        //    //_channelWhiteList.GetOrAdd(653106031731408896, "shrekbot-testing"); //start with testing text channel
        //    //uncomment when I eventually release this bot
        //    //_channelWhiteList.GetOrAdd(553092290990571535, "general-swamp");
        //    //_channelWhiteList.GetOrAdd(553090651957493760, "shrek-meme-storage");
        //    //_channelWhiteList.GetOrAdd(563113644066340895, "link-vids");
        //}

        static TextChannelWhiteList()
        {
            _channelWhiteList = new System.Collections.Concurrent.ConcurrentDictionary<ulong, string>();
            _channelWhiteList.GetOrAdd(653106031731408896, "shrekbot-testing"); //start with testing text channel

            _channelWhiteList.GetOrAdd(553092290990571535, "general-swamp");
            _channelWhiteList.GetOrAdd(553090651957493760, "shrek-meme-storage");
            _channelWhiteList.GetOrAdd(563113644066340895, "link-vids");
        }

        internal static bool ContainsId(ulong channelId) => _channelWhiteList.ContainsKey(channelId);

        internal static void Add(ulong channelId, string channelName) => _channelWhiteList.GetOrAdd(channelId, channelName);
        
        internal static bool Remove(ulong channelId) => _channelWhiteList.TryRemove(channelId, out _);

        internal static string Print()
        {
            StringBuilder sb = new StringBuilder("Text Channels in White list for checking web links\n");
            foreach (var item in _channelWhiteList)
                sb.AppendLine($"**{item.Value}**");
            return sb.ToString();
        }
        
    }
}
