using System;
using Microsoft.Data.Sqlite;
using NotLoveBot.Models;
using Telegram.Bot.Types;

namespace NotLoveBot.DataBaseProcess
{
    class GetDataProcessing
    {
        // Класс является временным решением для получения различных данных!

        private string _path = "Data Source=DataBaseSQLite/NotLoveBotDataBase.db";

        private SqliteCommand _sqliteCommand = new SqliteCommand();

        public async Task<List<SenderModel>> GetListSenders(string date)
        {
            List<SenderModel> senderModels = new List<SenderModel>();

            using (var connection = new SqliteConnection(_path))
            {
                await connection.OpenAsync();

                _sqliteCommand.Connection = connection;
                _sqliteCommand.CommandText = $"SELECT * FROM UserMessageHistory_test_394832 WHERE date = '{date}'";

                using (var executeReader = await _sqliteCommand.ExecuteReaderAsync())
                {
                    while(await executeReader.ReadAsync())
                    {
                        var senderModel = new SenderModel
                        {
                            User = executeReader.GetString(1),
                            Id = executeReader.GetString(3),
                            Type = executeReader.GetString(5),
                            Message = executeReader.GetString(2),
                            Status = executeReader.GetString(6)
                        };

                        senderModels.Add(senderModel);
                    }
                }
                
                await _sqliteCommand.ExecuteNonQueryAsync();
                return senderModels;
            }
        }

        public async Task<List<AdministrationModel>> GetListAdministrators()
        {
            List<AdministrationModel> administrationModels = new List<AdministrationModel>();

            using (var connection = new SqliteConnection(_path))
            {
                await connection.OpenAsync();

                _sqliteCommand.Connection = connection;
                _sqliteCommand.CommandText = $"SELECT * FROM Administrators_test_394832";

                using (var executeReader = await _sqliteCommand.ExecuteReaderAsync())
                {
                    while(await executeReader.ReadAsync())
                    {
                        var administrationModel = new AdministrationModel
                        {
                            Id = executeReader.GetString(0),
                            Status = executeReader.GetString(1),
                            Name = executeReader.GetString(2)
                        };

                        administrationModels.Add(administrationModel);
                    }
                }
                
                await _sqliteCommand.ExecuteNonQueryAsync();
                return administrationModels;
            }
        }
    }
}