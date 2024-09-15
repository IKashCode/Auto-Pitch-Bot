using System;
using System.Collections.Concurrent;
using NotLoveBot.BotController;
using NotLoveBot.DataBaseProcess;
using NotLoveBot.Program;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace NotLoveBot.AdministrationPanelController
{
    public class AdministratorMenu
    {
        // Класс для управления сообщениями с меню.
        private static KeyboardMessagesController _keyboardMessagesController = new KeyboardMessagesController();

        // Класс для работы с базой данных.
        private static SetDataProcessing _setDataProcessing = new SetDataProcessing();

        // Список активных администраторов в паенеле.
        public static ConcurrentDictionary<string, List<string>> ActiveAdministratorsId = new ConcurrentDictionary<string, List<string>>();

        // Переменная, которая позволяет хранить индивидуальные обработчики событий каждого пользователя.
        private static Dictionary<long, EventHandler<CallbackQueryEventArgs>> _usersCallbacks = new Dictionary<long, EventHandler<CallbackQueryEventArgs>>();

        public async Task GetAdministratorMenu(TelegramBotClient telegramBotClient, Message message, Message editMessage, string administratorStatus, string botName)
        {
            try
            {
            // Меню для администратора.
            InlineKeyboardMarkup adminInlineKeyboardMarkup = new InlineKeyboardMarkup(new[] {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅ Фильтр ссылок"),
                    InlineKeyboardButton.WithCallbackData("⏱️ Тайминг")
                },
                new[] { InlineKeyboardButton.WithCallbackData("📋 Отправители") },
                new[] { InlineKeyboardButton.WithCallbackData("← Назад") }
            });

            // Другой вид клавиатуры для главного администратора (владельца).
            if (administratorStatus == "owner")
            {
                adminInlineKeyboardMarkup = new InlineKeyboardMarkup(new[] {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("✅ Фильтр ссылок"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("✍️ Текст"),
                        InlineKeyboardButton.WithCallbackData("⏱️ Тайминг"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("📋 Отправители"),
                        InlineKeyboardButton.WithCallbackData("📜 Модераторы")
                    },
                    new[] { InlineKeyboardButton.WithCallbackData("← Назад") }
                });
            }

            // Создание и отправка сообщения с меню.
            Message administratorMenuPanel;

            if (editMessage == null)
            {
                administratorMenuPanel = await telegramBotClient.SendTextMessageAsync(message.Chat.Id,
                $"⚙️ *Параметры @{botName}*\n\n ● Для ознакомления с функционалом этой панели посетите официальную документацию — [здесь](https://telegra.ph/CHto-takoe-Auto-Pitch-Bot-09-14).\n\n*⇩ Выберите одну из кнопок ниже, чтобы продолжить.*",
                replyMarkup: adminInlineKeyboardMarkup, parseMode: ParseMode.Markdown);
            }
            else
            {
                administratorMenuPanel = await telegramBotClient.EditMessageTextAsync(editMessage.Chat.Id, editMessage.MessageId,
                $"⚙️ *Параметры @{botName}*\n\n ● Для ознакомления с функционалом этой панели посетите официальную документацию — [здесь](https://telegra.ph/CHto-takoe-Auto-Pitch-Bot-09-14).\n\n*⇩ Выберите одну из кнопок ниже, чтобы продолжить.*",
                replyMarkup: adminInlineKeyboardMarkup, parseMode: ParseMode.Markdown);
            }

            // Добавление в общий список сообщений с меню.
            await _keyboardMessagesController.AddKeyboardMessages(administratorMenuPanel);
            
            EventHandler<CallbackQueryEventArgs> BotOnButtonClick = async (sender, callbackQueryEventArgs) =>
            {
                try {     
                var callbackQueryMessage = callbackQueryEventArgs.CallbackQuery;

                // Удаляем всех других пользователей из обработчика другого пользователя.
                if (callbackQueryMessage.From.Id != message.From.Id)
                    return;


                CreatedBotController createdBotController = new CreatedBotController();
                // Проверка на администратора.
                if (await _setDataProcessing.ExistenceCheck($"Administrators_{botName}", "id", message.From.Id.ToString()) == false)
                {
                    await createdBotController.BotParameters(telegramBotClient, message, administratorMenuPanel);

                    telegramBotClient.OnCallbackQuery -= _usersCallbacks[message.From.Id];
                    _usersCallbacks.Remove(message.From.Id);
                    return;
                }

                switch (callbackQueryMessage.Data)
                {
                    case "✅ Фильтр ссылок":
                        var connectionBotModel = ConnectionController.TelegramBotClients.Values.FirstOrDefault(bot => bot.BotName == botName);
                        bool enabledCheckMessages = Convert.ToBoolean(connectionBotModel.FilterStatus);

                        SwitchingSystemStatus switchingSystemStatus = new SwitchingSystemStatus();
                        await switchingSystemStatus.StatusController(telegramBotClient, message, administratorMenuPanel, enabledCheckMessages, "Фильтр ссылок", administratorStatus, botName);
                        break;
                    case "✍️ Текст":
                        MessageTextController messageTextController = new MessageTextController();
                        await messageTextController.SetMessageText(telegramBotClient, message, administratorMenuPanel, administratorStatus, botName);
                        break;
                    case "⏱️ Тайминг":
                        DelayController delayController = new DelayController();
                        await delayController.SetDelay(telegramBotClient, message, administratorMenuPanel, administratorStatus, botName);
                        break;
                    case "📋 Отправители":
                        if (administratorMenuPanel != null)
                        {
                            // Удаление панели администратора.
                            await telegramBotClient.DeleteMessageAsync(administratorMenuPanel.Chat.Id, administratorMenuPanel.MessageId);

                            UnloadList unloadList = new UnloadList();
                            await unloadList.UnloadListSenders(telegramBotClient, message, "📅 Укажите дату в формате *DD.MM.YYYY*, за которую вы хотите получить список всех отправителей сообщений, или воспользуйтесь командой /today, чтобы показать список за сегодня.", administratorStatus, botName);
                        }
                        break;
                    case "📜 Модераторы":
                        AdministratorsController administratorsController = new AdministratorsController();
                        await administratorsController.UnloadListAdministrators(telegramBotClient, message, administratorMenuPanel, administratorStatus, botName);
                        break;
                    case "← Назад":
                        await createdBotController.BotParameters(telegramBotClient, message, administratorMenuPanel);
                        break;
                }

                // Удаление обработчика из списка.
                telegramBotClient.OnCallbackQuery -= _usersCallbacks[message.From.Id];
                _usersCallbacks.Remove(message.From.Id);
                } catch(Exception exception){ Console.WriteLine(exception); }
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