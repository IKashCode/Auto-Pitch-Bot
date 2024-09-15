using System;
using Microsoft.Data.Sqlite;

namespace NotLoveBot.DataBaseProcess
{
    public class GetBotNameByCode
    {
        private string _path = "Data Source=DataBaseSQLite/NotLoveBotDataBase.db";
        private SqliteCommand _sqliteCommand = new SqliteCommand();
        public async Task<string> GetBotName(string administratorCode)
        {
            using (var connection = new SqliteConnection(_path))
            {
                await connection.OpenAsync();

                string sqliteCommandText = $"SELECT botName FROM AdministratorCodes WHERE code = @code";

                using (var sqliteCommand = new SqliteCommand(sqliteCommandText, connection))
                {
                    sqliteCommand.Parameters.AddWithValue("@code", administratorCode);

                    object sqliteCommandResult = await sqliteCommand.ExecuteScalarAsync();
                    string botName = null;
                    
                    if (sqliteCommandResult != null) 
                        botName = sqliteCommandResult.ToString();

                        return botName;
                }
            }
        }
    }
}