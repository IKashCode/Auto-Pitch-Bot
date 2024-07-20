using System;
using NotLoveBot.DataBaseProcess;
using NotLoveBot.Models;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace NotLoveBot.AdministrationPanelController
{
    class AdministratorsController
    {
        private AdministratorMenu _administratorMenu = new AdministratorMenu();

        private SetDataProcessing _setDataProcessing = new SetDataProcessing();
        private GetDataProcessing _getDataProcessing = new GetDataProcessing();
        
        public async Task UnloadListAdministrators(TelegramBotClient telegramBotClient, Message message, Message editMessage, string administratorStatus)
        {
            try{
            List<AdministrationModel> administrationModels = await _getDataProcessing.GetListAdministrators();
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
                new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад") }
            });

            Message administratorsList = await telegramBotClient.EditMessageTextAsync(editMessage.Chat.Id, editMessage.MessageId, administratorsListText, replyMarkup: ownerInlineKeyboardMarkup, parseMode: ParseMode.Markdown);

            telegramBotClient.OnCallbackQuery += BotOnButtonClick;
            async void BotOnButtonClick(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
            {
                var callbackQueryMessage = callbackQueryEventArgs.CallbackQuery;

                switch (callbackQueryMessage.Data)
                {
                    case "➕ Добавить":
                        await Controller(telegramBotClient, message, administratorStatus, "добавить");
                        break;
                    case "➖ Удалить":
                        await Controller(telegramBotClient, message, administratorStatus, "удалить");
                        break;
                    case "⬅️ Назад":
                        await _administratorMenu.GetAdministratorMenu(telegramBotClient, message, administratorsList, administratorStatus);
                        break;
                }

                telegramBotClient.OnCallbackQuery -= BotOnButtonClick;
            }}catch(Exception){}
        }

        private async Task Controller(TelegramBotClient telegramBotClient, Message message, string administratorStatus, string action)
        {
            try {
            // Изменения текста под нужное действие.
            string actionText = "❗️ Отправьте *username* того, кого вы хотите добавить в модераторы.";
            if (action == "удалить")
                actionText = $"❗️ Отправьте имя администратора, *указанного в списке*, чтобы удалить его из модераторов.";

            await telegramBotClient.SendTextMessageAsync(message.Chat.Id, actionText, parseMode: ParseMode.Markdown);

            telegramBotClient.OnMessage += BotOnMessageReceived;
            async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
            {
                Message replyMessage = messageEventArgs.Message;
                string username = replyMessage.Text;

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

                        await _administratorMenu.GetAdministratorMenu(telegramBotClient, message, null, administratorStatus);
                        break;
                    case "удалить":
                        string resultText = "✅ *Администратор был успешно снят с должности!*";

                        // Проверка на наличие администратора в списке.
                        if (await _setDataProcessing.ExistenceCheck("Administrators_test_394832", "name", username) == true)
                        {
                            List<AdministrationModel> administrationModels = await _getDataProcessing.GetListAdministrators();
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
                                await _setDataProcessing.SetCreateRequest($"DELETE FROM Administrators_test_394832 WHERE name = '{username}'");
                        }
                        else
                            resultText = "🤔 Простите, администратор не обнаружен в списке. Возможно, он уже был удалён.";

                            await telegramBotClient.SendTextMessageAsync(message.Chat.Id, resultText, parseMode: ParseMode.Markdown);
                            await _administratorMenu.GetAdministratorMenu(telegramBotClient, message, null, administratorStatus);
                        break;
                }

                telegramBotClient.OnMessage -= BotOnMessageReceived;
            }}catch(Exception){}
        }
    }
}