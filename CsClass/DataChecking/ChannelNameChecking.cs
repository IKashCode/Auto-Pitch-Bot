using System;
using NotLoveBot.Interface;
using Telegram.Bot;

namespace NotLoveBot.DataChecking
{
    public class ChennelNameChecking : IDataChecking
    {
        public async Task<bool> CheckingValidity(TelegramBotClient telegramBotClient, string channelName)
        {
            try
            {
                var channelInformation = await telegramBotClient.GetChatAsync(channelName);
                if (await telegramBotClient.GetChatAsync(channelInformation) != null)
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