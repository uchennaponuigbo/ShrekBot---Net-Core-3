using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShrekBot.Modules.Database
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
        internal int DeleteAbominationVariantFrom_Images(ulong diffHashVariant)
        {
            using (IDbConnection connection = new SQLiteConnection(_connectionString))
            {
                string sql = $"DELETE FROM Images WHERE hash = {diffHashVariant}";
                return connection.Execute(sql, null, null, _DBTimeoutSec);
            }
        }
    }
}
