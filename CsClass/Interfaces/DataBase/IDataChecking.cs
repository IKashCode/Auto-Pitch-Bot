using System;
using Telegram.Bot;

namespace NotLoveBot.Interface
{
    public interface IDataChecking
    {
        Task<bool> CheckingValidity(TelegramBotClient telegramBotClient, string data);
    }
}