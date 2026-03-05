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
        /// 
        /// </summary>
        /// <param name="recordsToDelete"></param>
        /// <returns>Number of rows deleted. If parameter is negative, returns -1 and no records are deleted</returns>
        internal int DeleteFrom_Table(WebDomain tableDomain, int recordsToDelete)
        {
            switch (tableDomain)
            {
                case WebDomain.YouTube:
                    return DeleteFrom("Youtube_Links", recordsToDelete);
                case WebDomain.Twitter:
                    return DeleteFrom("Twitter_Links", recordsToDelete);
                case WebDomain.Reddit:
                    return DeleteFrom("Reddit_Links", recordsToDelete);
                default:
                    return -1;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="recordsToDelete"></param>
        /// <returns>Number of rows deleted. If parameter is negative, returns -1 and no records are deleted</returns>
        internal int DeleteFrom_Table(Media tableMedia, int recordsToDelete)
        {
            switch (tableMedia)
            {
                case Media.Image:
                    return DeleteFrom("Images", recordsToDelete);
                case Media.Video:
                    return DeleteFrom("Videos", recordsToDelete);
                default:
                    return -1;
            }
        }

        private int DeleteFrom(string table_name, int recordsToDelete)
        {
            if (recordsToDelete < 0)
                return -1;
            using (IDbConnection connection = new SQLiteConnection(_connectionString))
            {
                string sql = DeleteNRecordsFromTable(recordsToDelete, table_name);
                return connection.Execute(sql, null, null, _DBTimeoutSec);
            }
        }

        /// <summary>
        /// Use when I manually add a variant to the filter list
        /// </summary>
        /// <param name="diffHashVariant"></param>
        /// <returns>Number of rows deleted</returns>
        internal static int DeleteAbominationVariantFrom_Images(ulong diffHashVariant)
        {
            using (IDbConnection connection = new SQLiteConnection(_connectionString))
            {
                string sql = $"DELETE FROM Images WHERE hash = {diffHashVariant}";
                return connection.Execute(sql, null, null, _DBTimeoutSec);
            }
        }

        /// <summary>
        /// Use for manually removing a specfic regex id from a database table
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="urlId"></param>
        /// <returns></returns>
        internal int DeleteUrlIdFrom_Table(WebDomain domain, string urlId)
        {
            switch (domain)
            {
                case WebDomain.YouTube:
                    return DeleteUrlIdFrom("Youtube_links", "video_id", urlId);
                case WebDomain.Twitter:
                    return DeleteUrlIdFrom("Twitter_Links", "tweet_id", urlId);
                case WebDomain.Reddit:
                    return DeleteUrlIdFrom("Reddit_Links", "reddit_id", urlId);
                default:
                    return -1;
            }
        }

        private int DeleteUrlIdFrom(string table_name, string id_column_name, string urlId)
        {
            using (IDbConnection connection = new SQLiteConnection(_connectionString))
            {
                string sql = $"DELETE FROM {table_name} WHERE {id_column_name} = '{urlId}'";
                return connection.Execute(sql, null, null, _DBTimeoutSec);
            }
        }

        /// <summary>
        /// Use for manually deleted a specific hash from a database table
        /// </summary>
        /// <param name="tableMedia"></param>
        /// <param name="hash"></param>
        /// <returns></returns>
        internal int DeleteHashFrom_Table(Media tableMedia, ulong hash)
        {
            switch (tableMedia)
            {
                case Media.Image:
                    return DeleteHashFrom("Images", hash);
                case Media.Video:
                    return DeleteHashFrom("Videos", hash);
                default:
                    return -1;
            }
        }

        private int DeleteHashFrom(string table_name, ulong hash)
        {
            using (IDbConnection connection = new SQLiteConnection(_connectionString))
            {
                string sql = $"DELETE FROM {table_name} WHERE hash = {hash}";
                return connection.Execute(sql, null, null, _DBTimeoutSec);
            }
        }
    }
}
