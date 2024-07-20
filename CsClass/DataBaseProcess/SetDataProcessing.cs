using System;
using Microsoft.Data.Sqlite;

namespace NotLoveBot.DataBaseProcess
{
    class SetDataProcessing
    {
        private string _path = "Data Source=DataBaseSQLite/NotLoveBotDataBase.db";

        private SqliteCommand _sqliteCommand = new SqliteCommand();

        public async Task SetCreateRequest(string sqliteCommandText)
        {
            using (var connection = new SqliteConnection(_path))
            {
                await connection.OpenAsync();

                _sqliteCommand.Connection = connection;
                _sqliteCommand.CommandText = sqliteCommandText;
                
                await _sqliteCommand.ExecuteNonQueryAsync();
            }
        }

        public async Task<bool> ExistenceCheck(string tableName, string columnName, string data)
        {
            bool exists = false;

            using (var connection = new SqliteConnection(_path))
            {
                await connection.OpenAsync();

                _sqliteCommand.Connection = connection;
                _sqliteCommand.CommandText = $"SELECT EXISTS (SELECT 1 FROM {tableName} WHERE {columnName} = '{data}');";
                
                int count = Convert.ToInt32(_sqliteCommand.ExecuteScalar());
                if (count > 0)
                    exists = true;
            }

            return exists;
        }
    }
}