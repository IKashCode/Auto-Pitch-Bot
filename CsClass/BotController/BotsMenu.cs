using System;
using NotLoveBot.AdministrationPanelController;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace NotLoveBot.BotController
{
    public class BotsMenu
    {
        // Класс для управления сообщениями с меню.
        private static KeyboardMessagesController _keyboardMessagesController = new KeyboardMessagesController();
        public static Dictionary<long, int> lastMenuMessageIds = new Dictionary<long, int>();
        private static CreatedBotController _createdBotController = new CreatedBotController();

        // Сохранение callback пользователей.
        private static Dictionary<long, EventHandler<CallbackQueryEventArgs>> usersCallbacks = new Dictionary<long, EventHandler<CallbackQueryEventArgs>>();
        
        public async Task GetBotsMenu(TelegramBotClient telegramBotClient, Message message, Message editMessage)
        {
            try {
            InlineKeyboardMarkup botsInlineKeyboardMarkup = new InlineKeyboardMarkup(new[] {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🔨 Создать"),
                    InlineKeyboardButton.WithCallbackData("🚫 Отключить"),
                },
                new[] { InlineKeyboardButton.WithCallbackData("⚙️ Параметры ваших ботов") }
            });

            // Проверка на тип сообщения.
            Message? botsMenu;
            if (editMessage == null)
            {
                botsMenu = await telegramBotClient.SendTextMessageAsync(message.Chat.Id,
                "*🛠️ Выберите одно из следующих действий:*\n\n⚙️ *Параметры ваших ботов* — здесь вы можете управлять созданными ботами.\n🔨 *Создать* — подключите вашего бота в выбранную группу или канал.\n🚫 *Отключить* — отключение созданных вами ботов.\n\n*Нажмите на соответствующую кнопку ниже, чтобы продолжить.*",
                replyMarkup: botsInlineKeyboardMarkup, parseMode: ParseMode.Markdown);
            }
            else
            {
                botsMenu = await telegramBotClient.EditMessageTextAsync(editMessage.Chat.Id, editMessage.MessageId,
                "*🛠️ Выберите одно из следующих действий:*\n\n⚙️ *Параметры ваших ботов* — здесь вы можете управлять созданными ботами.\n🔨 *Создать* — подключите вашего бота в выбранную группу или канал.\n🚫 *Отключить* — отключение созданных вами ботов.\n\n*Нажмите на соответствующую кнопку ниже, чтобы продолжить.*",
                replyMarkup: botsInlineKeyboardMarkup, parseMode: ParseMode.Markdown);
            }

            // Добавление в общий список сообщений с меню.
            await _keyboardMessagesController.AddKeyboardMessages(botsMenu);

            EventHandler<CallbackQueryEventArgs> BotOnButtonClick = async (sender, callbackQueryEventArgs) =>
            {
                var callbackQueryMessage = callbackQueryEventArgs.CallbackQuery;

                if (callbackQueryMessage.From.Id != message.From.Id)
                    return;

                switch (callbackQueryMessage.Data)
                {
                    case "🔨 Создать":
                        await _createdBotController.CreateBot(telegramBotClient, message, botsMenu, "🔑 Для подключения бота вам необходимо воспользоваться *официальным ботом Telegram для создания новых ботов — @BotFather*.\n● После создания бота вы получите *токен*, который нужно *скопировать и отправить в этот чат* для активации функционала.");
                        break;
                    case "🚫 Отключить":
                        await _createdBotController.DeleteBot(telegramBotClient, message, botsMenu);
                        break;
                    case "⚙️ Параметры ваших ботов":
                        await _createdBotController.BotParameters(telegramBotClient, message, botsMenu);
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
        } catch(Exception exception) { Console.WriteLine(exception); }
        }
    }
}