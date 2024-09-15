using System;
using Microsoft.Data.Sqlite;
using NotLoveBot.Interface;
using NotLoveBot.Models;
using Telegram.Bot.Types;

namespace NotLoveBot.DataBaseProcess
{
    class GetSendersData : IGetDataProcessing<SenderModel>
    {
        private string _path = "Data Source=DataBaseSQLite/NotLoveBotDataBase.db";

        public async Task<List<SenderModel>> GetList(string date, string botName)
        {
            List<SenderModel> senderModels = new List<SenderModel>();

            using (var connection = new SqliteConnection(_path))
            {
                await connection.OpenAsync();
                string sqliteCommandText = $"SELECT * FROM UserMessageHistory_{botName} WHERE date = @date";

                using (var sqliteCommand = new SqliteCommand(sqliteCommandText, connection))
                {
                    sqliteCommand.Parameters.AddWithValue("@date", date);

                    using (var executeReader = await sqliteCommand.ExecuteReaderAsync())
                    {
                        while(await executeReader.ReadAsync())
                        {
                            var senderModel = new SenderModel
                            {
                                User = executeReader.GetString(1),
                                Id = executeReader.GetString(3),
                                Type = executeReader.GetString(5),
                                Message = executeReader.GetString(2),
                                Status = executeReader.GetString(6),
                                FirstName = executeReader.GetString(7),
                                LastName = executeReader.GetString(8),
                                Time = executeReader.GetString(9),
                            };

                            senderModels.Add(senderModel);
                        }
                    }
                }
                
                return senderModels;
            }
        }
    }
}