using System.Collections.Generic;
using System.Text;

namespace ShrekBot.Modules.Data_Files_and_Management.Database
{
    internal partial class SwampDB
    {
        /// <summary>
        /// Used on the Web link tables
        /// </summary>
        /// <param name="regexedId"></param>
        /// <param name="discordUserId"></param>
        /// <param name="table_Name"></param>
        /// <param name="urlId_Column"></param>
        /// <param name="urlName_Column"></param>
        /// <returns></returns>
        private string SelectFromTable(string regexedId, ulong discordUserId, string table_Name,
           string urlId_Column, string urlName_Column = "")
        {
            StringBuilder sb = new StringBuilder($"SELECT {urlId_Column} AS UrlId, "); //AS  UrlId
            if (!string.IsNullOrEmpty(urlName_Column))
                sb.Append($"{urlName_Column} AS Name, "); //AS Name

            sb.Append($"discord_message_link_id AS DiscordMessageLinkIds, timestamp_created FROM {table_Name} "); // AS DiscordMessageLinkIds

            sb.Append($"WHERE discord_user_id = {discordUserId} AND {urlId_Column} = '{regexedId}' ORDER BY timestamp_created");
            return sb.ToString();
        }
        /// <summary>
        /// Used on the Media Tables
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="discordUserId"></param>
        /// <param name="table_Name"></param>
        /// <returns></returns>
        private string SelectFromTable(ulong hash, ulong discordUserId, string table_Name)
        {
            StringBuilder sb = new StringBuilder("SELECT hash AS Hash, discord_message_link_id AS DiscordMessageLinkIds, ");
            sb.Append($"timestamp_created FROM {table_Name} ");
            sb.Append($"WHERE discord_user_id = {discordUserId} AND Hash = '{hash}' ORDER BY timestamp_created");
            return sb.ToString();
        }

        /// <summary>
        /// Used for checking if a different user posts a meme another user already posted
        /// </summary>
        /// <param name="table_Name"></param>
        /// <param name="table_id"></param>
        /// <param name=""></param>
        /// <returns></returns>
        private string SelectFromTable(string table_Name, string id_Column_Name, ulong hash = 0, string regexedId = "")
        {
            /*
             SELECT discord_message_link_id FROM Youtube_Links
WHERE video_id = 'yZv2daTWRZU'
ORDER BY ROWID DESC LIMIT 5
             
             */
            StringBuilder sb = new StringBuilder($"SELECT discord_message_link_id FROM {table_Name} ");
            if(!string.IsNullOrEmpty(regexedId) && hash == 0) //if I pass in a regex id, then the hash is empty
                sb.Append($"WHERE {id_Column_Name} = '{regexedId}' ORDER BY timestamp_created DESC LIMIT 5");
            else if(hash != 0 && string.IsNullOrEmpty(regexedId)) //if I pass in a hash, then the regex id is empty
                sb.Append($"WHERE {id_Column_Name} = {hash} ORDER BY timestamp_created DESC LIMIT 5");
            //it cannot be both... well...
            return sb.ToString(); //
        }

        /// <summary>
        /// Used on the Web link tables
        /// </summary>
        /// <param name="url"></param>
        /// <param name="discordUserId"></param>
        /// <param name="table_Name"></param>
        /// <param name="urlId_Column"></param>
        /// <param name="urlName_Column"></param>
        /// <returns></returns>
        private string InsertIntoValues(UrlDetails url, ulong discordUserId, string table_Name,
            string urlId_Column, string urlName_Column = "")
        {
            StringBuilder sb = new StringBuilder($"INSERT INTO {table_Name} (");
            sb.Append($"'{urlId_Column}', ");

            if (!string.IsNullOrEmpty(urlName_Column))
                sb.Append($"'{urlName_Column}', ");

            sb.Append($"discord_user_id, discord_message_link_id) VALUES ('{url.UrlId}', ");

            if (!url.isNameEmpty())
                sb.Append($"'{url.Name}', ");

            sb.Append($"'{discordUserId}', '{url.DiscordMessageLinkIds}')");
            return sb.ToString();
        }

        /// <summary>
        /// Used on the Media tables
        /// </summary>
        /// <param name="media"></param>
        /// <param name="discordUserId"></param>
        /// <param name="table_Name"></param>
        /// <returns></returns>
        private string InsertIntoValues(MediaDetails media, ulong discordUserId, string table_Name)
        {
            StringBuilder sb = new StringBuilder($"INSERT INTO {table_Name} (hash, discord_user_id, discord_message_link_id) ");
            sb.Append($"VALUES ({media.Hash}, {discordUserId}, '{media.DiscordMessageLinkIds}')");

            return sb.ToString();
        }

        private string InsertIntoValues_Bulk(Dictionary<MediaDetails, byte> mediaDetails, ulong discordUserId, string table_Name)
        {
            StringBuilder sb = new StringBuilder($"INSERT INTO {table_Name} (hash, discord_user_id, discord_message_link_id) ");
            foreach(KeyValuePair<MediaDetails, byte> media in mediaDetails)
            {
                sb.Append($"VALUES ({media.Key.Hash}, {discordUserId}, '{media.Key.DiscordMessageLinkIds}'),");
            }
            sb.Length--;

            return sb.ToString();
        }

        private string DeleteOneRecordFromTable(UrlDetails url, ulong discordUserId,
            string table_Name, string urlId_Column)
        {
            return $"DELETE FROM {table_Name} WHERE ROWID IN (SELECT ROWID FROM {table_Name} " +
                $"WHERE discord_user_id = {discordUserId} AND {urlId_Column} = '{url.UrlId}' ORDER BY ROWID ASC LIMIT 1)";
        }

        private string DeleteOneRecordFromTable(MediaDetails media, ulong discordUserId,
            string table_Name)
        {
            return $"DELETE FROM {table_Name} WHERE ROWID IN (SELECT ROWID FROM {table_Name} " +
                $"WHERE discord_user_id = {discordUserId} AND hash = {media.Hash} ORDER BY ROWID ASC LIMIT 1)";
        }

        private string DeleteNRecordsFromTable_Bulk(ulong hash, ulong discordUserId,
            string table_Name, int recordsToDelete)
        {
            return $"DELETE FROM {table_Name} WHERE ROWID IN (SELECT ROWID FROM {table_Name} " +
                $"WHERE discord_user_id = {discordUserId} AND hash = {hash} ORDER BY ROWID ASC LIMIT {recordsToDelete})";
        }

        private string CountRecordsOfUser(string table_name, ulong discordUserId, string urlId_Column, UrlDetails url)
        {
            return $"SELECT COUNT(*) AS count FROM {table_name} " +
                $"WHERE discord_user_id = {discordUserId} AND {urlId_Column} = '{url.UrlId}'";
        }

        private string CountRecordsOfUser(string table_name, ulong discordUserId, MediaDetails media)
        {
            return $"SELECT COUNT(*) AS count FROM {table_name} " +
                $"WHERE discord_user_id = {discordUserId} AND hash = {media.Hash}";
        }

        private string CountRecordsByUserAndHash(string table_name, ulong discordUserId, ulong hash)
        {
            return $"SELECT Count(*) FROM {table_name} " +
                $"WHERE discord_user_id = {discordUserId} AND hash = {hash}";
        }

        private string DeleteNRecordsFromTable(int recordsToDelete, string table_Name)
        {
            return $"DELETE FROM {table_Name} WHERE ROWID IN " +
                $"(SELECT ROWID FROM {table_Name} ORDER BY ROWID ASC LIMIT {recordsToDelete})";
        }

        private string AllTableRecordsCount()
        {
            //the SQL CAST is to fix a bug in Dapper where primative values are not properly casted 
            return "SELECT 'Discord_Users' AS Tables, CAST(COUNT(*) AS INTEGER) AS Records FROM Discord_Users " +
                "UNION ALL " +
                "SELECT 'Youtube_Links', CAST(COUNT(*) AS INTEGER) FROM Youtube_Links " +
                "UNION ALL " +
                "SELECT 'Twitter_Links', CAST(COUNT(*) AS INTEGER) FROM Twitter_Links " +
                "UNION ALL " +
                "SELECT 'Reddit_Links', CAST(COUNT(*) AS INTEGER) FROM Reddit_Links " +
                "UNION ALL " +
                "SELECT 'Images', CAST(COUNT(*) AS INTEGER) FROM Images " +
                "UNION ALL " +
                "SELECT 'Videos', CAST(COUNT(*) AS INTEGER) FROM Videos";
        }

        /*
         * youtube_links
         SELECT video_id, discord_message_link_id, timestamp_created FROM Youtube_Links
WH ERE discord_user_id = 243379806463459328 AND video_id = '-wtIMTCHWuI'
ORDER BY timestamp_created;

        reddit_links
        SELECT reddit_id, r_username, discord_message_link_id, timestamp_created FROM Reddit_Links
WHERE discord_user_id = 243379806463459328 AND reddit_id = ''
ORDER BY timestamp_created;
         

        --SELECT COUNT(DISTINCT video_id) FROM Youtube_Links;
-- CREATE TRIGGER delete_oldest_record_from_user_on_youtube 
--    AFTER INSERT 
--    ON Youtube_Links
-- BEGIN
--  DELETE FROM Youtube_Links
-- WHERE discord_user_id = OLD.user_id;
-- END;

-- DELETE FROM Youtube_Links WHERE discord_user_id = 7 IN (
-- SELECT discord_user_id FROM Youtube_Links ORDER BY timestamp_created ASC LIMIT 1)

SELECT video_id, timestamp_created FROM Youtube_Links 
WHERE discord_user_id = 452598298897809408 AND video_id = 'dQw4w9WgXcQ'
ORDER BY timestamp_created ASC LIMIT 1



        Trigger for an insert...

        DELETE FROM Youtube_Links
WHERE ROWID IN
(
SELECT ROWID FROM Youtube_Links 
WHERE discord_user_id = 366340012394020865 AND video_id = 'yZv2daTWRZU'
ORDER BY ROWID ASC LIMIT 1
)

                    /*
             SELECT 'Discord_Users' AS Tables, COUNT(*) AS Records FROM Discord_Users
UNION ALL
SELECT 'Youtube_Links', COUNT(*) FROM Youtube_Links
UNION ALL
SELECT 'Twitter_Links', COUNT(*) FROM Twitter_Links
UNION ALL
SELECT 'Reddit_Links', COUNT(*) FROM Reddit_Links
UNION ALL
SELECT 'Images', COUNT(*) FROM Images
UNION ALL
SELECT 'Videos', COUNT(*) FROM Videos
             */
    }
}
