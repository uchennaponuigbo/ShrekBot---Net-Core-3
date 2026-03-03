using Dapper;
using ShrekBot.Modules.Swamp.Helpers;
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
        //the original operation is pretty slow as expected, so I want to do it all on one connection rather than a sum of collections
        internal void SelectFromAndInsertInto_Table(Media tableMedia, ulong discordUserId,
            ref Dictionary<MediaDetails, byte> hashesToInsert, ref StringBuilder possibleReply)
        {
            switch (tableMedia)
            {
                case Media.Image:
                    SelectThenInsertIntoMediaTable(discordUserId, "Images", ref hashesToInsert, ref possibleReply); break;
                case Media.Video:
                    SelectThenInsertIntoMediaTable(discordUserId, "Videos", ref hashesToInsert, ref possibleReply); break;
            }

        }
        //do length checks outside of the function
        private void SelectThenInsertIntoMediaTable(ulong discordUserId, string table_name,
            ref Dictionary<MediaDetails, byte> hashesToInsert, ref StringBuilder possibleReply)
        {
            using (IDbConnection connection = new SQLiteConnection(_connectionString))
            {
                //all the hashes in the dictionary are the same, so it doesn't matter which one I grab, hence "first"
                //MediaDetails first = hashesToInsert.First().Key;
                //string sql_message_links = SelectFromTable(table_name, "hash", first.Hash);
                ////string sql_count = CountRecordsOfUser(table_name, discordUserId, hashesToInsert.)
                ///*
                // string sql = SelectFromTable(table_name, id_Column_name, hash, regexedId);
                //return connection.Query<string>(sql, null, null, true, _DBTimeoutSec).ToArray();
                // */
                //string[] duplicates = connection.Query<string>(sql_message_links, null, null, true, _DBTimeoutSec).ToArray();
                //if (duplicates.Length > 0)
                //{
                //    for (int i = 0; i < duplicates.Length; i++)
                //        possibleReply.AppendLine(URLCreate.DiscordTextChannel(duplicates[i]));
                //}

                //string sql_bulk_insert = InsertIntoValues_Bulk(hashesToInsert, discordUserId, table_name);
                ///*int rowsAffected =*/
                //connection.Execute(sql_bulk_insert);
                //foreach (var hashes in hashesToInsert)
                //{
                //    //what if I checked the count here, after doing everything else above just once?
                //    //it'll be an improvement since I'm limiting the number of queries
                //    string sql_count = CountRecordsByUserAndHash(table_name, discordUserId, hashes.Key.Hash);
                //    int rowsAffected = connection.Query(sql_Count);

                //    if (rowsAffected > 0)
                //    {
                //        int records = 0;
                //        var parameters = new
                //        {
                //            count = records
                //        };
                //        int connDuplicates = connection.Query<int>
                //            (CountRecordsOfUser(table_name, discordUserId, first), parameters, null, true, _DBTimeoutSec)
                //            .ToArray()[0];
                //        if (connDuplicates > _MaxDuplicates)
                //        {
                //            int recordsToDelete = connDuplicates - _MaxDuplicates;
                //            string deleteQuery = DeleteNRecordsFromTable_Bulk(first.Hash, discordUserId, table_name, recordsToDelete);//DeleteOneRecordFromTable(media, discordUserId, table_name);
                //            connection.Execute(deleteQuery, null, null, _DBTimeoutSec);
                //        }
                //    }
                //}
                //if (rowsAffected > 0)
                //{
                //    int records = 0;
                //    var parameters = new
                //    {
                //        count = records
                //    };
                //    int connDuplicates = connection.Query<int>
                //        (CountRecordsOfUser(table_name, discordUserId, first), parameters, null, true, _DBTimeoutSec)
                //        .ToArray()[0];
                //    if (connDuplicates > _MaxDuplicates)
                //    {
                //        int recordsToDelete = connDuplicates - _MaxDuplicates;
                //        string deleteQuery = DeleteNRecordsFromTable_Bulk(first.Hash, discordUserId, table_name, recordsToDelete);//DeleteOneRecordFromTable(media, discordUserId, table_name);
                //        connection.Execute(deleteQuery, null, null, _DBTimeoutSec);
                //    }
                //}


                foreach (KeyValuePair<MediaDetails, byte> mediaDetails in hashesToInsert)
                {
                    MediaDetails media = mediaDetails.Key;
                    string sql_message_links = SelectFromTable(table_name, "hash", media.Hash);
                    string[] duplicates = connection.Query<string>(sql_message_links, null, null, true, _DBTimeoutSec).ToArray();
                    if (duplicates.Length > 0)
                    {
                        for (int i = 0; i < duplicates.Length; i++)
                            possibleReply.AppendLine(URLCreate.DiscordTextChannel(duplicates[i]));
                    }

                    string sql_insert = InsertIntoValues(media, discordUserId, table_name);
                    int rowsAffected = connection.Execute(sql_insert);
                    if (rowsAffected > 0)
                    {
                        int records = 0;
                        var parameters = new
                        {
                            count = records
                        };
                        int connDuplicates = connection.Query<int>
                            (CountRecordsOfUser(table_name, discordUserId, media), parameters, null, true, _DBTimeoutSec)
                            .ToArray()[0];
                        if (connDuplicates > _MaxDuplicates)
                        {
                            string deleteQuery = DeleteOneRecordFromTable(media, discordUserId, table_name);
                            connection.Execute(deleteQuery, null, null, _DBTimeoutSec);
                        }
                    }
                }
            }
        }
        /*
         string[] duplicates = database.SelectFrom_Table(Media.Image, image.Key.Hash);
                if (duplicates.Length > 0)
                {
                    for (int i = 0; i < duplicates.Length; i++)
                        possibleReply.AppendLine(URLCreate.DiscordTextChannel(duplicates[i]));
                }
                database.InsertInto_Table(Media.Image, image.Key, socketMessage.Author.Id);
         */
    }
}
