using System;
using Microsoft.Data.Sqlite;
using NotLoveBot.Interface;
using NotLoveBot.Models;

namespace NotLoveBot.DataBaseProcess
{
    class GetAdministratorsData : IGetDataProcessing<AdministrationModel>
    {
        private string _path = "Data Source=DataBaseSQLite/NotLoveBotDataBase.db";
        private SqliteCommand _sqliteCommand = new SqliteCommand();

        public async Task<List<AdministrationModel>> GetList(string data, string botName)
        {
            List<AdministrationModel> administrationModels = new List<AdministrationModel>();

            using (var connection = new SqliteConnection(_path))
            {
                await connection.OpenAsync();

                _sqliteCommand.Connection = connection;
                _sqliteCommand.CommandText = $"SELECT * FROM Administrators_{botName}";

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