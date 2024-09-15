using System;
using Microsoft.Data.Sqlite;

namespace NotLoveBot.DataBaseProcess
{
    class SetDataProcessing
    {
        private string _path = "Data Source=DataBaseSQLite/NotLoveBotDataBase.db";

        public async Task SetCreateRequest(string sqliteCommandText, object[,] allData, string data)
        {
            using (var connection = new SqliteConnection(_path))
            {
                await connection.OpenAsync();

                using (var sqliteCommand = new SqliteCommand(sqliteCommandText, connection))
                {
                    
                    if (data == null)
                    {
                        if (allData != null && sqliteCommandText.Contains("@"))
                        {
                            for (int index = 0; index < allData.GetLength(1); index++)
                            { sqliteCommand.Parameters.AddWithValue($"@{allData[1, index]}", allData[0, index]); }
                        }
                    }
                    else
                    {
                        if (sqliteCommandText.Contains("@"))
                            sqliteCommand.Parameters.AddWithValue($"@{data}", data);
                    }

                    await sqliteCommand.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<bool> ExistenceCheck(string tableName, string columnName, string data)
        {
            bool exists = false;

            using (var connection = new SqliteConnection(_path))
            {
                await connection.OpenAsync();

                using (var sqliteCommand = new SqliteCommand(_path, connection))
                {
                    sqliteCommand.Connection = connection;
                    sqliteCommand.CommandText = $"SELECT EXISTS (SELECT 1 FROM {tableName} WHERE {columnName} = @{data});";

                    sqliteCommand.Parameters.AddWithValue($"@{data}", data);

                    int count = Convert.ToInt32(await sqliteCommand.ExecuteScalarAsync());
                    if (count > 0)
                        exists = true;
                }
            }

            return exists;
        }
    }
}