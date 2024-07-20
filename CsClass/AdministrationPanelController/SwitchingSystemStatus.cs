using System;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using NotLoveBot.AdministrationPanelController;
using Telegram.Bot.Types.Enums;

namespace NotLoveBot.AdministrationPanelController
{
    public class SwitchingSystemStatus
    {
        private AdministratorMenu _administratorMenu = new AdministratorMenu();

        public async Task StatusController(TelegramBotClient telegramBotClient, Message message, Message editMessage, bool statusSystem, string functionName, string administratorStatus)
        {
            try {
            // Создание панели переключения и ее текста.
            string actionName = "⏹️ Выключить",
            statusText = "в данный момент *включена*. Вы можете выключить её, используя кнопки ниже.",
            resultMessage = $"❌ *{functionName} выключена!*";

            if (statusSystem == false)
            {
                actionName = "▶️ Включить";
                statusText = "в данный момент *выключена*. Вы можете включить её, используя кнопки ниже.";
            }

            InlineKeyboardMarkup inlineKeyboardMarkup = new InlineKeyboardMarkup(new[] {
                InlineKeyboardButton.WithCallbackData(actionName),
                InlineKeyboardButton.WithCallbackData("⬅️ Назад")
            });

            Message statusControllerPanel = await telegramBotClient.EditMessageTextAsync(editMessage.Chat.Id, editMessage.MessageId, $"⬇️ *{functionName}* {statusText}", replyMarkup: inlineKeyboardMarkup, parseMode: ParseMode.Markdown);

            telegramBotClient.OnCallbackQuery += BotOnButtonClick;

            async void BotOnButtonClick(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
            {
                var callbackQueryMessage = callbackQueryEventArgs.CallbackQuery;
                var telegramBotClient = (TelegramBotClient)sender;

                // Измненение статуса параметра.
                if (callbackQueryMessage.Data == "⏹️ Выключить" || callbackQueryMessage.Data == "▶️ Включить")
                {
                    statusSystem = !statusSystem;

                    if (statusSystem == true)
                        resultMessage = $"✅ *{functionName} включена!*";

                    // Сообщение о статусе функционала после переключения.
                    statusControllerPanel = await telegramBotClient.EditMessageTextAsync(statusControllerPanel.Chat.Id, statusControllerPanel.MessageId, resultMessage, parseMode: ParseMode.Markdown);
                    await Task.Delay(2000);

                    await _administratorMenu.GetAdministratorMenu(telegramBotClient, message, statusControllerPanel, administratorStatus);

                    // Приравнивания статуса переменной, имя которой указано в переменной "functionalName" (для исправления бага).
                    switch (functionName)
                    {
                        case "Пересылка сообщений":
                            NotLoveBot.AdministrationPanelController.AdministratorMenu.EnabledMessages = statusSystem;
                            break;
                        case "Проверка сообщений":
                            NotLoveBot.AdministrationPanelController.AdministratorMenu.EnabledCheckMessages = statusSystem;
                            break;
                        default:
                            Console.WriteLine("Error: You must specify the name of the controlled functionality!");
                            break;
                    }
                }
                else
                    await _administratorMenu.GetAdministratorMenu(telegramBotClient, message, statusControllerPanel, administratorStatus);

                telegramBotClient.OnCallbackQuery -= BotOnButtonClick;
            }
            }catch(Exception){}
        }
    }
}