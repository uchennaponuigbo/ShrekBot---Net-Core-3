using Discord.WebSocket;
using ShrekBot.Modules.Data_Files_and_Management.Database;

namespace ShrekBot.Modules.Swamp.Helpers
{
    /// <summary>
    /// Helper class used for creating URLs from the database or to Discord text channels
    /// </summary>
    internal static class URLCreate
    {
        public static string YouTube(UrlDetails youtubeUrl)
        {
            if (!youtubeUrl.isIdEmpty())
                return "https://www.youtube.com/watch?v=" + youtubeUrl.UrlId;
            return "";
        }

        public static string Twitter(UrlDetails twitterUrl)
        {
            if (!twitterUrl.isIdAndNameEmpty())
                return $"https://twitter.com/{twitterUrl.Name}/status/{twitterUrl.UrlId}";
            return "";
        }

        public static string Reddit(UrlDetails redditUrl)
        {
            if (!redditUrl.isIdAndNameEmpty())
                return $"https://www.reddit.com/r/{redditUrl.Name}/comments/{redditUrl.UrlId}/";
            return "";
        }

        public static string DiscordTextChannel(UrlDetails discordTextChannelUrl)
        {
            if (!discordTextChannelUrl.isIdEmpty())
                return $"https://discord.com/channels/{discordTextChannelUrl.DiscordMessageLinkIds}";
            return "";
        }

        public static string DiscordTextChannel(MediaDetails discordTextChannelUrl)
            => $"https://discord.com/channels/{discordTextChannelUrl.DiscordMessageLinkIds}";

        public static string DiscordTextChannel(string messageLink)
            => $"https://discord.com/channels/{messageLink}";

        public static string DiscordTextChannel(SocketMessage socketMessage)
        {
            ulong guildId = ((SocketGuildChannel)socketMessage.Channel).Guild.Id;
            ulong channelId = socketMessage.Channel.Id;
            ulong messageId = socketMessage.Id;
            return $"https://discord.com/channels/{guildId}/{channelId}/{messageId}";
        }

        /// <summary>
        /// Use this for getting the guild/channel/message format, rather than whole link
        /// </summary>
        /// <param name="socketMessage"></param>
        /// <returns></returns>
        public static string PartialDiscordTextChannel(SocketMessage socketMessage)
        {
            ulong guildId = ((SocketGuildChannel)socketMessage.Channel).Guild.Id;
            ulong channelId = socketMessage.Channel.Id;
            ulong messageId = socketMessage.Id;
            return $"{guildId}/{channelId}/{messageId}";
        }
    }
}
