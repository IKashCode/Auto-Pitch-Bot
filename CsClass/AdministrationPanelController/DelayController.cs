using System;
using NotLoveBot.DataBaseProcess;
using NotLoveBot.Interface;
using NotLoveBot.Models;
using NotLoveBot.Program;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace NotLoveBot.AdministrationPanelController
{
    public class DelayController
    {
        // Классы для работы с базами данных.
        private IGetDataProcessing<ConnectionBotModel> _getBotsData = new GetBotsData();
        private SetDataProcessing _setDataProcessing = new SetDataProcessing();

        private static Dictionary<long, EventHandler<CallbackQueryEventArgs>> _usersCallbacks = new Dictionary<long, EventHandler<CallbackQueryEventArgs>>();
        private string[] _delays = { "❌", "15 S", "30 S", "60 S", "90 S" };
        
        public async Task SetDelay(TelegramBotClient telegramBotClient, Message message, Message editMessage, string administratorStatus, string botName)
        {
            try {
            // Получение данных telegram bot из базы данных.
            List<ConnectionBotModel> connectionBotModels = await _getBotsData.GetList(botName, "botName");
            var connectionBotModel = connectionBotModels.FirstOrDefault();

            // Создание клавиатуры для выбора задержки.
            List<InlineKeyboardButton> inlineKeyboardButtons = new List<InlineKeyboardButton>();
            for (int index = 0; index < _delays.Length; index++)
            {
                string? delayButtonText = _delays[index];

                // Поиск выбранного параметра.
                if (_delays[index] == $"{connectionBotModel.Delay.ToString()} S")
                    delayButtonText = $"✅ {delayButtonText}";
                else
                {
                    // Если выбрано значение 0, то задаем особый вид кнопке.
                    if (connectionBotModel.Delay == 0 && _delays[index] == "❌")
                        delayButtonText = $"✅ Нет";
                }

                InlineKeyboardButton inlineKeyboardButton = InlineKeyboardButton.WithCallbackData(delayButtonText);
                inlineKeyboardButtons.Add(inlineKeyboardButton);
            }

            InlineKeyboardMarkup delayInlineKeyboardMarkup = new InlineKeyboardMarkup( new[] {
                inlineKeyboardButtons.ToArray(),
                new[] { InlineKeyboardButton.WithCallbackData("← Назад") }
            });

            Message delayControllerPanel = await telegramBotClient.EditMessageTextAsync(editMessage.Chat.Id, editMessage.MessageId,
                "⏱️ *Выберите задержку для отправки сообщений в канал:*",
                replyMarkup: delayInlineKeyboardMarkup, parseMode: ParseMode.Markdown);

            EventHandler<CallbackQueryEventArgs> BotOnButtonClick = async (sender, callbackQueryEventArgs) =>
            {
                try {
                var callbackQueryMessage = callbackQueryEventArgs.CallbackQuery;
                var telegramBotClient = (TelegramBotClient)sender;

                if (callbackQueryMessage.From.Id != message.From.Id)
                    return;

                switch (callbackQueryMessage.Data)
                {
                    case "← Назад":
                        AdministratorMenu administratorMenu = new AdministratorMenu();
                        await administratorMenu.GetAdministratorMenu(telegramBotClient, message, delayControllerPanel, administratorStatus, botName);
                        telegramBotClient.OnCallbackQuery -= _usersCallbacks[message.From.Id];
                        _usersCallbacks.Remove(message.From.Id);
                        break;
                    default:
                        object[,] data = { { 0, botName }, { "delay", "botName" } };
                        int delay = 0;

                        if (callbackQueryMessage.Data != "❌" && !callbackQueryMessage.Data.Contains("✅"))
                        {
                            string delayString = callbackQueryMessage.Data.TrimEnd('S', ' ');
                            Console.WriteLine(delayString);
                            delay = Convert.ToInt32(delayString);
                            data[0,0] = delay;
                        }

                        // Запись в базу данных, если значение не является выбранным.
                        if (!callbackQueryMessage.Data.Contains("✅"))
                        {
                            // Запись в базу данных.
                            await _setDataProcessing.SetCreateRequest("UPDATE Bots SET Delay = @delay WHERE botName = @botName;", data, null);

                            // Перезаписываем список для быстрого доступа в других частях кода.
                            var connectionBotModel = ConnectionController.TelegramBotClients.Values.FirstOrDefault(bot => bot.BotName == botName);
                            var updateConnectionBotModel = new ConnectionBotModel
                            {
                                BotName = connectionBotModel.BotName,
                                Token = connectionBotModel.Token,
                                ChannelName = connectionBotModel.ChannelName,
                                ChannelId = connectionBotModel.ChannelId,
                                UserId = connectionBotModel.UserId,
                                BotClient = connectionBotModel.BotClient,
                                Delay = delay,
                                ReplyMessageText = connectionBotModel.ReplyMessageText,
                                StartMessageText = connectionBotModel.StartMessageText,
                                FilterStatus = connectionBotModel.FilterStatus,
                            };

                            // Удаляем прошлое значение и присваиваем новое.
                            ConnectionController.TelegramBotClients.TryRemove(connectionBotModel.Token, out _);
                            ConnectionController.TelegramBotClients.TryAdd(updateConnectionBotModel.Token, updateConnectionBotModel);

                            await SetDelay(telegramBotClient, message, delayControllerPanel, administratorStatus, botName);
                        }
                        break;
                }
                
                if (callbackQueryMessage.From.Id != message.From.Id)
                    return;
                    
                } catch(Exception exception) { Console.WriteLine(exception); }
            };

            // Если обработчик с ID пользователя уже создан, то он удаляется.
            if (_usersCallbacks.ContainsKey(message.From.Id))
            {
                telegramBotClient.OnCallbackQuery -= _usersCallbacks[message.From.Id];
                _usersCallbacks[message.From.Id] = BotOnButtonClick;
            }
            else
                _usersCallbacks.Add(message.From.Id, BotOnButtonClick);

            telegramBotClient.OnCallbackQuery += BotOnButtonClick;
            } catch(Exception exception) { Console.WriteLine(exception); }
        }
    }
}