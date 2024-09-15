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
    public class MessageTextController
    {
        // Класс для управления сообщениями с меню.
        private static KeyboardMessagesController _keyboardMessagesController = new KeyboardMessagesController();
        
        // Классы для работы с базами данных.
        private IGetDataProcessing<ConnectionBotModel> _getBotsData = new GetBotsData();
        private SetDataProcessing _setDataProcessing = new SetDataProcessing();

        private static Dictionary<long, EventHandler<CallbackQueryEventArgs>> _usersCallbacks = new Dictionary<long, EventHandler<CallbackQueryEventArgs>>();

        // Значения для проверки на длину разных типов сообщений.
        private int maximumLengthStart = 350, maximumLengthReplyMessage = 200;
        
        public async Task SetMessageText(TelegramBotClient telegramBotClient, Message message, Message editMessage, string administratorStatus, string botName)
        {
            try {
            List<ConnectionBotModel> connectionBotModels = await _getBotsData.GetList(botName, "botName");
            var connectionBotModel = connectionBotModels.FirstOrDefault();

            InlineKeyboardMarkup textInlineKeyboardMarkup = new InlineKeyboardMarkup(new[] {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✏️ → ответы"),
                    InlineKeyboardButton.WithCallbackData("✏️ → /start")
                },
                new[] { InlineKeyboardButton.WithCallbackData("← Назад") }
            });

            Message textControllerPanel;
            if (editMessage == null)
            {
                textControllerPanel = await telegramBotClient.SendTextMessageAsync(message.Chat.Id,
                $"✉️ *Заданный текст ответов:*\n\n{connectionBotModel.ReplyMessageText}\n\n*🚀 Описание команды «/start»:*\n\n{connectionBotModel.StartMessageText}",
                replyMarkup: textInlineKeyboardMarkup, parseMode: ParseMode.Markdown);
            }
            else
            {
                textControllerPanel = await telegramBotClient.EditMessageTextAsync(editMessage.Chat.Id, editMessage.MessageId,
                $"✉️ *Заданный текст ответов:*\n\n{connectionBotModel.ReplyMessageText}\n\n*🚀 Описание команды «/start»:*\n\n{connectionBotModel.StartMessageText}",
                replyMarkup: textInlineKeyboardMarkup, parseMode: ParseMode.Markdown);
            }

            // Добавление в общий список сообщений с меню.
            await _keyboardMessagesController.AddKeyboardMessages(textControllerPanel);

            EventHandler<CallbackQueryEventArgs> BotOnButtonClick = async (sender, callbackQueryEventArgs) =>
            {
                var callbackQueryMessage = callbackQueryEventArgs.CallbackQuery;
                var telegramBotClient = (TelegramBotClient)sender;

                if (callbackQueryMessage.From.Id != message.From.Id)
                    return;

                if (callbackQueryMessage.Data == "✏️ → ответы" || callbackQueryMessage.Data == "✏️ → /start")
                {
                    Message indicateDateMessage;
                    if (textControllerPanel == null)
                    {
                        indicateDateMessage = await telegramBotClient.SendTextMessageAsync(textControllerPanel.Chat.Id,
                        "💬 *Отправьте новый текст, чтобы заменить текущий.*",
                        parseMode: ParseMode.Markdown);
                    }
                    else
                    {
                        indicateDateMessage = await telegramBotClient.EditMessageTextAsync(textControllerPanel.Chat.Id, textControllerPanel.MessageId,
                        "💬 *Отправьте новый текст, чтобы заменить текущий.*",
                        parseMode: ParseMode.Markdown);
                    }

                    telegramBotClient.OnMessage += BotOnMessageReceived;
                    async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
                    {
                        try {
                        Message replyMessage = messageEventArgs.Message;
                        string dataMessage;

                        // Проверка формата данных.
                        if (replyMessage.Type != MessageType.Text)
                        {
                            await telegramBotClient.SendTextMessageAsync(message.Chat.Id, $"⚠️ *Формат данных некорректен!* Пожалуйста, отправьте данные в виде текста.", parseMode: ParseMode.Markdown);
                            telegramBotClient.OnMessage -= BotOnMessageReceived;
                            telegramBotClient.OnMessage += BotOnMessageReceived;
                            return;
                        }
                        else
                            dataMessage = replyMessage.Text;

                        if (replyMessage.From.Id != message.From.Id)
                            return;
                        // Проверка на наличие команд.
                        foreach (string command in ConnectionController.commands)
                        {
                            if (dataMessage == command)
                            {
                                telegramBotClient.OnMessage -= BotOnMessageReceived;
                                return;
                            }
                        }

                        string tableName = null;
                        int maximumLength = 0;
                        switch (callbackQueryMessage.Data)
                        {
                            case "✏️ → ответы":
                                tableName = "replyMessageText";
                                maximumLength = maximumLengthReplyMessage;
                                break;
                            case "✏️ → /start":
                                tableName = "startMessageText";
                                maximumLength = maximumLengthStart;
                                break;
                        }

                        if (dataMessage.Length < maximumLength)
                        {
                            object[,] textMessageData = { { dataMessage, botName }, { "messageText", "botName" } };
                            await _setDataProcessing.SetCreateRequest($"UPDATE Bots SET {tableName} = @messageText WHERE botName = @botName;", textMessageData, null);

                            // Обновление значения в списке данных telegram bot.
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
                                FilterStatus = connectionBotModel.FilterStatus,
                            };

                            // Удаляем прошлое значение и присваиваем новое.
                            ConnectionController.TelegramBotClients.TryRemove(connectionBotModel.Token, out _);
                            ConnectionController.TelegramBotClients.TryAdd(updateConnectionBotModel.Token, updateConnectionBotModel);

                            await telegramBotClient.SendTextMessageAsync(message.Chat.Id, "🔄 *Текст обновлён!* В дальнейшем все сообщения будут отправляться с новым содержанием.", parseMode: ParseMode.Markdown);
                            await SetMessageText(telegramBotClient, message, null, administratorStatus, botName);
                            telegramBotClient.OnMessage -= BotOnMessageReceived;
                        }
                        else
                        {
                            await telegramBotClient.SendTextMessageAsync(message.Chat.Id, $"⚠️ *Текст не должен превышать* `{maximumLength.ToString()}` *символов!* Пожалуйста, отправьте новый текст.", parseMode: ParseMode.Markdown);
                            telegramBotClient.OnMessage -= BotOnMessageReceived;
                            telegramBotClient.OnMessage += BotOnMessageReceived;
                        }
                    } catch (Exception exception) { Console.WriteLine(exception); }
                }
                }
                else
                {
                    AdministratorMenu administratorMenu = new AdministratorMenu();
                    await administratorMenu.GetAdministratorMenu(telegramBotClient, message, textControllerPanel, administratorStatus, botName);
                    telegramBotClient.OnCallbackQuery -= _usersCallbacks[message.From.Id];
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
        } catch (Exception exception) { Console.WriteLine(exception); }
        }
    }
}