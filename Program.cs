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

namespace NotLoveBot.Program
{
    public class Program
    {
        // public static string ConnectionString = "";

        // private static TelegramBotClient? _telegramBotClient;

        // public static string Token = "6706511312:AAE1DBCvQXffYlqGBH0gdg9P9L0XWIIKsik";
        // public static string Token = "7004921852:AAHVc1w1weO58bnNdYnYPFU4NQpYlIh0HuQ";

        // public static long ChannelId = -1002057738592;
        public static long ChannelId = -1002023006202;


        // Класс - меню администратора.
        private static AdministratorMenu _administratorMenu = new AdministratorMenu();

        // Класс для проверки сообщений.
        private static CheckMessage _checkMessage = new CheckMessage();

        // Класс для работы с базой данных.
        private static SetDataProcessing _dataProcessing = new SetDataProcessing();
        private static GetDataProcessing _getDataProcessing = new GetDataProcessing();

        // Список активных администраторов в паенеле.
        public static List<string> ActiveAdministratorsId = new List<string>();

        // Список администраторов, которые ожидают добавления в базу данных.
        public static List<string> PendingAdditionAdministrators = new List<string>();

        public static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs, string token)
        {
            try{
                TelegramBotClient _telegramBotClient = new TelegramBotClient(token);
                Message anyMessage = messageEventArgs.Message;

                // Получение username и id.
                string username = "username отсутсвует";
                if (anyMessage.From.Username != null)
                    username = anyMessage.From.Username.ToString();

                string id = anyMessage.From.Id.ToString();

                Console.WriteLine($"🛜 @{username}, - | {anyMessage.Text} |");

                // Проверка наличия администраторов в панели.
                bool exists = ActiveAdministratorsId.Exists(item => item.Equals(id, StringComparison.OrdinalIgnoreCase));

                // Если, код администратора не отправлен.
                switch (anyMessage.Text)
                {
                    case "/start":
                        await _telegramBotClient.SendTextMessageAsync(anyMessage.Chat.Id,
                        "👋 *Привет! Я бот, который отправляет сообщения в канал « В Аксае не любят… ».* Подпишитесь на канал [здесь](https://t.me/VAksuiNeLubat), чтобы отправлять текст, фото, видео, аудио, голосовые и видео сообщения. Бот доступен круглосуточно, 24/7.\n\n❓ *Для дополнительной информации используйте команду* /help",
                        parseMode: ParseMode.Markdown);
                        return;
                        break;
                    case "/controller":

                        // Проверка на наличие администратора в списке ожидания подтверждения.
                        if (PendingAdditionAdministrators.Count != 0)
                        {
                            foreach (string pendingAdditionAdministrator in PendingAdditionAdministrators)
                            {
                                if (pendingAdditionAdministrator == username)
                                {
                                    // Добавление в список администраторов.
                                    await _dataProcessing.SetCreateRequest($"INSERT INTO Administrators_test_394832 (id, status, name) VALUES ('{id}', 'moderator', '{username}')");
                                    PendingAdditionAdministrators.Remove(username);
                                    break;
                                }
                            }
                        }

                        List<AdministrationModel> administrationModels = await _getDataProcessing.GetListAdministrators();

                        bool passAdministratorPanel = false;
                        string administratorStatus = "moderator";

                        // Проверка на администратора.
                        foreach (var administrationModel in administrationModels)
                        {
                            if (administrationModel.Id == id)
                            {
                                passAdministratorPanel = true;
                                administratorStatus = administrationModel.Status;
                                break;
                            }
                        }

                        if (passAdministratorPanel == true)
                        {
                            // Добавление в список активных администраторов в панели.
                            if (exists == false)
                            {
                                ActiveAdministratorsId.Add(id);
                                await _telegramBotClient.SendTextMessageAsync(anyMessage.Chat.Id, $"🎉 *Добро пожаловать в панель администратора, {username}!*\n\n❓ Чтобы выйти и продолжить отправку сообщений в канал, пожалуйста, нажмите кнопку « *🚪 Выйти* ».", parseMode: ParseMode.Markdown);
                            }

                            await _administratorMenu.GetAdministratorMenu(_telegramBotClient, anyMessage, null, administratorStatus);
                        }
                        else
                            await _telegramBotClient.SendTextMessageAsync(anyMessage.Chat.Id, $"❌ *У вас нет доступа к этой панели, так как вы не являетесь администратором.* Если вы администратор, свяжитесь с техподдержкой бота с помощью команды /help.", parseMode: ParseMode.Markdown);
                        break;
                    case "/help":
                        await _telegramBotClient.SendTextMessageAsync(anyMessage.Chat.Id,
                        $"💔 *Кого в Аксае не любят?*\n\n📢 *Обратите внимание:*\n● Чтобы публиковать сообщения, нужно быть подписанным на наш канал - « [В Аксае не любят…](https://t.me/VAksuiNeLubat) ». Если вы не подписаны, отправка сообщений будет недоступна. Отправка сообщений полностью анонимна, и данные об отправителях не распространяются.\n\n📸 *Что публикует бот:*\n● Текст, фото, видео, аудио, голосовые и видео сообщения. Все сообщения публикуются моментально, а бот работает круглосуточно, 24/7.\n\n🎉 *Особенности канала:*\n● Канал посвящен развлекательному контенту без серьезных тематик. В данный канал вы можете отправлять тех, кого не любите в Аксае. Автор канала не несет ответственности за опубликованный контент.\n\n📬 *Контакты:*\n● Обратная связь: @admAksNotLove\n● Почта для связи с разработчиком: vanl.not.love.bot@outlook.com",
                        parseMode: ParseMode.Markdown);
                        break;
                    default:
                        if (NotLoveBot.AdministrationPanelController.AdministratorMenu.EnabledMessages == false || exists == true)
                            return;

                        Message forwardedMessages = messageEventArgs.Message;
                        string messageText = "текст отсутсвует";

                        // Проверка подписки на канал.
                        if (await CheckUserSubscription(forwardedMessages.From.Id, _telegramBotClient) == false)
                        {
                            await _telegramBotClient.SendTextMessageAsync(forwardedMessages.Chat.Id,
                            "❗️ *Для использования этой функции подпишитесь на наш канал «* [В Аксае не любят...](https://t.me/VAksuiNeLubat) *»!* 🚀 После подписки вы сможете отправлять сообщения через бота. Спасибо!",
                            parseMode: ParseMode.Markdown);

                            return;
                        }

                        string messageType = "❌ неверный тип файла";
                        string textMessageStatus = "✅";

                        // Ответное сообщение на отправку, которое определяется проверкой.
                        string? checkMessageText = "✨📸 *Спасибо за ваше медиа-сообщение!* ⏳ Оно будет отправлено в канал через `30 секунд`. 💬 Не переживайте, все полностью анонимно!";

                        // Определение типа сообщения.
                        switch (messageEventArgs.Message.Type)
                        {
                            case MessageType.Text:
                                messageType = "📝 текстовое-сообщение";

                                // Проверка сообщение и ответ.
                                checkMessageText = await _checkMessage.CheckForbiddenSymbols(forwardedMessages.Text, NotLoveBot.AdministrationPanelController.AdministratorMenu.EnabledCheckMessages);
                                messageText = forwardedMessages.Text.ToString();

                                await _telegramBotClient.SendTextMessageAsync(anyMessage.Chat.Id, checkMessageText, parseMode: ParseMode.Markdown);
                                await Task.Delay(TimeSpan.FromSeconds(30));

                                if (!checkMessageText.Contains("🚫"))
                                    await _telegramBotClient.SendTextMessageAsync(ChannelId, forwardedMessages.Text, parseMode: ParseMode.Markdown);
                                else
                                    textMessageStatus = "🚫";

                                Console.WriteLine($"{forwardedMessages.From.Username} : {forwardedMessages.Text}");
                                break;
                            case MessageType.Photo:
                                await _telegramBotClient.SendTextMessageAsync(anyMessage.Chat.Id, checkMessageText, parseMode: ParseMode.Markdown);
                                await Task.Delay(TimeSpan.FromSeconds(30));

                                await _telegramBotClient.SendPhotoAsync(ChannelId, forwardedMessages.Photo.LastOrDefault().FileId);
                                messageType = "📷 фото-файл";
                                break;
                            case MessageType.Video:
                                await _telegramBotClient.SendTextMessageAsync(anyMessage.Chat.Id, checkMessageText, parseMode: ParseMode.Markdown);
                                await Task.Delay(TimeSpan.FromSeconds(30));

                                await _telegramBotClient.SendVideoAsync(ChannelId, forwardedMessages.Video.FileId);
                                messageType = "🎥 видео-файл";
                                break;
                            case MessageType.Audio:
                                await _telegramBotClient.SendTextMessageAsync(anyMessage.Chat.Id, checkMessageText, parseMode: ParseMode.Markdown);
                                await Task.Delay(TimeSpan.FromSeconds(30));

                                await _telegramBotClient.SendAudioAsync(ChannelId, forwardedMessages.Audio.FileId);
                                messageType = "🎵 аудио-файл";
                                break;
                            case MessageType.Voice:
                                await _telegramBotClient.SendTextMessageAsync(anyMessage.Chat.Id, checkMessageText, parseMode: ParseMode.Markdown);
                                await Task.Delay(TimeSpan.FromSeconds(30));

                                await _telegramBotClient.SendVoiceAsync(ChannelId, forwardedMessages.Voice.FileId);
                                messageType = "🎤 голосовое-сообщение";
                                break;
                            case MessageType.VideoNote:
                                await _telegramBotClient.SendTextMessageAsync(anyMessage.Chat.Id, checkMessageText, parseMode: ParseMode.Markdown);
                                await Task.Delay(TimeSpan.FromSeconds(30));

                                await _telegramBotClient.SendVideoNoteAsync(ChannelId, forwardedMessages.VideoNote.FileId);
                                messageType = "📀 видео-сообщение";
                                break;
                            default:
                                checkMessageText = "⚠️ *Извините, но этот формат сообщения не поддерживается. Пожалуйста, отправьте текст, фото, видео, аудио, голосовое или видео сообщение!*";
                                await _telegramBotClient.SendTextMessageAsync(anyMessage.Chat.Id, checkMessageText, parseMode: ParseMode.Markdown);
                                break;
                        }

                        string consoleMessageOutput = "файл";
                        // Проверка на наличие текста.
                        if (forwardedMessages.Type == MessageType.Text)
                            consoleMessageOutput = forwardedMessages.Text;

                        // Получения даты отправки сообщения.
                        DateTime currentDate = DateTime.Today;
                        string currentDateText = currentDate.ToString("dd.MM.yyyy");

                        Console.WriteLine($"{textMessageStatus} @{username} (ID: {id}), Type: {messageType}, - | {consoleMessageOutput} |");

                        // Внесение в сообщения в базу данных.
                        string sqliteRequest = $"INSERT INTO UserMessageHistory_test_394832 (user, id, message, date, type, status) VALUES ('{username}', '{id}', '{messageText}', '{currentDateText}', '{messageType}', '{textMessageStatus}');";
                        await _dataProcessing.SetCreateRequest(sqliteRequest);
                        break;
                }
            } catch(Exception exception){Console.WriteLine(exception); }
        }

        // Метод, который проверяет подписку на канал.
        private static async Task<bool> CheckUserSubscription(long userId, TelegramBotClient telegramBotClient)
        {
            try
            {
                var chatMember = await telegramBotClient.GetChatMemberAsync(ChannelId, userId);

                return
                chatMember.Status == ChatMemberStatus.Member ||
                chatMember.Status == ChatMemberStatus.Administrator ||
                chatMember.Status == ChatMemberStatus.Creator;
            }
            catch (Exception) { return false; }
        }
    }
}