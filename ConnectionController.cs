using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Formats.Asn1;
using NotLoveBot.AdministrationPanelController;
using NotLoveBot.BotController;
using NotLoveBot.DataBaseProcess;
using NotLoveBot.Interface;
using NotLoveBot.Models;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace NotLoveBot.Program
{
    public class ConnectionController
    {
        public static TelegramBotClient? _telegramBotClient;
        // public static string Token = "7098079115:AAHyxAsO9XthZA79Gu4EY9I7WsJwsbnw3_k";
        public static string Token = "6820716903:AAEo7r-nGOLSJfNQfVZ-MMNERSvzEiq9a60";
        
        public static ConcurrentDictionary<string, ConnectionBotModel> TelegramBotClients = new ConcurrentDictionary<string, ConnectionBotModel>();

        // Переменная, которая сохраняет в себе все сообщения с меню.
        public static Dictionary<long, List<int>> KeyboardMessages = new Dictionary<long, List<int>>();

        // Класс для работы с базой данных.
        private static IGetDataProcessing<ConnectionBotModel> _getBotsData = new GetBotsData();
        private static SetDataProcessing _setDataProcessing = new SetDataProcessing();
        private static GetBotNameByCode _getBotNameByCode = new GetBotNameByCode();

        // Класс для управления сообщениями с меню.
        private static KeyboardMessagesController _keyboardMessagesController = new KeyboardMessagesController();

        private static BotsMenu _botsMenu = new BotsMenu();
        public static string[] commands = { "/start", "/menu", "/mybots", "/help" };

        public static async Task Main(string[] args)
        {
            // Запуск всех уже интегрированных ботов из базы данных.
            List<ConnectionBotModel> connectionBotModels = await _getBotsData.GetList(null, null);
            
            foreach (var connectionBotModel in connectionBotModels)
            {
                // Добавление в список активных telegram bot.
                ConnectionController.TelegramBotClients[connectionBotModel.Token] = connectionBotModel;

                connectionBotModel.BotClient.OnMessage += (sender, messageEventArgs) => NotLoveBot.Program.Program.BotOnMessageReceived(sender, messageEventArgs, connectionBotModel.Token);
                connectionBotModel.BotClient.OnMessageEdited  += (sender, messageEventArgs) => NotLoveBot.Program.Program.BotOnMessageReceived(sender, messageEventArgs, connectionBotModel.Token);
                connectionBotModel.BotClient.StartReceiving();
            }

            _telegramBotClient = new TelegramBotClient(Token);
            
            _telegramBotClient.OnMessage += SuperBotOnMessageReceived;
            _telegramBotClient.OnMessageEdited += SuperBotOnMessageReceived;

            try {
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

                switch (message.Text)
                {
                    case "/start":
                        await _telegramBotClient.SendTextMessageAsync(message.Chat.Id,
                        "👋 *Привет!* Познакомьтесь с *@AutoPitchBot* — это конструктор ботов, который автоматизирует процесс пересылки сообщений между чатами. Создавайте и добавляйте своих ботов в систему в *неограниченном количестве!*\n\n⚙️ Чтобы настроить бота, используйте команду */menu*. Начните автоматизировать свои чаты с Auto Pitch Bot! 🚀",
                        parseMode: ParseMode.Markdown);
                        break;
                    case "/menu":
                        await _keyboardMessagesController.DeleteKeyboardMessages(message.Chat.Id);
                        await _botsMenu.GetBotsMenu(_telegramBotClient, message, null);
                        break;
                    case "/mybots":
                        await _keyboardMessagesController.DeleteKeyboardMessages(message.Chat.Id);
                        CreatedBotController createdBotController = new CreatedBotController();
                        await createdBotController.BotParameters(_telegramBotClient, message, null);
                        break;
                    case "/help":
                            InlineKeyboardMarkup helpInlineKeyboardMarkup = new InlineKeyboardMarkup(new[] {
                                new[] { InlineKeyboardButton.WithUrl("👽 Обратиться в поддержку →", $"https://t.me/AutoPitchBotSupport") },
                            });
                        await _telegramBotClient.SendTextMessageAsync(message.Chat.Id,
                        $"*@AutoPitchBot — это конструктор ботов*, который автоматизирует процесс пересылки сообщений между чатами. Вы можете создать *неограниченное количество* ботов. Кроме того, сервис *полностью бесплатен* благодаря рекламе в конце сообщений.\n\n❓*Как использовать бота?* Полная документация доступна по следующей ссылке — https://telegra.ph/CHto-takoe-Auto-Pitch-Bot-09-14\n\n🎧 *Техническая поддержка:*\n● _Telegram — @AutoPitchBotSupport_\n● _Email — IKashCode@outlook.com_\n❗️ *Если вы обнаружили ошибку, пожалуйста, сообщите разработчику!*\n\n🔔 *Telegram канал с актуальными новостями AutoPitchBot — @IKashCodeDev*",
                        replyMarkup: helpInlineKeyboardMarkup, parseMode: ParseMode.Markdown);
                        break;
                    default:
                        if (message.Text.Contains("/confirm"))
                        {
                            string administratorCode = message.Text.Substring(message.Text.IndexOf(' ', + 1)).Trim();
                            bool isAdministrator = await _setDataProcessing.ExistenceCheck("AdministratorCodes", "code", administratorCode);
                            if (isAdministrator == true)
                            {
                                string botName = await _getBotNameByCode.GetBotName(administratorCode);
                                if (botName != null)
                                {
                                    await _setDataProcessing.SetCreateRequest($"DELETE FROM AdministratorCodes WHERE code = @{administratorCode};", null, administratorCode);
                                    if (await _setDataProcessing.ExistenceCheck($"Administrators_{botName}", "id", message.From.Id.ToString()))
                                    {
                                        await _telegramBotClient.SendTextMessageAsync(message.Chat.Id,
                                        $"⚠️ *Данный администратор уже зарегистрирован в системе.* Вы не можете добавить его повторно!",
                                        parseMode: ParseMode.Markdown);
                                        return;
                                    }

                                    // Определение username администратора.
                                    string username = message.From.Username;
                                    if (username == null)
                                        username = $"NoName_{message.From.Id}";

                                    // Запись в базу данных.
                                    string[,] administratorsData = {
                                        { message.From.Id.ToString(), "moderator", username },
                                        { "channelName", "status", "username" }};
                                    await _setDataProcessing.SetCreateRequest($"INSERT INTO Administrators_{botName} (id, status, name) VALUES (@channelName, @status, @username);", administratorsData, null);

                                    await _telegramBotClient.SendTextMessageAsync(message.Chat.Id,
                                    $"🔐 *Вы успешно добавлены в администраторы @{botName}!* Для управления перейдите в *«⚙️ Параметры ваших ботов»*.",
                                    parseMode: ParseMode.Markdown);
                                }
                            }
                            else
                            {
                                await _telegramBotClient.SendTextMessageAsync(message.Chat.Id,
                                "⚠️ *Данный код подтверждения не активен или уже использован!*\n● Если вы уверены, что ввели правильный код, пожалуйста, свяжитесь со [службой поддержки](https://t.me/AutoPitchBotSupport) для дальнейшей помощи.",
                                parseMode: ParseMode.Markdown);
                            }
                        }
                        break;
                }
            }
            catch (Exception exception) { Console.WriteLine(exception); }
        }
    }
}