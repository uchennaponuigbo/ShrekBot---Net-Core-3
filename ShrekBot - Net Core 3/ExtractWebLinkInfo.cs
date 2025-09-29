using System;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace ShrekBot
{ 
    internal struct UrlDetails
    {
        /// <summary>
        /// Unique set of characters and/or numbers that define the website url
        /// </summary>
        public string UrlId;
        /// <summary>
        /// username or name of subreddit, or nothing if its YouTube
        /// </summary>
        public string Name;

        public UrlDetails(string uid, string name)
        {
            UrlId = uid;
            this.Name = name;
        }

        public bool isIdEmpty() => string.IsNullOrEmpty(UrlId);

        public override string ToString()
        {
            return $"{Name} | {UrlId}";
        }
    }

    internal class ExtractWebLinkInfo
    {
        internal enum WebDomain
        {
            None = 0,
            YouTube = 1,
            Twitter = 2,
            Reddit = 3
        }

        internal WebDomain Domain { get; private set; }
        internal ExtractWebLinkInfo() { Domain = WebDomain.None;  }

        public UrlDetails ExtractURL(string discordMessage)
        {
            Domain = WebDomain.None;
            UrlDetails details = new UrlDetails();

            Uri uri = CheckUrl(discordMessage);
            if (uri == null)
                return details; //return empty strings, avoid the checks

            //optional group at the very end breaks my current logic for non-youtube links, so I have to account for that
            string[] ifThereIsAddedGarbageInUrl = discordMessage.Split("?");
            string nonYoutubeLink = ifThereIsAddedGarbageInUrl[0];

            //We want to avoid running this whole function every time someone sends a message, due to the Regex checks
            details = CheckIfTwitterURL(nonYoutubeLink);
            if (Domain == WebDomain.Twitter)
                return details;

            details = CheckIfRedditURL(nonYoutubeLink);
            if (Domain == WebDomain.Reddit)
                return details;

            if (details.isIdEmpty()) //if the twitter check fails, we go to the youtube check
            {
                details.Name = ""; //we don't need the twitter username to be a part of the youtube check
                string youtubeVideoId = GetYouTubeVideoIdFromUrl(discordMessage, uri);
                details.UrlId = youtubeVideoId;
            }
            return details;
        }

        public bool IsUrlDetailsEmpty(UrlDetails details) 
            => details.isIdEmpty() && string.IsNullOrEmpty(details.Name);

        private Uri CheckUrl(string possibleURL)
        {
            Uri uri = null;
            if (!Uri.TryCreate(possibleURL, UriKind.Absolute, out uri))
            {
                try
                {
                    uri = new UriBuilder("http", possibleURL).Uri;
                }
                catch(UriFormatException)
                {
                    // invalid url
                    return null;
                }
            }
            return uri;
        }

        private UrlDetails GetIdAndNameFromUrl(string guaranteedUrl, int idOffset, int nameOffset, WebDomain thisDomain)
        {
            Domain = thisDomain;
            string[] splits = guaranteedUrl.Split("/");
            string urlId = splits[splits.Length - idOffset];
            string username = splits[splits.Length - nameOffset];

            return new UrlDetails(urlId, username);
        }

        private UrlDetails CheckIfTwitterURL(string possibleTwitterUrl)
        {
            Regex regex = 
                new Regex("^(htt(p|ps):\\/\\/|www\\.|htt(p|ps):\\/\\/www\\.)((fx)?twitter|(fixup)?x)\\.com\\/\\w{1,15}\\/status\\/\\d{1,19}$"); //(\\?s=\\d{1,2}\\&t=\\w{1,22})?
            //extra regex fluff commented out
            if (regex.IsMatch(possibleTwitterUrl))
            {
                //string[] ifThereIsAddedGarbageInUrl = possibleTwitterUrl.Split("?");
                return GetIdAndNameFromUrl(possibleTwitterUrl, 1, 3, WebDomain.Twitter); //ifThereIsAddedGarbageInUrl[0]
            }
            return new UrlDetails(); //invalid URL
        }       

        public UrlDetails CheckIfRedditURL(string possibleRedditUrl)
        {
            Regex regex = 
                new Regex("^(htt(p|ps):\\/\\/(old\\.|new\\.)?|htt(p|ps):\\/\\/www\\.)reddit\\.com\\/r\\/\\w{1,21}\\/comments\\/\\w{1,8}\\/\\w{1,300}\\/$");

            if (regex.IsMatch(possibleRedditUrl))
            {
                return GetIdAndNameFromUrl(possibleRedditUrl, 3, 5, WebDomain.Reddit);
            }
            return new UrlDetails();

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

        public string CreateYouTubeURL(UrlDetails youtubeUrl)
        {
            if (!youtubeUrl.isIdEmpty())
                return "https://www.youtube.com/watch?v=" + youtubeUrl.UrlId;
            return "";
        }

        public string CreateTwitterURL(UrlDetails twitterUrl)
        {
            if (!twitterUrl.isIdEmpty())
                return $"https://twitter.com/{twitterUrl.Name}/status/{twitterUrl.UrlId}";
            return "";
        }

        public string CreateRedditURL(UrlDetails redditUrl)
        {
            if (!redditUrl.isIdEmpty() && redditUrl.Name != "")
                return $"https://www.reddit.com/r/{redditUrl.Name}/comments/{redditUrl.UrlId}/";
            return "";
        }
    }
}
