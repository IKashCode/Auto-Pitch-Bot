using System;
using Microsoft.Data.Sqlite;
using NotLoveBot.Interface;
using NotLoveBot.Models;
using Telegram.Bot;

namespace NotLoveBot.DataBaseProcess
{
    class GetBotsData : IGetDataProcessing<ConnectionBotModel>
    {
        private string _path = "Data Source=DataBaseSQLite/NotLoveBotDataBase.db";

        public async Task<List<ConnectionBotModel>> GetList(string data, string tableName)
        {
            List<ConnectionBotModel> connectionBotModels = new List<ConnectionBotModel>();

            using (var connection = new SqliteConnection(_path))
            {
                await connection.OpenAsync();

                string sqliteCommandText = $"SELECT * FROM Bots";
                if (data != null)
                    sqliteCommandText = $"SELECT * FROM Bots WHERE {tableName} = @data";

                using (var sqliteCommand = new SqliteCommand(sqliteCommandText, connection))
                {
                    sqliteCommand.Parameters.AddWithValue("@data", data);

                    using (var executeReader = await sqliteCommand.ExecuteReaderAsync())
                    {
                        while(await executeReader.ReadAsync())
                        {
                            var connectionBotModel = new ConnectionBotModel
                            {
                                BotName = executeReader.GetString(0),
                                Token = executeReader.GetString(1),
                                ChannelName = executeReader.GetString(2),
                                ChannelId = executeReader.GetString(3),
                                UserId = executeReader.GetString(4),
                                BotClient = new TelegramBotClient(executeReader.GetString(1)),
                                Delay = executeReader.GetInt32(5),
                                ReplyMessageText = executeReader.GetString(6),
                                FilterStatus = executeReader.GetInt32(7),
                                StartMessageText = executeReader.GetString(8)
                            };

                            connectionBotModels.Add(connectionBotModel);
                        }
                    }
                }

                return connectionBotModels;
            }
        }
    }
}