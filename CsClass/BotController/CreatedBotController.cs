using System;
using NotLoveBot.DataBaseProcess;
using NotLoveBot.DataChecking;
using NotLoveBot.Interface;
using NotLoveBot.Models;
using NotLoveBot.Program;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using NotLoveBot.AdministrationPanelController;
using System.Text.RegularExpressions;
using NotLoveBot.CheckRules;

namespace NotLoveBot.BotController
{
    public class CreatedBotController
    {
        // Классы для работы с базой данных.
        private IDataChecking _channelNameCheckig = new ChennelNameChecking();
        private SetDataProcessing _setDataProcessing = new SetDataProcessing();
        private static IGetDataProcessing<ConnectionBotModel> _getBotsData = new GetBotsData();
        private static IGetDataProcessing<AdministrationModel> _getAdministratorsData = new GetAdministratorsData();

        // Класс для проверки токена.
        private IDataChecking _tokenChecking = new TokenChecking();
        private CheckMessage _checkMessage = new CheckMessage();

        // Различные контроллеры меню.
        private static AdministratorMenu _administratorMenu = new AdministratorMenu();
        private static BotsMenu _botsMenu = new BotsMenu();

        private static Dictionary<long, EventHandler<CallbackQueryEventArgs>> usersCallbacks = new Dictionary<long, EventHandler<CallbackQueryEventArgs>>();

        public async Task CreateBot(TelegramBotClient telegramBotClient, Message message, Message editMessage, string TokenMessageText)
        {
            try {
            InlineKeyboardMarkup tokenInlineKeyboardMarkup = new InlineKeyboardMarkup(new[] {
                new[] { InlineKeyboardButton.WithUrl("❓ Как создать bot?", "https://youtube.com/shorts/miiZ_wSaA0g?si=Mydch5NmpzsnOMwV") },
                new[] { InlineKeyboardButton.WithCallbackData("← Назад") }
            });

            Message tokenInstructionMessage = await telegramBotClient.EditMessageTextAsync(editMessage.Chat.Id, editMessage.MessageId,
            TokenMessageText,
            replyMarkup: tokenInlineKeyboardMarkup, parseMode: ParseMode.Markdown);

            EventHandler<CallbackQueryEventArgs> CreateBotOnButtonClick = async (sender, callbackQueryEventArgs) =>
            {
                var callbackQueryMessage = callbackQueryEventArgs.CallbackQuery;
                if (callbackQueryMessage.From.Id != message.From.Id)
                    return;

                if (callbackQueryMessage.Data == "← Назад")
                {
                    telegramBotClient.OnMessage -= BotOnMessageReceivedToken;
                    await _botsMenu.GetBotsMenu(telegramBotClient, message, tokenInstructionMessage);
                }
            };

            // Если обработчик с ID пользователя уже создан, то он удаляется.
            if (usersCallbacks.ContainsKey(message.From.Id))
            {
                telegramBotClient.OnCallbackQuery -= usersCallbacks[message.From.Id];
                usersCallbacks[message.From.Id] = CreateBotOnButtonClick;
            }
            else
                usersCallbacks.Add(message.From.Id, CreateBotOnButtonClick);

            telegramBotClient.OnCallbackQuery += CreateBotOnButtonClick;

            telegramBotClient.OnMessage += BotOnMessageReceivedToken;
            async void BotOnMessageReceivedToken(object sender, MessageEventArgs messageEventArgs)
            {
                Message tokenMessage = messageEventArgs.Message;
                string token = tokenMessage.Text;

                if (tokenMessage.From.Id != message.From.Id)
                    return;
                // Проверка на наличие команд.
                foreach (string command in ConnectionController.commands)
                {
                    if (token == command || tokenMessage.Type != MessageType.Text)
                    {
                        telegramBotClient.OnMessage -= BotOnMessageReceivedToken;
                        return;
                    }
                }
                
                if (!ConnectionController.TelegramBotClients.ContainsKey(token))
                {
                    if (await _tokenChecking.CheckingValidity(telegramBotClient, token) == true)
                    {
                        // Создание Telegram клиента и добавление в список рабочих клиентов.
                        TelegramBotClient connectTelegramBotClient = new TelegramBotClient(token);

                        // Получение информации о подключаемом боте.
                        var botInformation = await connectTelegramBotClient.GetMeAsync();

                        await telegramBotClient.SendTextMessageAsync(tokenMessage.Chat.Id,
                        "👽 Отлично, *бот найден. Пришлите @username канала*, в который будут пересылаться сообщения пользователей.",
                        parseMode: ParseMode.Markdown);

                        // Удаление клавиатуру у сообщения выше.
                        await telegramBotClient.EditMessageReplyMarkupAsync(message.Chat.Id, tokenInstructionMessage.MessageId, replyMarkup: null);

                        telegramBotClient.OnMessage += BotOnMessageReceivedChannelName;
                        async void BotOnMessageReceivedChannelName(object sender, MessageEventArgs messageEventArgs)
                        {
                            Message channelNameMessage = messageEventArgs.Message;
                            string usernameChannel = channelNameMessage.Text;

                            if (channelNameMessage.From.Id != message.From.Id)
                                return;
                            // Проверка на наличие команд.
                            foreach (string command in ConnectionController.commands)
                            {
                                if (usernameChannel == command || channelNameMessage.Type != MessageType.Text)
                                {
                                    telegramBotClient.OnMessage -= BotOnMessageReceivedToken;
                                    return;
                                }
                            }

                            // Проверка на наличие ссылки.
                            string pattern = @"https?:\/\/t\.me\/([a-zA-Z0-9_]+)";
                            Match match = Regex.Match(usernameChannel, pattern);
                            if (match.Success)
                                usernameChannel = $"@{match.Groups[1].Value}";

                            if (await _channelNameCheckig.CheckingValidity(telegramBotClient, usernameChannel) == true)
                            {  
                                var chatInformation = await telegramBotClient.GetChatAsync(usernameChannel);

                                // Проверка на наличие username канала.
                                string channelName = await ExistenceCheck(botInformation.Username.ToString(), "channelname");
                                // Проверка на наличие username пользователя.
                                string userName = await ExistenceCheck(channelNameMessage.From.Username.ToString(), "username");

                                // Массив SQLite запросов для подключения bot.
                                string[] SQLiteRequestsCreate = {
                                    $"CREATE TABLE UserMessageHistory_{botInformation.Username.ToString()} (number INTEGER PRIMARY KEY, user TEXT, message TEXT, id TEXT, date TEXT, type TEXT, status TEXT, firstName TEXT, lastName TEXT, time TEXT);",
                                    $"CREATE TABLE Administrators_{botInformation.Username.ToString()} (id TEXT, status TEXT, name TEXT);" };
                                string[] SQLiteRequestsAdd = {
                                    $"INSERT INTO Bots (botName, token, channelName, channelID, userID, delay, replyMessageText, filterStatus, startMessageText) VALUES (@username, @token, @channelName, @channelId, @userId, @delay, @replyMessageText, @filterStatus, @startMessageText);",
                                    $"INSERT INTO Administrators_{botInformation.Username.ToString()} (id, status, name) VALUES (@channelName, @status, @username);"
                                };
                                object[,] botsData = {
                                    { botInformation.Username.ToString(), token, channelName, chatInformation.Id.ToString(), message.From.Id.ToString(), 30, "✨ *Благодарим за ваше сообщение!* ⏳ Оно будет отправлено в канал в ближайшее время.", 0, "👋 *Привет!* Я бот, который поможет вам отправить сообщение в группу или канал. Просто отправьте ваше сообщение, и я сделаю всё остальное! 🚀" },
                                    { "username", "token", "channelName", "channelId", "userId", "delay", "replyMessageText", "filterStatus", "startMessageText" }
                                },
                                administratorsData = {
                                    { channelNameMessage.From.Id.ToString(), "owner", userName },
                                    { "channelName", "status", "username" }
                                };

                                try {
                                    foreach (string SQLiteRequestCreate in SQLiteRequestsCreate)
                                    {
                                        await _setDataProcessing.SetCreateRequest(SQLiteRequestCreate, null, null);
                                    }

                                    // Цикл, который выполняет только запросы внесения в базу данных.
                                    for (int index = 0; index < SQLiteRequestsAdd.Length; index++)
                                    {
                                        switch (index)
                                        {
                                            case 0:
                                                await _setDataProcessing.SetCreateRequest(SQLiteRequestsAdd[index], botsData, null);
                                                break;
                                            case 1:
                                                await _setDataProcessing.SetCreateRequest(SQLiteRequestsAdd[index], administratorsData, null);
                                                break;
                                        }
                                    }
                                } catch(Exception exception) { Console.WriteLine(exception); }

                                // Запуск новго telegram bot.
                                try
                                {
                                    // Создание модели для добавления в список информации о боте.
                                    var connectionBotModel = new ConnectionBotModel
                                    {
                                        BotName = botInformation.Username.ToString(),
                                        Token = token,
                                        ChannelName = channelName,
                                        ChannelId = chatInformation.Id.ToString(),
                                        UserId = channelNameMessage.From.Id.ToString(),
                                        BotClient = connectTelegramBotClient,
                                        Delay = 30,
                                        FilterStatus = 0,
                                        ReplyMessageText = botsData[0, 6].ToString(),
                                        StartMessageText = botsData[0, 8].ToString()
                                    };

                                    // Добавление в список активных telegram bot.
                                    ConnectionController.TelegramBotClients[token] = connectionBotModel;

                                    connectTelegramBotClient.OnMessage += (sender, messageEventArgs) => NotLoveBot.Program.Program.BotOnMessageReceived(sender, messageEventArgs, token);
                                    connectTelegramBotClient.OnMessageEdited  += (sender, messageEventArgs) => NotLoveBot.Program.Program.BotOnMessageReceived(sender, messageEventArgs, token);
                                    connectTelegramBotClient.StartReceiving();

                                    // Логирования об успешном запуске.
                                    Console.WriteLine($"The user has successfully integrated the new telegram bot!\nData——————————————————————————————————————————————————————\n— Token: {token.ToString()}\n— Channel: {channelName}\n— Username: @{userName}\n——————————————————————————————————————————————————————————\n");

                                    await telegramBotClient.SendTextMessageAsync(channelNameMessage.Chat.Id,
                                    $"*Ваш* [бот](https://t.me/{connectionBotModel.BotName}) *успешно интегрирован с @AutoPitchBot!* 🎉\n\n● Подписчики теперь могут автоматически отправлять сообщения в ваш канал или группу. Для *настройки и управления* перейдите в раздел *«⚙️ Параметры ваших ботов»*.",
                                    parseMode: ParseMode.Markdown);
                                }
                                catch (Exception exception) {
                                    // Логирования об не успешном запуске.
                                    Console.WriteLine($"The user has not successfully integrated the new telegram bot!\nException————————————————————————————————————————————————————\n{exception}\nData—————————————————————————————————————————————————————————\n— Token: {token.ToString()}\n— Channel: {channelName}\n— Username: {userName}\n—————————————————————————————————————————————————————————————");
                                }

                                telegramBotClient.OnMessage -= BotOnMessageReceivedChannelName;
                            }
                            else
                            {
                                await telegramBotClient.SendTextMessageAsync(channelNameMessage.Chat.Id,
                                "⚠️ *Не удается найти канал или группу!* Пожалуйста, проверьте правильность @username вашего канала.",
                                parseMode: ParseMode.Markdown);

                                telegramBotClient.OnMessage -= BotOnMessageReceivedChannelName;
                                telegramBotClient.OnMessage += BotOnMessageReceivedChannelName;
                            }
                        }

                        telegramBotClient.OnMessage -= BotOnMessageReceivedToken;
                    }
                    else
                    {
                        await telegramBotClient.SendTextMessageAsync(message.Chat.Id,
                        "⚠️ *Бот не найден!* Пожалуйста, проверьте токен и попробуйте снова.",
                        parseMode: ParseMode.Markdown);

                        telegramBotClient.OnMessage -= BotOnMessageReceivedToken;
                        telegramBotClient.OnMessage += BotOnMessageReceivedToken;
                    }
                }
                else
                {
                    await telegramBotClient.SendTextMessageAsync(tokenMessage.Chat.Id,
                    "⚠️ *Похоже, что данный бот уже успешно интегрирован с @AutoPitchBot.* Пожалуйста, попробуйте *ввести токен* еще раз для проверки или *вернитесь* в главное меню.",
                    parseMode: ParseMode.Markdown);

                    telegramBotClient.OnMessage -= BotOnMessageReceivedToken;
                    telegramBotClient.OnMessage += BotOnMessageReceivedToken;
                }
            }
            } catch(Exception exception) { Console.WriteLine(exception); }
        }

        public async Task DeleteBot(TelegramBotClient telegramBotClient, Message message, Message editMessage)
        {
            try {
            // Получение всех ботов конкретного пользователя.
            List<ConnectionBotModel> connectionBotModels = await _getBotsData.GetList(message.From.Id.ToString(), "userID");
            InlineKeyboardMarkup deleteBotsInlineKeyboardMarkup = await GetBotsKeyboard(message, connectionBotModels);
            Message botsDeleteMessage;
            
            if (connectionBotModels.Count == 0)
            {
                botsDeleteMessage = await telegramBotClient.EditMessageTextAsync(editMessage.Chat.Id, editMessage.MessageId,
                "🤔 *У вас пока нет ботов.* Чтобы создать бота, перейдите в главное меню, успользуя команду — /menu",
                replyMarkup: deleteBotsInlineKeyboardMarkup, parseMode: ParseMode.Markdown);
            }
            else
            {
                botsDeleteMessage = await telegramBotClient.EditMessageTextAsync(editMessage.Chat.Id, editMessage.MessageId,
                "❗️ *Выберите бота, которого вы хотите удалить.*\nПосле этого бот будет отключен от системы и не сможет пересылать сообщения.",
                replyMarkup: deleteBotsInlineKeyboardMarkup, parseMode: ParseMode.Markdown);
            }

            EventHandler<CallbackQueryEventArgs> DeleteBotOnButtonClick = async (sender, callbackQueryEventArgs) =>
            {
                var callbackQueryMessage = callbackQueryEventArgs.CallbackQuery;
                if (callbackQueryMessage.From.Id != message.From.Id)
                    return;

                if (callbackQueryMessage.Data == "← Назад")
                {
                    await _botsMenu.GetBotsMenu(telegramBotClient, message, botsDeleteMessage);
                    return;
                }
                else
                {
                    try
                    {
                        // Удаление из списка telegram bots.
                        string deleteBotName = callbackQueryMessage.Data.Substring(3);
                        var connectionBotModel = ConnectionController.TelegramBotClients.Values.FirstOrDefault(bot => bot.BotName == deleteBotName);
                        ConnectionController.TelegramBotClients.TryRemove(connectionBotModel.Token, out _);

                        string[] SQLiteRequests = {
                            $"DELETE FROM Bots WHERE BotName = @{deleteBotName}",
                            $"DROP TABLE Administrators_{deleteBotName}",
                            $"DROP TABLE UserMessageHistory_{deleteBotName}"
                        };

                        foreach (var SQLiteRequest in SQLiteRequests)
                        { await _setDataProcessing.SetCreateRequest(SQLiteRequest, null, deleteBotName); }

                        // Остановка bot.
                        connectionBotModel.BotClient.StopReceiving();

                        List<ConnectionBotModel> connectionBotModels = await _getBotsData.GetList(message.From.Id.ToString(), "userID");
                        InlineKeyboardMarkup deleteBotsInlineKeyboardMarkup = await GetBotsKeyboard(message, connectionBotModels);

                        await DeleteBot(telegramBotClient, message, botsDeleteMessage);
                    }
                    catch (Exception exception) { Console.WriteLine(exception); }
                }
            };
            
            // Если обработчик с ID пользователя уже создан, то он удаляется.
            if (usersCallbacks.ContainsKey(message.From.Id))
            {
                telegramBotClient.OnCallbackQuery -= usersCallbacks[message.From.Id];
                usersCallbacks[message.From.Id] = DeleteBotOnButtonClick;
            }
            else
                usersCallbacks.Add(message.From.Id, DeleteBotOnButtonClick);
                
            telegramBotClient.OnCallbackQuery += DeleteBotOnButtonClick;
        } catch(Exception exception) { Console.WriteLine(exception); }
        }

        public async Task BotParameters(TelegramBotClient telegramBotClient, Message message, Message editMessage)
        {
            try {
            List<ConnectionBotModel> connectionBotModels = await _getBotsData.GetList(null, null);
            var inlineKeyboardButton = new List<InlineKeyboardButton[]>();
            int inlineKeyboardLength = 0;

            foreach (var connectionBotModel in connectionBotModels)
            {
                // Получаем всех администраторов в системе.
                List<AdministrationModel> administrationModels = await _getAdministratorsData.GetList(null, connectionBotModel.BotName);
                bool isAdministrator = await _setDataProcessing.ExistenceCheck($"Administrators_{connectionBotModel.BotName}", "id", message.From.Id.ToString());

                // Проверяем на наличие в конкретной системе.
                if (isAdministrator == true)
                {
                    inlineKeyboardLength++;
                    string status = "😎";

                    foreach (var administrationModel in administrationModels)
                    {
                        // Считываем данные администратора.
                        if (administrationModel.Id == message.From.Id.ToString())
                        {
                            if (administrationModel.Status == "owner")
                                status = "👑";

                            // Добавляем кнопку для администратора.
                            inlineKeyboardButton.Add(new[] { InlineKeyboardButton.WithCallbackData($"@{connectionBotModel.BotName} → {status}") });
                        }
                    }
                }
            }
            // Создаем клавиатуру ботов.
            inlineKeyboardButton.Add(new[] { InlineKeyboardButton.WithCallbackData("← Назад") });
            InlineKeyboardMarkup botParametersInlineKeyboardMarkup = new InlineKeyboardMarkup(inlineKeyboardButton);

            Message botParametersMessage;

            if (editMessage == null)
            {
                if (inlineKeyboardLength == 0) {
                    botParametersMessage = await telegramBotClient.SendTextMessageAsync(message.Chat.Id,
                    "🤔 *У вас пока нет ботов.* Чтобы создать бота, перейдите в главное меню, успользуя команду — /menu",
                    replyMarkup: botParametersInlineKeyboardMarkup, parseMode: ParseMode.Markdown); }
                else {
                    botParametersMessage = await telegramBotClient.SendTextMessageAsync(message.Chat.Id,
                    "🤖 *Выберите бота, для которого хотите изменить настройки:*",
                    replyMarkup: botParametersInlineKeyboardMarkup, parseMode: ParseMode.Markdown); }
            }
            else
            {
                if (inlineKeyboardLength == 0) {
                    botParametersMessage = await telegramBotClient.EditMessageTextAsync(editMessage.Chat.Id, editMessage.MessageId,
                    "🤔 *У вас пока нет ботов.* Чтобы создать бота, перейдите в главное меню, успользуя команду — /menu",
                    replyMarkup: botParametersInlineKeyboardMarkup, parseMode: ParseMode.Markdown); }
                else {
                    botParametersMessage = await telegramBotClient.EditMessageTextAsync(editMessage.Chat.Id, editMessage.MessageId,
                    "🤖 *Выберите бота, для которого хотите изменить настройки:*",
                    replyMarkup: botParametersInlineKeyboardMarkup, parseMode: ParseMode.Markdown); }
            }

            EventHandler<CallbackQueryEventArgs> DeleteBotOnButtonClick = async (sender, callbackQueryEventArgs) =>
            {
                var callbackQueryMessage = callbackQueryEventArgs.CallbackQuery;
                if (callbackQueryMessage.From.Id != message.From.Id)
                    return;

                if (callbackQueryMessage.Data == "← Назад")
                    await _botsMenu.GetBotsMenu(telegramBotClient, message, botParametersMessage);
                else
                {
                    string botName = callbackQueryMessage.Data.Substring(1);
                    botName = botName.Remove(botName.Length - 5);

                    string status = "moderator";
                    if (callbackQueryMessage.Data.Contains("👑"))
                        status = "owner";

                    // Вызываем панель администратора.
                    await _administratorMenu.GetAdministratorMenu(telegramBotClient, message, botParametersMessage, status, botName);
                }

                // Удаляем этот обработчик для коректной работы.
                telegramBotClient.OnCallbackQuery -= usersCallbacks[message.From.Id];
                usersCallbacks.Remove(message.From.Id);
            };

            // Если обработчик с ID пользователя уже создан, то он удаляется.
            if (usersCallbacks.ContainsKey(message.From.Id))
            {
                telegramBotClient.OnCallbackQuery -= usersCallbacks[message.From.Id];
                usersCallbacks[message.From.Id] = DeleteBotOnButtonClick;
            }
            else
                usersCallbacks.Add(message.From.Id, DeleteBotOnButtonClick);
                
            telegramBotClient.OnCallbackQuery += DeleteBotOnButtonClick;
        } catch(Exception exception) { Console.WriteLine(exception); }
        }

        private async Task<InlineKeyboardMarkup> GetBotsKeyboard(Message message, List<ConnectionBotModel> connectionBotModels)
        {
            // Получение кнопок для deleteBotsInlineKeyboardMarkup.
            var inlineKeyboardButton = new List<InlineKeyboardButton[]>();
            foreach (var connectionBotModel in connectionBotModels)
            { inlineKeyboardButton.Add(new[] { InlineKeyboardButton.WithCallbackData($"❌ @{connectionBotModel.BotName}") }); }
            inlineKeyboardButton.Add(new[] { InlineKeyboardButton.WithCallbackData("← Назад") });

            InlineKeyboardMarkup inlineKeyboardMarkup = new InlineKeyboardMarkup(inlineKeyboardButton);
            return inlineKeyboardMarkup;
        }

        private async Task<string> ExistenceCheck(string dataVerifiable, string dataName)
        {
            if (dataVerifiable != null)
                return dataVerifiable;

            return $"{dataName} отсутсвует";
        }
    }
}