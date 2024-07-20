using System;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using NotLoveBot.Program;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

namespace NotLoveBot.AdministrationPanelController
{
    public class AdministratorMenu
    {
        // Переменные, которые отвечают за работу отправки и проверки.
        public static bool EnabledMessages = true, EnabledCheckMessages = true;
        
        // Переменная, которая позволяет хранить индивидуальные обработчики событий каждого пользователя.
        private static Dictionary<long, EventHandler<CallbackQueryEventArgs>> usersCallbacks = new Dictionary<long, EventHandler<CallbackQueryEventArgs>>();

        public async Task GetAdministratorMenu(TelegramBotClient telegramBotClient, Message message, Message editMessage, string administratorStatus)
        {
            try
            {
            // Меню для администратора.
            InlineKeyboardMarkup adminInlineKeyboardMarkup = new InlineKeyboardMarkup(new[] {
                new[] { InlineKeyboardButton.WithCallbackData("📩 Пересылка") },
                new[] { InlineKeyboardButton.WithCallbackData("✅ Проверка") },
                new[] { InlineKeyboardButton.WithCallbackData("📋 Список отправителей") },
                new[] { InlineKeyboardButton.WithCallbackData("🚪 Выйти") }
            });

            // Другой вид клавиатуры для главного администратора (владельца).
            if (administratorStatus == "owner")
            {
                adminInlineKeyboardMarkup = new InlineKeyboardMarkup(new[] {
                    new[] { InlineKeyboardButton.WithCallbackData("📩 Пересылка") },
                    new[] { InlineKeyboardButton.WithCallbackData("✅ Проверка") },
                    new[] { InlineKeyboardButton.WithCallbackData("📋 Список отправителей") },
                    new[] { InlineKeyboardButton.WithCallbackData("📜 Список модераторов") },
                    new[] { InlineKeyboardButton.WithCallbackData("🚪 Выйти") }
                });
            }

            // Создание и отправка сообщения с меню.
            Message administratorMenuPanel;

            if (editMessage == null)
            {
                administratorMenuPanel = await telegramBotClient.SendTextMessageAsync(message.Chat.Id,
                "*Выберите одно из действий, которое хотите выполнить:*\n\n📩 *Пересылка* — управляйте пересылкой сообщений в канал — включайте и выключайте эту функцию по вашему усмотрению.\n\n✅ *Проверка* — настройте автоматическую проверку сообщений перед их пересылкой в канал — включайте или выключайте эту опцию.\n\n📋 *Список отправителей* — получите список отправителей сообщений за определенную дату.\n\n📜 *Список модераторов* — здесь вы можете управлять списком модераторов, добавлять или удалять их. _Доступно только для владельца._\n\n*Пожалуйста, нажмите на соответствующую кнопку ниже, чтобы продолжить.*",
                replyMarkup: adminInlineKeyboardMarkup, parseMode: ParseMode.Markdown);
            }
            else
            {
                administratorMenuPanel = await telegramBotClient.EditMessageTextAsync(editMessage.Chat.Id, editMessage.MessageId,
                "*Выберите одно из действий, которое хотите выполнить:*\n\n📩 *Пересылка* — управляйте пересылкой сообщений в канал — включайте и выключайте эту функцию по вашему усмотрению.\n\n✅ *Проверка* — настройте автоматическую проверку сообщений перед их пересылкой в канал — включайте или выключайте эту опцию.\n\n📋 *Список отправителей* — получите список отправителей сообщений за определенную дату.\n\n📜 *Список модераторов* — здесь вы можете управлять списком модераторов, добавлять или удалять их. _Доступно только для владельца._\n\n*Пожалуйста, нажмите на соответствующую кнопку ниже, чтобы продолжить.*",
                replyMarkup: adminInlineKeyboardMarkup, parseMode: ParseMode.Markdown);
            }
            
            EventHandler<CallbackQueryEventArgs> BotOnButtonClick = async (sender, callbackQueryEventArgs) =>
            {
                var callbackQueryMessage = callbackQueryEventArgs.CallbackQuery;

                // Удаляем всех других пользователей из обработчика другого пользователя.
                if (callbackQueryMessage.From.Id != message.From.Id)
                    return;

                // Класс для переключения функций (управление администратора).
                SwitchingSystemStatus switchingSystemStatus = new SwitchingSystemStatus();
                // Класс для выгрузки всех отправителей за конкретную дату.
                UnloadList unloadList = new UnloadList();
                // Класс для управления модераторами бота.
                AdministratorsController administratorsController = new AdministratorsController();

                switch (callbackQueryMessage.Data)
                {
                    case "📩 Пересылка":
                        await switchingSystemStatus.StatusController(telegramBotClient, callbackQueryMessage.Message, administratorMenuPanel, EnabledMessages, "Пересылка сообщений", administratorStatus);
                        break;
                    case "✅ Проверка":
                        await switchingSystemStatus.StatusController(telegramBotClient, callbackQueryMessage.Message, administratorMenuPanel, EnabledCheckMessages, "Проверка сообщений", administratorStatus);
                        break;
                    case "📋 Список отправителей":
                        if (administratorMenuPanel != null)
                            await telegramBotClient.DeleteMessageAsync(administratorMenuPanel.Chat.Id, administratorMenuPanel.MessageId);
                            
                        await unloadList.UnloadListSenders(telegramBotClient, message, "📅 Укажите дату в формате *DD.MM.YYYY*, за которую вы хотите получить список всех отправителей сообщений, или воспользуйтесь командой /today, чтобы показать список за сегодня.", administratorStatus);
                        break;
                    case "📜 Список модераторов":
                        await administratorsController.UnloadListAdministrators(telegramBotClient, message, administratorMenuPanel, administratorStatus);
                        break;
                    case "🚪 Выйти":
                        // Удаления администратора из списка активных в панели.
                        foreach (string administrator in NotLoveBot.Program.Program.ActiveAdministratorsId)
                        {
                            if (administrator == callbackQueryMessage.From.Id.ToString())
                            {
                                NotLoveBot.Program.Program.ActiveAdministratorsId.Remove(administrator);
                                await telegramBotClient.SendTextMessageAsync(callbackQueryMessage.Message.Chat.Id, "🚪 *Вы успешно вышли из панели администратора.* Чтобы вернуться, введите команду /controller. 🔄", parseMode: ParseMode.Markdown);

                                // Удаление обработчика событий для отдельного пользователя.
                                telegramBotClient.OnCallbackQuery -= usersCallbacks[message.From.Id];
                                usersCallbacks.Remove(message.From.Id);
                                break;
                            }
                        }
                        break;
                }
            };

            // Если обработчик с ID пользователя уже создан, то он удаляется.
            if (usersCallbacks.ContainsKey(message.From.Id))
            {
                telegramBotClient.OnCallbackQuery -= usersCallbacks[message.From.Id];
                usersCallbacks[message.From.Id] = BotOnButtonClick;
            }
            else
                usersCallbacks.Add(message.From.Id, BotOnButtonClick);

            telegramBotClient.OnCallbackQuery += BotOnButtonClick;
        }
        catch(Exception exception)
        {
            Console.WriteLine(exception);
        }
    }
    }
}