using Dapper;
using System.Data;
using System.Data.SQLite;
using System.Linq;

namespace ShrekBot.Modules.Database
{
    internal partial class SwampDB
    {
        /// <summary>
        /// Inserts records, and deletes the oldest record when there is at least 5 of the same record in the database
        /// </summary>
        /// <param name="tableDomain"></param>
        /// <param name="urlDetails"></param>
        /// <param name="discordUserId"></param>
        /// <returns>The number of rows inserted. Or -1, if unable to</returns>
        internal int InsertInto_Table(WebDomain tableDomain, UrlDetails urlDetails, ulong discordUserId)
        {
            switch (tableDomain)
            {
                case WebDomain.YouTube:
                    return InsertInto(urlDetails, discordUserId, "Youtube_Links", "video_id");
                case WebDomain.Twitter:
                    return InsertInto(urlDetails, discordUserId, "Twitter_Links", "tweet_id", "t_username");
                case WebDomain.Reddit:
                    return InsertInto(urlDetails, discordUserId, "Reddit_Links", "reddit_id", "r_username");
                default:
                    return -1;
            }
        }

        private int InsertInto(UrlDetails urlDetails, ulong discordUserId, string table_name,
             string id_Column, string urlName_Column = "")
        {
            using (IDbConnection connection = new SQLiteConnection(_connectionString))
            {
                string sql = InsertIntoValues(urlDetails, discordUserId, table_name, id_Column, urlName_Column);
                int rowsAffected = connection.Execute(sql);
                if (rowsAffected > 0)
                {
                    int records = 0;
                    var parameters = new
                    {
                        count = records
                    };
                    int duplicates = connection.Query<int>
                        (CountRecordsOfUser(table_name, discordUserId, id_Column, urlDetails), parameters, null, true, _DBTimeoutSec)
                        .ToArray()[0];
                    if (duplicates > _MaxDuplicates)
                    {
                        string deleteQuery = DeleteOneRecordFromTable(urlDetails, discordUserId, table_name, id_Column);
                        connection.Execute(deleteQuery, null, null, _DBTimeoutSec);
                    }
                }
                return rowsAffected;
            }
        }

        /// <summary>
        /// Inserts records, and deletes the oldest record when there is at least 5 of the same record in the database
        /// </summary>
        /// <param name="tableMedia"></param>
        /// <param name="mediaDetails"></param>
        /// <param name="discordUserId"></param>
        /// <returns>The number of rows inserted. Or -1, if unable to</returns>
        internal int InsertInto_Table(Media tableMedia, MediaDetails mediaDetails, ulong discordUserId)
        {
            switch (tableMedia)
            {
                case Media.Image:
                    return InsertInto(mediaDetails, discordUserId, "Images");
                case Media.Video:
                    return InsertInto(mediaDetails, discordUserId, "Videos");
                default:
                    return -1;
            }
        }

        private int InsertInto(MediaDetails mediaDetails, ulong discordUserId, string table_name)
        {
            using (IDbConnection connection = new SQLiteConnection(_connectionString))
            {
                string sql = InsertIntoValues(mediaDetails, discordUserId, table_name);
                int rowsAffected = connection.Execute(sql);
                if (rowsAffected > 0)
                {
                    int records = 0;
                    var parameters = new
                    {
                        count = records
                    };
                    int duplicates = connection.Query<int>
                        (CountRecordsOfUser(table_name, discordUserId, mediaDetails), parameters, null, true, _DBTimeoutSec)
                        .ToArray()[0];
                    if (duplicates > _MaxDuplicates)
                    {
                        string deleteQuery = DeleteOneRecordFromTable(mediaDetails, discordUserId, table_name);
                        connection.Execute(deleteQuery, null, null, _DBTimeoutSec);
                    }
                }
                return rowsAffected;
            }
        }
    }
}
