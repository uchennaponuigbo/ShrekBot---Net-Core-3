using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShrekBot.Modules.Data_Files_and_Management.Database
{
    internal partial class SwampDB
    {
        /// <summary>
        /// Selects a user by discord user id and regexed keyword
        /// </summary>
        /// <param name="tableDomain"></param>
        /// <param name="discordUserId"></param>
        /// <param name="regexed_videoId"></param>
        /// <returns>An array of records that is no greater than 5, or an empty array if there is no data</returns>
        internal UrlDetails[] SelectFrom_Table(WebDomain tableDomain, ulong discordUserId, string regexed_videoId)
        {
            UrlDetails[] urlDetails;
            switch (tableDomain)
            {
                case WebDomain.YouTube:
                    urlDetails = SelectFrom_Youtube(discordUserId, regexed_videoId); break;
                case WebDomain.Twitter:
                    urlDetails = SelectFrom_Twitter(discordUserId, regexed_videoId); break;
                case WebDomain.Reddit:
                    urlDetails = SelectFrom_Reddit(discordUserId, regexed_videoId); break;
                default:
                    urlDetails = new UrlDetails[0]; break;
            }
            return urlDetails;
        }

        private UrlDetails[] SelectFrom_Youtube(ulong discordUserId, string regexed_videoId)
        {
            using (IDbConnection connection = new SQLiteConnection(_connectionString))
            {
                string sql = SelectFromTable(regexed_videoId, discordUserId, "Youtube_Links", "video_id");
                UrlDetails db = new UrlDetails();
                var parameters = new
                {
                    video_id = db.UrlId,
                    discord_message_link_id = db.DiscordMessageLinkIds,
                    db.timestamp_created
                };

                return connection.Query<UrlDetails>(sql, parameters, null, true, _DBTimeoutSec).ToArray();
            }
        }

        private UrlDetails[] SelectFrom_Twitter(ulong discordUserId, string regexed_tweetId)
        {
            using (IDbConnection connection = new SQLiteConnection(_connectionString))
            {
                string sql = SelectFromTable(regexed_tweetId, discordUserId, "Twitter_Links", "tweet_id", "t_username");
                UrlDetails db = new UrlDetails();
                var parameters = new
                {
                    tweet_id = db.UrlId,
                    t_username = db.Name,
                    discord_message_link_id = db.DiscordMessageLinkIds,
                    db.timestamp_created
                };

                return connection.Query<UrlDetails>(sql, parameters, null, true, _DBTimeoutSec).ToArray();
            }
        }

        private UrlDetails[] SelectFrom_Reddit(ulong discordUserId, string regexed_redditId)
        {
            using (IDbConnection connection = new SQLiteConnection(_connectionString))
            {
                string sql = SelectFromTable(regexed_redditId, discordUserId, "Reddit_Links", "reddit_id", "r_username");
                UrlDetails db = new UrlDetails();
                var parameters = new
                {
                    reddit_id = db.UrlId,
                    r_username = db.Name,
                    discord_message_link_id = db.DiscordMessageLinkIds,
                    db.timestamp_created
                };

                return connection.Query<UrlDetails>(sql, parameters, null, true, _DBTimeoutSec).ToArray();
            }
        }

        /// <summary>
        /// Selects a user by discord user id and hash value of image/video
        /// </summary>
        /// <param name="tableDomain"></param>
        /// <param name="discordUserId"></param>
        /// <param name="regexed_videoId"></param>
        /// <returns>An array of records that is no greater than 5, or an empty array if there is no data</returns>
        internal MediaDetails[] SelectFrom_Table(Media tableMedia, ulong discordUserId, ulong hash)
        {
            MediaDetails[] mediaDetails;
            switch (tableMedia)
            {
                case Media.Image:
                    mediaDetails = SelectFrom_Images(discordUserId, hash); break;
                case Media.Video:
                    mediaDetails = SelectFrom_Videos(discordUserId, hash); break;
                default:
                    mediaDetails = new MediaDetails[0]; break;
            }
            return mediaDetails;
        }

        private MediaDetails[] SelectFrom_Images(ulong discordUserId, ulong hash)
        {
            using (IDbConnection connection = new SQLiteConnection(_connectionString))
            {
                string sql = SelectFromTable(hash, discordUserId, "Images");
                MediaDetails db = new MediaDetails();
                var parameters = new
                {
                    hash = db.Hash,
                    discord_message_link_id = db.DiscordMessageLinkIds,
                    db.timestamp_created
                };

                return connection.Query<MediaDetails>(sql, parameters, null, true, _DBTimeoutSec).ToArray();
            }
        }

        private MediaDetails[] SelectFrom_Videos(ulong discordUserId, ulong hash)
        {
            using (IDbConnection connection = new SQLiteConnection(_connectionString))
            {
                string sql = SelectFromTable(hash, discordUserId, "Videos");
                MediaDetails db = new MediaDetails();
                var parameters = new
                {
                    hash = db.Hash,
                    discord_message_link_id = db.DiscordMessageLinkIds,
                    db.timestamp_created
                };

                return connection.Query<MediaDetails>(sql, parameters, null, true, _DBTimeoutSec).ToArray();
            }
        }

        /// <summary>
        /// Selects the most recent discord message link entries based on the hash value of the image/video
        /// </summary>
        /// <param name="tableMedia"></param>
        /// <returns>Returns at most the latest 5 entries, or an empty array if there is no data</returns>
        internal string[] SelectFrom_Table(Media tableMedia, ulong hash)
        {
            if (hash == 0)
                return new string[0];
            switch (tableMedia)
            {
                case Media.Image:
                    return SelectMessageLinksFrom_Table(hash, "", "Images", "hash");                      
                case Media.Video:
                    return SelectMessageLinksFrom_Table(hash, "", "Videos", "hash");
                default:
                    return new string[0];
            }
            //string sql = SelectFromTable()
            //should I join this statement in the other functions and add another parameter? or should I keep it seperate here?
            //questions... questions...

        }

        /// <summary>
        /// Selects the most recent discord message link entries based on the regexed id of the weblink
        /// </summary>
        /// <param name="tableMedia"></param>
        /// <returns>At most the latest 5 entries, or an empty array if there is no data</returns>
        internal string[] SelectFrom_Table(WebDomain tableDomain, string regexedId)
        {
            if (string.IsNullOrEmpty(regexedId))
                return new string[0];
            switch (tableDomain)
            {
                case WebDomain.YouTube:
                    return SelectMessageLinksFrom_Table(0, regexedId, "Youtube_Links", "video_id");
                case WebDomain.Twitter:
                    return SelectMessageLinksFrom_Table(0, regexedId, "Twitter_Links", "tweet_id");
                case WebDomain.Reddit:
                    return SelectMessageLinksFrom_Table(0, regexedId, "Reddit_Links", "reddit_id");
                default:
                    return new string[0];
            }
            //string sql = SelectFromTable()
            //should I join this statement in the other functions and add another parameter? or should I keep it seperate here?
            //questions... questions...

        }

        private string[] SelectMessageLinksFrom_Table(ulong hash, string regexedId, string table_name, string id_Column_name)
        {
            using (IDbConnection connection = new SQLiteConnection(_connectionString))
            {
                string sql = SelectFromTable(table_name, id_Column_name, hash, regexedId);
                return connection.Query<string>(sql, null, null, true, _DBTimeoutSec).ToArray();
            }
        }
        /// <summary>
        /// Gets the count of records from all tables in the database
        /// </summary>
        /// <returns>The number of records per table preformatted</returns>
        internal string SelectCountOfRecordsFromAllTables()
        {
            using (IDbConnection connection = new SQLiteConnection(_connectionString))
            {
                string sql = AllTableRecordsCount();

                Tuple<string, Int64>[] query = connection.Query<string, Int64, Tuple<string, Int64>>
                    (sql, Tuple.Create, null, null, true, splitOn: "*", _DBTimeoutSec).ToArray();

                StringBuilder result = new StringBuilder("Tables and Record Count\n");
                for(int i = 0; i < query.Length; i++)
                {
                    result.Append($"{query[i].Item1} | {query[i].Item2}\n");
                }
                result.Length--; //https://stackoverflow.com/a/17215160/9521550
                return result.ToString();
            }
        }
    }
}
