using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace ShrekBot.Modules.Database
{
    internal partial class SwampDB
    {
        private string _connectionString;
        private const int _MaxDuplicates = 5;
        public const int _DBTimeoutSec = 6;
        internal SwampDB()
        {
            const string dbName = "ShrekbotDB.db";
            _connectionString = $"Data Source={dbName};Version=3;";
            if (!File.Exists(dbName))
            {
                SQLiteConnection.CreateFile(dbName); //uncomment when I need to create a new database                
                CreateAndSetUp();
            }  
        }

        private void CreateAndSetUp()
        {
            string[] sqlFiles = Directory.GetFiles(@".\sql files", "*.sql");
            SQLiteConnection connection = new SQLiteConnection(_connectionString);
            connection.Open();
            for (int i = 0; i < sqlFiles.Length; i++)
            {
                string script = File.ReadAllText(sqlFiles[i]);
                SQLiteCommand command = new SQLiteCommand(script, connection);
                command.ExecuteNonQuery();

            }
            connection.Close();
        }

        /// <summary>
        /// A List of user ids from all users in Drake's Server
        /// </summary>
        /// <returns></returns>
        internal List<ulong> Friends()
        {
            using (IDbConnection connection = new SQLiteConnection(_connectionString))
            {
                string sql = "SELECT user_id FROM Discord_Users";
                return connection.Query<ulong>(sql, null, null, true, _DBTimeoutSec).ToList();
            }
        }

        /// <summary>
        /// Adds new member of Drake's Server into the database
        /// </summary>
        /// <remarks>
        /// This method should only be called during the UserJoined event
        /// </remarks>
        /// <param name="discordUserId"></param>
        /// <param name="discordName"></param>
        /// <returns></returns>
        internal int AddNewFriend(ulong discordUserId, string discordName)
        {
            using (IDbConnection connection = new SQLiteConnection(_connectionString))
            {
                string sql = $"INSERT INTO Discord_Users VALUES ({discordUserId}, '{discordName}')";
                return connection.Execute(sql, null, null, _DBTimeoutSec);
            }
        }

        /// <summary>
        /// Delete all records from user in corresponding databases.
        /// </summary>
        /// <remarks>
        /// This method should only be called during the GuildUserLeave or UserBanned event.
        /// <para>In addition, a Delete Trigger is called on DB side where all records of the user is deleted from all tables</para>
        /// </remarks>
        /// <param name="discordUserId"></param>
        /// <returns></returns>
        internal int RemoveFriend(ulong discordUserId)
        {
            using (IDbConnection connection = new SQLiteConnection(_connectionString))
            {
                string sql = $"DELETE FROM Discord_Users WHERE user_id = {discordUserId}";
                return connection.Execute(sql, null, null, _DBTimeoutSec);
            }
        }    
    }
}
