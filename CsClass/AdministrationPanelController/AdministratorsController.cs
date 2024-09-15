using System;
using System.Text;
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
    class AdministratorsController
    {
        // Класс для управления сообщениями с меню.
        private static KeyboardMessagesController _keyboardMessagesController = new KeyboardMessagesController();
        private SetDataProcessing _setDataProcessing = new SetDataProcessing();
        private IGetDataProcessing<AdministrationModel> _getAdministratorsData = new GetAdministratorsData();
        private static Dictionary<long, EventHandler<CallbackQueryEventArgs>> _usersCallbacks = new Dictionary<long, EventHandler<CallbackQueryEventArgs>>();
        
        public async Task UnloadListAdministrators(TelegramBotClient telegramBotClient, Message message, Message editMessage, string administratorStatus, string botName)
        {
            try{
            List<AdministrationModel> administrationModels = await _getAdministratorsData.GetList(null, botName);
            string administratorsListText = "📜 *Список модераторов:*\n";

            foreach (var administrationModel in administrationModels)
            {
                // Если администратор является владельцем, то ставится корона вместо очков.
                string administrationStatus = "😎";
                if (administrationModel.Status == "owner")
                    administrationStatus = "👑";

                administratorsListText += $"\n{administrationStatus} `{administrationModel.Name}`\n🆔 `{administrationModel.Id}`\n";
            }

            InlineKeyboardMarkup ownerInlineKeyboardMarkup = new InlineKeyboardMarkup(new[] {
                new[] 
                { 
                    InlineKeyboardButton.WithCallbackData("➕ Добавить"),
                    InlineKeyboardButton.WithCallbackData("➖ Удалить") 
                },
                new[] { InlineKeyboardButton.WithCallbackData("← Назад") }
            });

            Message administratorsList = await telegramBotClient.EditMessageTextAsync(editMessage.Chat.Id, editMessage.MessageId, administratorsListText, replyMarkup: ownerInlineKeyboardMarkup, parseMode: ParseMode.Markdown);
            AdministratorMenu administratorMenu = new AdministratorMenu();
            // Добавление в общий список сообщений с меню.
            await _keyboardMessagesController.AddKeyboardMessages(administratorsList);

            EventHandler<CallbackQueryEventArgs> BotOnButtonClick = async (sender, callbackQueryEventArgs) =>
            {
                var callbackQueryMessage = callbackQueryEventArgs.CallbackQuery;
                if (callbackQueryMessage.From.Id != message.From.Id)
                    return;

                switch (callbackQueryMessage.Data)
                {
                    case "➕ Добавить":
                        await telegramBotClient.DeleteMessageAsync(administratorsList.Chat.Id, administratorsList.MessageId);
                        await Controller(telegramBotClient, message, administratorStatus, "добавить", botName);
                        break;
                    case "➖ Удалить":
                        await Controller(telegramBotClient, message, administratorStatus, "удалить", botName);
                        break;
                    case "← Назад":
                        await administratorMenu.GetAdministratorMenu(telegramBotClient, message, administratorsList, administratorStatus, botName);
                        telegramBotClient.OnCallbackQuery -= _usersCallbacks[message.From.Id];
                        _usersCallbacks.Remove(message.From.Id);
                        break;
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
            }catch(Exception){}
        }

        private async Task Controller(TelegramBotClient telegramBotClient, Message message, string administratorStatus, string action, string botName)
        {
            try {
            bool isDelete = true;
            AdministratorMenu administratorMenu = new AdministratorMenu();
            if (action != "удалить")
            {
                // Генерация кода.
                string administratorCode = await GetCode(16);
                isDelete = false;

                InlineKeyboardMarkup ownerInlineKeyboardMarkup = new InlineKeyboardMarkup(new[] {
                    new[] { InlineKeyboardButton.WithCallbackData("❌ Закрыть") }});
                
                // Добавление кода в базу данных.
                string[,] administratorCodesData = { { administratorCode, botName }, { "code", "botName" } };
                await _setDataProcessing.SetCreateRequest("INSERT INTO AdministratorCodes (code, botName) VALUES (@code, @botName)", administratorCodesData, null);

                Message administratorCodeMessage = await telegramBotClient.SendTextMessageAsync(message.Chat.Id,
                $"👤 Для подтверждения назначения в администраторы, новому администратору *нужно отправить команду с кодом в @AutoPitchBot:*\n`/confirm {administratorCode}`\n● После подтверждения, новый администратор получит возможность управлять параметрами *@{botName}*", 
                replyMarkup: ownerInlineKeyboardMarkup, parseMode: ParseMode.Markdown);

                // Добавление в общий список сообщений с меню.
                await _keyboardMessagesController.AddKeyboardMessages(administratorCodeMessage);
                
                EventHandler<CallbackQueryEventArgs> BotOnButtonClick = async (sender, callbackQueryEventArgs) =>
                {
                    var callbackQueryMessage = callbackQueryEventArgs.CallbackQuery;
                    if (callbackQueryMessage.Data == "❌ Закрыть")
                    {
                        ConnectionController.KeyboardMessages[message.Chat.Id].Remove(administratorCodeMessage.MessageId);
                        await _keyboardMessagesController.DeleteKeyboardMessages(message.Chat.Id);

                        await telegramBotClient.DeleteMessageAsync(administratorCodeMessage.Chat.Id, administratorCodeMessage.MessageId);
                        await administratorMenu.GetAdministratorMenu(telegramBotClient, message, null, administratorStatus, botName);
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

            // Запрещать удаление при добавлении нового администратора.
            if (isDelete == false)
                return;

            await telegramBotClient.SendTextMessageAsync(message.Chat.Id, "❗️ Отправьте имя администратора, *указанного в списке*, чтобы удалить его из модераторов.", parseMode: ParseMode.Markdown);
            telegramBotClient.OnMessage += BotOnMessageReceived;
            async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
            {
                Message replyMessage = messageEventArgs.Message;
                string username = replyMessage.Text;

                if (replyMessage.From.Id != message.From.Id)
                    return;
                // Проверка на наличие команд.
                foreach (string command in ConnectionController.commands)
                {
                    if (username == command || replyMessage.Type != MessageType.Text)
                    {
                        telegramBotClient.OnMessage -= BotOnMessageReceived;
                        return;
                    }
                }

                switch (action)
                {
                    case "добавить":
                        // Удаление "@" в начале username.
                        if (username.StartsWith("@"))
                            username = username.TrimStart('@');

                        NotLoveBot.Program.Program.PendingAdditionAdministrators.Add(username);

                        await telegramBotClient.SendTextMessageAsync(message.Chat.Id,
                        $"✅ *Администратор добавлен в список ожидающих подтверждения.* Чтобы он смог получить статус администратора, ему необходимо выполнить команду /controller.\n\n❓ Если вы хотите очистить список ожидающих, используйте команду /clear.",
                        parseMode: ParseMode.Markdown);

                        await administratorMenu.GetAdministratorMenu(telegramBotClient, message, null, administratorStatus, botName);
                        break;
                    case "удалить":
                        string resultText = "✅ *Администратор был успешно снят с должности!*";

                        // Проверка на наличие администратора в списке.
                        if (await _setDataProcessing.ExistenceCheck($"Administrators_{botName}", "name", username) == true)
                        {
                            List<AdministrationModel> administrationModels = await _getAdministratorsData.GetList(null, botName);
                            bool deletePermission = true;

                            // Проверка статуса администратора перед удалением через цикл.
                            foreach (var administrationModel in administrationModels)
                            {
                                if (administrationModel.Name == username & administrationModel.Status == "owner")
                                {
                                    deletePermission = false;

                                    resultText = "❌ *Невозможно удалить владельца из списка модераторов, поскольку он назначается или удаляется системой!*";
                                    break;
                                }
                            }

                            // Запрос удаления выполняется, если система проверка дала разрешение.
                            if (deletePermission == true)
                                await _setDataProcessing.SetCreateRequest($"DELETE FROM Administrators_{botName} WHERE name = @{username}", null, username);
                        }
                        else
                            resultText = "🤔 Простите, администратор не обнаружен в списке. Возможно, он уже был удалён.";

                            await telegramBotClient.SendTextMessageAsync(message.Chat.Id, resultText, parseMode: ParseMode.Markdown);
                            await administratorMenu.GetAdministratorMenu(telegramBotClient, message, null, administratorStatus, botName);
                        break;
                }

                telegramBotClient.OnMessage -= BotOnMessageReceived;
            }}catch(Exception){}
        }

        private static readonly Random _random = new Random();
        private async Task<string> GetCode(int length)
        {
            const string symbols = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            StringBuilder stringBuilder = new StringBuilder();

            for (int index = 0; index < length; index++) { stringBuilder.Append(symbols[_random.Next(symbols.Length)]); }
            return stringBuilder.ToString();
        }
    }
}