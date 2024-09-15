using System;
using System.Diagnostics;
using System.Text;
using NotLoveBot.AdministrationPanelController;
using NotLoveBot.DataBaseProcess;
using NotLoveBot.CheckRules;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using NotLoveBot.Models;
using NotLoveBot.Interface;
using System.Linq;

namespace NotLoveBot.Program
{
    public class Program
    {
        // Класс для проверки сообщений.
        private static CheckMessage _checkMessage = new CheckMessage();

        // Класс для работы с базой данных.
        private static SetDataProcessing _dataProcessing = new SetDataProcessing();
        private static IGetDataProcessing<AdministrationModel> _getAdministratorsData = new GetAdministratorsData();
        private static IGetDataProcessing<ConnectionBotModel> _getBotsData = new GetBotsData();

        // Список администраторов, которые ожидают добавления в базу данных.
        public static List<string> PendingAdditionAdministrators = new List<string>();

        // Рекламная подпись внизу каждого сообщения.
        public static string Ads = "_Бот создан с помощью — @AutoPitchBot_";

        public static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs, string token)
        {
            try {
                TelegramBotClient telegramBotClient = new TelegramBotClient(token);
                Message anyMessage = messageEventArgs.Message;

                // Получение информации о bot из базы данных.
                List<ConnectionBotModel> connectionBotModels = await _getBotsData.GetList(token, "token");
                ConnectionBotModel connectionBotModel = connectionBotModels.FirstOrDefault();

                // Получение username telegram bot.
                string botName = connectionBotModel.BotName;
                string channelId = connectionBotModel.ChannelId;

                // Получение username и id.
                string username = "username отсутсвует";
                if (anyMessage.From.Username != null)
                    username = anyMessage.From.Username.ToString();

                string id = anyMessage.From.Id.ToString();

                string lastName = "LastName отсутсвует";
                if (anyMessage.From.LastName != null)
                    lastName = anyMessage.From.LastName.ToString();

                Console.WriteLine($"🛜 @{username}, - | {anyMessage.Text} |");

                // Проверка наличия администраторов в панели.
                bool exists = false;
                if (AdministratorMenu.ActiveAdministratorsId.ContainsKey(botName))
                {
                    List<string> activeAdministratorsId = AdministratorMenu.ActiveAdministratorsId[botName];
                    exists = activeAdministratorsId.Exists(item => item.Equals(id, StringComparison.OrdinalIgnoreCase));
                }

                // Если, код администратора не отправлен.
                switch (anyMessage.Text)
                {
                    case "/start":
                        await telegramBotClient.SendTextMessageAsync(anyMessage.Chat.Id,
                        $"{connectionBotModel.StartMessageText}\n\n{Ads}",
                        parseMode: ParseMode.Markdown);
                        return;
                        break;
                    default:
                        if (exists == true)
                            return;

                        Message forwardedMessages = messageEventArgs.Message;
                        string messageText = "текст отсутсвует";

                        // Проверка подписки на канал.
                        if (await CheckUserSubscription(forwardedMessages.From.Id, telegramBotClient, channelId) == false)
                        {
                            Chat channel = await telegramBotClient.GetChatAsync(channelId);
                            string channelName = channel.Username;
                            if (channelName != null)
                            {
                                InlineKeyboardMarkup channelInlineKeyboardMarkup = new InlineKeyboardMarkup(new[] {
                                    new[] { InlineKeyboardButton.WithUrl("Подписаться →", $"https://t.me/{channelName}") },
                                });
                            
                                await telegramBotClient.SendTextMessageAsync(forwardedMessages.Chat.Id,
                                "⚠️ Для использования функции пересылки сообщений в канал или группу требуется, *чтобы вы были на него подписаны!*",
                                replyMarkup: channelInlineKeyboardMarkup, parseMode: ParseMode.Markdown);
                            }
                            else
                                await telegramBotClient.SendTextMessageAsync(forwardedMessages.Chat.Id, "⚠️ Для использования функции пересылки сообщений в канал или группу требуется, *чтобы вы были на него подписаны!*", parseMode: ParseMode.Markdown);

                            return;
                        }

                        // Получение статуса проверки на ссылки из списка.
                        var botModel = ConnectionController.TelegramBotClients.Values.FirstOrDefault(bot => bot.Token == token);
                        bool enabledCheckMessages = Convert.ToBoolean(botModel.FilterStatus);

                        string messageType = "❌ неверный тип файла";
                        string textMessageStatus = "✅";

                        // Ответное сообщение на отправку, которое определяется проверкой.
                        string? checkMessageText = connectionBotModel.ReplyMessageText;

                        // Определение типа сообщения.
                        switch (messageEventArgs.Message.Type)
                        {
                            case MessageType.Text:
                                messageType = "📝 текстовое-сообщение";

                                // Проверка сообщение и ответ.
                                checkMessageText = await _checkMessage.CheckLink(checkMessageText, forwardedMessages.Text, enabledCheckMessages);
                                messageText = forwardedMessages.Text.ToString();

                                await telegramBotClient.SendTextMessageAsync(anyMessage.Chat.Id, checkMessageText, parseMode: ParseMode.Markdown);
                                await Task.Delay(TimeSpan.FromSeconds((double)botModel.Delay));
                                Console.WriteLine((double)botModel.Delay);

                                if (!checkMessageText.Contains("🚫"))
                                    await telegramBotClient.SendTextMessageAsync(channelId, forwardedMessages.Text, parseMode: ParseMode.Markdown);
                                else
                                    textMessageStatus = "🚫";

                                Console.WriteLine($"{forwardedMessages.From.Username} : {forwardedMessages.Text}");
                                break;
                            default:
                                MessageType[] messageTypes = { MessageType.Photo, MessageType.Video, MessageType.VideoNote, MessageType.Voice, MessageType.Audio };
                                bool rightType = false;
                                foreach (var type in messageTypes)
                                {
                                    if (messageEventArgs.Message.Type == type)
                                    {
                                        rightType = true;
                                        break;
                                    }
                                }
                                if (rightType == false)
                                {
                                    await telegramBotClient.SendTextMessageAsync(messageEventArgs.Message.Chat.Id,
                                    "⚠️ *Извините, но этот формат сообщения не поддерживается. Пожалуйста, отправьте текст, фото, видео, аудио, голосовое или видео сообщение!*",
                                    parseMode: ParseMode.Markdown);
                                    return;
                                }

                                if (!string.IsNullOrEmpty(messageEventArgs.Message.Caption))
                                {
                                    messageText = messageEventArgs.Message.Caption.ToString();
                                    checkMessageText = await _checkMessage.CheckLink(checkMessageText, messageEventArgs.Message.Caption, enabledCheckMessages);
                                }
                                else
                                    checkMessageText = $"{checkMessageText}\n\n{NotLoveBot.Program.Program.Ads}";

                                await telegramBotClient.SendTextMessageAsync(anyMessage.Chat.Id, checkMessageText, parseMode: ParseMode.Markdown);
                                await Task.Delay(TimeSpan.FromSeconds((double)botModel.Delay));

                                if (!checkMessageText.Contains("🚫"))
                                    messageType = await SendMediaMessage(telegramBotClient, anyMessage, channelId);
                                else
                                    textMessageStatus = "🚫";
                                break;
                        }

                        string consoleMessageOutput = "файл";
                        // Проверка на наличие текста.
                        if (forwardedMessages.Type == MessageType.Text)
                            consoleMessageOutput = forwardedMessages.Text;

                        // Получения даты отправки сообщения.
                        string currentDateText = DateTime.Today.ToString("dd.MM.yyyy");
                        string currentTimeText = DateTime.Now.ToString("HH:mm");

                        Console.WriteLine($"{textMessageStatus} @{username} (ID: {id}), Type: {messageType}, - | {consoleMessageOutput} |");

                        // Внесение в сообщения в базу данных.
                        string sqliteRequest = $"INSERT INTO UserMessageHistory_{botName} (user, id, message, date, type, status, firstName, lastName, time) VALUES (@username, @id, @message, @date, @type, @status, @firstName, @lastName, @time);";
                        string[,] userMessageHistoryData = {
                            { username, id, messageText, currentDateText, messageType, textMessageStatus, anyMessage.From.FirstName.ToString(), lastName, currentTimeText },
                            { "username", "id", "message", "date", "type", "status", "firstName", "lastName", "time" } 
                        };

                        await _dataProcessing.SetCreateRequest(sqliteRequest, userMessageHistoryData, null);
                        break;
                }
            } catch(Exception exception){Console.WriteLine(exception); }
        }

        private static async Task<string> SendMediaMessage(TelegramBotClient telegramBotClient, Message message, string channelId)
        {
            string? messageType = null;

            switch (message.Type)
            {
                case MessageType.Photo:
                    await telegramBotClient.SendPhotoAsync(channelId, message.Photo.LastOrDefault().FileId, caption: message.Caption);
                    messageType = "📷 фото-файл";
                    break;
                case MessageType.Video:
                    await telegramBotClient.SendVideoAsync(channelId, message.Video.FileId, caption: message.Caption);
                    messageType = "🎥 видео-файл";
                    break;
                case MessageType.Audio:
                    await telegramBotClient.SendAudioAsync(channelId, message.Audio.FileId, caption: message.Caption);
                    messageType = "🎵 аудио-файл";
                    break;
                case MessageType.Voice:
                    await telegramBotClient.SendVoiceAsync(channelId, message.Voice.FileId, caption: message.Caption);
                    messageType = "🎤 голосовое-сообщение";
                    break;
                case MessageType.VideoNote:
                    await telegramBotClient.SendVideoNoteAsync(channelId, message.VideoNote.FileId);
                    messageType = "📀 видео-сообщение";
                    break;
            }

            return messageType;
        }

        // Метод, который проверяет подписку на канал.
        private static async Task<bool> CheckUserSubscription(long userId, TelegramBotClient telegramBotClient, string channelId)
        {
            try
            {
                var chatMember = await telegramBotClient.GetChatMemberAsync(channelId, userId);

                return
                chatMember.Status == ChatMemberStatus.Member ||
                chatMember.Status == ChatMemberStatus.Administrator ||
                chatMember.Status == ChatMemberStatus.Creator;
            }
            catch (Exception) { return false; }
        }
    }
}