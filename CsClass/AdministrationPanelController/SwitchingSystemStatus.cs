using System;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using NotLoveBot.DataBaseProcess;
using NotLoveBot.Program;
using NotLoveBot.Models;

namespace NotLoveBot.AdministrationPanelController
{
    public class SwitchingSystemStatus
    {
        // Класс для работы с базой данных.
        private SetDataProcessing _setDataProcessing = new SetDataProcessing();

        private static Dictionary<long, EventHandler<CallbackQueryEventArgs>> _usersCallbacks = new Dictionary<long, EventHandler<CallbackQueryEventArgs>>();

        public async Task StatusController(TelegramBotClient telegramBotClient, Message message, Message editMessage, bool statusSystem, string functionName, string administratorStatus, string botName)
        {
            try {
            // Создание панели переключения и ее текста.
            string actionName = "⏹️ Выключить",
            statusText = "в данный момент *включен*. Вы можете выключить его, используя кнопки ниже.",
            resultMessage = $"❌ *{functionName} выключен!*";

            if (statusSystem == false)
            {
                actionName = "▶️ Включить";
                statusText = "в данный момент *выключен*. Вы можете включить его, используя кнопки ниже.";
            }

            InlineKeyboardMarkup inlineKeyboardMarkup = new InlineKeyboardMarkup(new[] {
                InlineKeyboardButton.WithCallbackData(actionName),
                InlineKeyboardButton.WithCallbackData("← Назад")
            });

            Message statusControllerPanel = await telegramBotClient.EditMessageTextAsync(editMessage.Chat.Id, editMessage.MessageId, $"⇩ *{functionName}* {statusText}", replyMarkup: inlineKeyboardMarkup, parseMode: ParseMode.Markdown);

            EventHandler<CallbackQueryEventArgs> BotOnButtonClick = async (sender, callbackQueryEventArgs) =>
            {
                var callbackQueryMessage = callbackQueryEventArgs.CallbackQuery;
                var telegramBotClient = (TelegramBotClient)sender;
                
                if (callbackQueryMessage.From.Id != message.From.Id)
                    return;

                AdministratorMenu administratorMenu = new AdministratorMenu();
                // Измненение статуса параметра.
                if (callbackQueryMessage.Data == "⏹️ Выключить" || callbackQueryMessage.Data == "▶️ Включить")
                {
                    statusSystem = !statusSystem;

                    if (statusSystem == true)
                        resultMessage = $"✅ *{functionName} включен!*";

                    // Сообщение о статусе функционала после переключения.
                    statusControllerPanel = await telegramBotClient.EditMessageTextAsync(statusControllerPanel.Chat.Id, statusControllerPanel.MessageId, resultMessage, parseMode: ParseMode.Markdown);
                    await Task.Delay(1000);

                    await administratorMenu.GetAdministratorMenu(telegramBotClient, message, statusControllerPanel, administratorStatus, botName);

                    // Создаем массив для записи в базу данных.
                    int statusSystemIntValue = statusSystem ? 1 : 0;
                    object[,] data = { { statusSystemIntValue, botName }, { "status", "botName" } };
                    // Запись в базу данных и смена значения.
                    await _setDataProcessing.SetCreateRequest("UPDATE Bots SET FilterStatus = @status WHERE botName = @botName;", data, null);

                    // Обновляем значение в списке.
                    var connectionBotModel = ConnectionController.TelegramBotClients.Values.FirstOrDefault(bot => bot.BotName == botName);
                    var updateConnectionBotModel = new ConnectionBotModel
                    {
                        BotName = connectionBotModel.BotName,
                        Token = connectionBotModel.Token,
                        ChannelName = connectionBotModel.ChannelName,
                        ChannelId = connectionBotModel.ChannelId,
                        UserId = connectionBotModel.UserId,
                        BotClient = connectionBotModel.BotClient,
                        Delay = connectionBotModel.Delay,
                        ReplyMessageText = connectionBotModel.ReplyMessageText,
                        StartMessageText = connectionBotModel.StartMessageText,
                        FilterStatus = statusSystemIntValue,
                    };

                    // Удаляем прошлое значение и присваиваем новое.
                    ConnectionController.TelegramBotClients.TryRemove(connectionBotModel.Token, out _);
                    ConnectionController.TelegramBotClients.TryAdd(updateConnectionBotModel.Token, updateConnectionBotModel);

                    telegramBotClient.OnCallbackQuery -= _usersCallbacks[message.From.Id];
                }
                else
                {
                    await administratorMenu.GetAdministratorMenu(telegramBotClient, message, statusControllerPanel, administratorStatus, botName);
                    telegramBotClient.OnCallbackQuery -= _usersCallbacks[message.From.Id];
                    _usersCallbacks.Remove(message.From.Id);
                }
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
            }
            catch(Exception exception) { Console.WriteLine(exception); }
        }
    }
}