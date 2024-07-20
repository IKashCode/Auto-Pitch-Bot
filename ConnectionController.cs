using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace NotLoveBot.Program
{
    public class ConnectionController
    {
        
        private static TelegramBotClient? _telegramBotClient;
        public static string Token = "6820716903:AAEo7r-nGOLSJfNQfVZ-MMNERSvzEiq9a60";

        private static ConcurrentDictionary<string, TelegramBotClient> _telegramBotClients = new ConcurrentDictionary<string, TelegramBotClient>();

        public static async Task Main(string[] args)
        {
            _telegramBotClient = new TelegramBotClient(Token);
            
            _telegramBotClient.OnMessage += SuperBotOnMessageReceived;
            _telegramBotClient.OnMessageEdited += SuperBotOnMessageReceived;

            try{
                _telegramBotClient.StartReceiving();
                Console.WriteLine($"The system has been successfully launched, token: {Token}.");
            }
            catch (Exception exception){
                Console.WriteLine($"The system did not start successfully, exception: {exception}.");
            }
            
            Console.ReadLine();
            _telegramBotClient.StopReceiving();
        }

        private static async void SuperBotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            try
            {
                var message = messageEventArgs.Message;

                if (message.Text == "/start")
                    await _telegramBotClient.SendTextMessageAsync(message.Chat.Id, "lol");

                if (message.Text.StartsWith("/add"))
                {
                    var token = message.Text.Split(' ')[1];

                    if (!_telegramBotClients.ContainsKey(token))
                    {
                        var telegramBotClient = new TelegramBotClient(token);
                        _telegramBotClients[token] = telegramBotClient;

                        telegramBotClient.OnMessage += (sender, messageEventArgs) => NotLoveBot.Program.Program.BotOnMessageReceived(sender, messageEventArgs, token);
                        telegramBotClient.OnMessageEdited  += (sender, messageEventArgs) => NotLoveBot.Program.Program.BotOnMessageReceived(sender, messageEventArgs, token);
                        
                        telegramBotClient.StartReceiving();

                        await _telegramBotClient.SendTextMessageAsync(message.Chat.Id, "бот готов");
                    }
                    else
                        await _telegramBotClient.SendTextMessageAsync(message.Chat.Id, "ошибка");
                }
            }
            catch (Exception exception) { Console.WriteLine(exception); }
        }
    }
}