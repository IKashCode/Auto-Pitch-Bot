using System;
using NotLoveBot.Interface;
using Telegram.Bot;

namespace NotLoveBot.DataChecking
{
    public class TokenChecking : IDataChecking
    {
        public async Task<bool> CheckingValidity(TelegramBotClient telegramBotClient, string token)
        {
            try
            {
                TelegramBotClient connectTelegramBotClient = new TelegramBotClient(token);
                
                // Проверка на наличия данных по токену.
                var botInformation = await connectTelegramBotClient.GetMeAsync();
                if (botInformation != null)
                    return true;
            }
            catch(Exception exeption)
            {
                Console.WriteLine(exeption);
                return false;
            }

            return false;
        }
    }
}