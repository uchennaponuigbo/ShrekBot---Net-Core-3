using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using ShrekBot.Modules.Data_Files_and_Management.Database;

namespace ShrekBot
{
    //1/17/2026 FUNCTIONALITY IS DONE. COPY TO SHREKBOT
    //1/22/2026 ADDING OBJECT TO REFERENCE DISCORD MESSAGELINK
    //2/19/2026 EVEN MORE GARBAGE ADDED
    
    internal class ExtractWebLinkInfo
    {
        internal WebDomain Domain { get; private set; }
        internal ExtractWebLinkInfo() { Domain = WebDomain.None; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="discordMessage"></param>
        /// <param name="discordMessageIds">guildid/serverid | channelid | messageid</param>
        /// <returns></returns>
        internal UrlDetails ExtractURL(string discordMessage, Tuple<ulong, ulong, ulong> discordMessageIds)
        {
            Domain = WebDomain.None;
            UrlDetails details = new UrlDetails();

            Uri uri = CheckUrl(discordMessage);
            if (uri == null)
                return details; //return empty strings, avoid the checks

            //optional group at the very end breaks my current logic for non-youtube links, so I have to account for that
            string[] removeTheAddedGarbageInUrl = discordMessage.Split("?");
            string nonYoutubeLink = removeTheAddedGarbageInUrl[0];

            //We want to try and avoid running this whole function every time someone sends a message, due to the Regex checks

            details = CheckIfTwitterURL(nonYoutubeLink, discordMessageIds);
            if (Domain == WebDomain.Twitter)
                return details;

            details = CheckIfRedditURL(nonYoutubeLink, discordMessageIds);
            if (Domain == WebDomain.Reddit)
                return details;

            if (details.isIdEmpty()) //if the twitter check fails, we go to the youtube check
            {
                details.Name = ""; //we don't need the twitter username to be a part of the youtube check
                string youtubeVideoId = GetYouTubeVideoIdFromUrl(discordMessage, uri);
                details.UrlId = youtubeVideoId;
                ExtractDiscordUrl(ref details, discordMessageIds.Item1, discordMessageIds.Item2, discordMessageIds.Item3);
            }
            return details;
        }
        /// <summary>
        /// Structured as "serverid/channelid/messageid"
        /// </summary>
        /// <param name="guildId"></param>
        /// <param name="channelId"></param>
        /// <param name="messageId"></param>
        /// <returns></returns>
        public string ExtractDiscordUrl(ulong guildId, ulong channelId, ulong messageId)
        {
            //serverid/channelid/messageid
            return $"{guildId}/{channelId}/{messageId}";
            //example
            //https://discord.com/channels/370001518927020032/370001518927020037/370080067428024320
        }

        /// <summary>
        /// Structured as "serverid/channelid/messageid"
        /// </summary>
        /// <param name="guildId"></param>
        /// <param name="channelId"></param>
        /// <param name="messageId"></param>
        public void ExtractDiscordUrl(ref UrlDetails url, ulong guildId, ulong channelId, ulong messageId)
        {
            //serverid/channelid/messageid
            string messageLink = $"{guildId}/{channelId}/{messageId}";
            url.DiscordMessageLinkIds = messageLink;
        }

        public bool IsUrlDetailsEmpty(UrlDetails details)
            => details.isIdEmpty() && string.IsNullOrEmpty(details.Name);

        private UrlDetails GetIdAndNameFromUrl(string guaranteedUrl, int idOffset, int nameOffset,
            WebDomain thisDomain, Tuple<ulong, ulong, ulong> discordMessageIds)
        {
            Domain = thisDomain;
            string[] splits = guaranteedUrl.Split("/");
            string urlId = splits[splits.Length - idOffset];
            string username = splits[splits.Length - nameOffset];
            string messageLinks = ExtractDiscordUrl(discordMessageIds.Item1, discordMessageIds.Item2, discordMessageIds.Item3);

            return new UrlDetails(urlId, username, messageLinks);
        }

        private UrlDetails CheckIfTwitterURL(string possibleTwitterUrl, Tuple<ulong, ulong, ulong> discordMessageIds)
        {
            Regex regex =
                new Regex("^(htt(p|ps):\\/\\/|www\\.|htt(p|ps):\\/\\/www\\.)(((f|v)x)?twitter|fixvx|(fixup)?x)\\.com\\/\\w{1,15}\\/status\\/\\d{1,19}$"); //(\\?s=\\d{1,2}\\&t=\\w{1,22})?
            //extra regex fluff commented out
            if (regex.IsMatch(possibleTwitterUrl))
            {
                //string[] ifThereIsAddedGarbageInUrl = possibleTwitterUrl.Split("?");
                return GetIdAndNameFromUrl(possibleTwitterUrl, 1, 3, WebDomain.Twitter, discordMessageIds); //ifThereIsAddedGarbageInUrl[0]
            }
            return new UrlDetails(); //invalid URL
        }

        private UrlDetails CheckIfRedditURL(string possibleRedditUrl, Tuple<ulong, ulong, ulong> discordMessageIds)
        {
            Regex regex =
                new Regex("^(htt(p|ps):\\/\\/(old\\.|new\\.)?|htt(p|ps):\\/\\/www\\.)reddit\\.com\\/r\\/\\w{1,21}\\/comments\\/\\w{1,8}\\/\\w{1,300}\\/$");

            if (regex.IsMatch(possibleRedditUrl))
            {
                return GetIdAndNameFromUrl(possibleRedditUrl, 3, 5, WebDomain.Reddit, discordMessageIds);
            }
            return new UrlDetails();

        }

        private Uri CheckUrl(string possibleURL)
        {
            Uri uri = null;
            if (!Uri.TryCreate(possibleURL, UriKind.Absolute, out uri))
            {
                try
                {
                    uri = new UriBuilder("http", possibleURL).Uri;
                }
                catch (UriFormatException)
                {
                    // invalid url
                    return null;
                }
            }
            return uri;
        }

        private string GetYouTubeVideoIdFromUrl(string youtubeUrl, Uri uri)
        {
            const string YouTube_Video_Id_Regex = @"^[a-zA-Z0-9_-]{11}$";

            string host = uri.Host;
            string[] youTubeHosts = { "www.youtube.com", "youtube.com", "youtu.be", "www.youtu.be", "m.youtube.com", "www.youtube-nocookie.com", "youtube-nocookie.com" }; //, "http://m.youtube.com" https://www.youtube-nocookie.com/embed/lalOy8Mbfdc?rel=0
            if (!youTubeHosts.Contains(host))
                return "";

            NameValueCollection query = HttpUtility.ParseQueryString(uri.Query);
            Regex regex = new Regex(YouTube_Video_Id_Regex);

            if (query.AllKeys.Contains("v"))
            {
                Domain = WebDomain.YouTube;
                //return Regex.Match(query["v"], YouTube_Video_Id_Regex).Value;
                return regex.Match(query["v"]).Value;
                //return query["v"].Replace("/", "");
            }
            else if (query.AllKeys.Contains("u"))
            {
                if (query.AllKeys.Contains("a")) //contains an attribution link
                {
                    //return Regex.Match(query["a"], YouTube_Video_Id_Regex).Value;
                    Domain = WebDomain.YouTube;
                    return regex.Match(query["a"]).Value;
                }
                // some urls have something like "u=/watch?v=AAAAAAAAA16"
                Domain = WebDomain.YouTube;
                return Regex.Match(query["u"], $@"/watch\?v=({YouTube_Video_Id_Regex})").Groups[1].Value; //[a-zA-Z0-9_-]{11}
            }
            else
            {
                // remove a trailing forward space
                string last = uri.Segments.Last().Replace("/", "");

                if (regex.IsMatch(last))//@"^v=[a-zA-Z0-9_-]{11}$" //Regex.IsMatch(last, YouTube_Video_Id_Regex
                {
                    Domain = WebDomain.YouTube;
                    return last.Replace("v=", "");
                }


                string[] segments = uri.Segments;
                if (segments.Length > 2 && segments[segments.Length - 2] != "v/" && segments[segments.Length - 2] != "watch/")
                    return "";

                //return Regex.Match(last, @"^[a-zA-Z0-9_-]{11}$").Value;
                Domain = WebDomain.YouTube;
                return last.Split('&')[0];
            }
        }
    }
}
