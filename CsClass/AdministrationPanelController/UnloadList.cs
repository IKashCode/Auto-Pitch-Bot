using System;
using System.Globalization;
using NotLoveBot.DataBaseProcess;
using NotLoveBot.Interface;
using NotLoveBot.Models;
using NotLoveBot.Program;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace NotLoveBot.AdministrationPanelController
{
    public class UnloadList
    {
        // Класс для работы с базой данных.
        private IGetDataProcessing<SenderModel> _getSendersData = new GetSendersData();

        private string _htmlContent = "<!DOCTYPE html><html><head lang=\"ru\">";

        public async Task UnloadListSenders(TelegramBotClient telegramBotClient, Message message, string dateEntryMessage, string administratorStatus, string botName)
        {
            try {
            Message indicateDateMessage;
            indicateDateMessage = await telegramBotClient.SendTextMessageAsync(message.Chat.Id, dateEntryMessage, parseMode: ParseMode.Markdown);

            telegramBotClient.OnMessage += BotOnMessageReceived;
            async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
            {
                Message replyMessage = messageEventArgs.Message;
                string dataMessage = replyMessage.Text;
                
                if (replyMessage.From.Id != message.From.Id)
                    return;
                // Проверка на наличие команд.
                foreach (string command in ConnectionController.commands)
                {
                    if (dataMessage == command)
                    {
                        telegramBotClient.OnMessage -= BotOnMessageReceived;
                        return;
                    }
                }

                // Проверка даты на валидность.
                if (DateTime.TryParseExact(dataMessage, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) == true || dataMessage == "/today" && replyMessage.Type == MessageType.Text)
                {
                    if (dataMessage == "/today")
                        dataMessage = DateTime.Today.ToString("dd.MM.yyyy");

                    List<SenderModel> senderModels = await _getSendersData.GetList(dataMessage, botName);

                    if (senderModels.Count == 0)
                        await telegramBotClient.SendTextMessageAsync(message.Chat.Id, "❌ В базе данных сообщений за указанную дату не обнаружено!", parseMode: ParseMode.Markdown);
                    else
                    {
                        try
                        {
                            _htmlContent += $"<title>{dataMessage}</title>" + "<meta charset=\"UTF-8\"><meta name=\"viewport\" http-equiv=\"Content-type\" content=\"width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=0\"><style>@import url('https://fonts.googleapis.com/css2?family=Roboto:ital,wght@0,100;0,300;0,400;0,500;0,700;0,900;1,100;1,300;1,400;1,500;1,700;1,900&display=swap');.roboto-thin { font-family:'Roboto',sans-serif;font-weight:100;font-style:normal; }.roboto-light { font-family:'Roboto',sans-serif;font-weight:300;font-style:normal; }.roboto-regular { font-family:'Roboto',sans-serif;font-weight:400;font-style:normal; }.roboto-medium { font-family:'Roboto',sans-serif;font-weight:500;font-style:normal; }.roboto-bold { font-family:'Roboto',sans-serif;font-weight:700;font-style:normal; }.roboto-black { font-family:'Roboto',sans-serif;font-weight:900;font-style:normal; }.roboto-thin-italic { font-family:'Roboto',sans-serif;font-weight:100;font-style:italic; }.roboto-light-italic { font-family:'Roboto',sans-serif;font-weight:300;font-style:italic; }.roboto-regular-italic { font-family:'Roboto',sans-serif;font-weight:400;font-style:italic; }.roboto-medium-italic { font-family:'Roboto',sans-serif;font-weight:500;font-style:italic; }.roboto-bold-italic { font-family:'Roboto',sans-serif;font-weight:700;font-style:italic; }.roboto-black-italic { font-family:'Roboto',sans-serif;font-weight:900;font-style:italic; }body{ background-color: #151515; display: flex; flex-wrap: wrap; justify-content: center; margin: 0; padding: 0; }nav{ width: 100%; height: auto; margin-bottom: 40px; max-width: 500px; padding: 0; display: flex; flex-wrap: wrap; justify-content: center; margin-top: 50px; }footer{ position: fixed; bottom: 0; left: 50%; width: 100%; max-width: 500px; background-color: #303030; color: white; text-align: center; padding: 10px; transform: translateX(-50%); box-sizing: border-box; border-radius: 5px 5px 0px 0px; }footer a{ color: white; font-family: 'Roboto'; text-decoration: none; font-size: 16px; }.searchBar{ position: fixed; font-family: 'Roboto'; color: white; background-color: #383838; border: 0; outline: 0; padding: 10px 10px 10px 20px; width: 100%; max-width: 500px; font-size: 16px; margin-bottom: 1px; transition: 0.3s; box-sizing: border-box; margin-top: 5px; border-radius: 20px; }@media (max-width: 600px){ .searchBar{ max-width: 98%; padding: 7px 10px 7px 20px; }nav{ margin-top: 43px; }footer{ max-width: 100%; border-radius: 0px 0px 0px 0px; }}.searchBar:focus{ background-color: #494949; transition: 0.3s; }.noResults{ font-family: 'Roboto'; color: rgba(255, 255, 255, 0.5); text-align: center; }.dateText{ font-family: 'Roboto'; font-size: 16px; color: rgba(255, 255, 255, 0.5); padding: 5px 10px 5px 10px; align-items: center; display: flex; justify-content: space-between; }.messageFile{ font-family: 'Roboto'; color: white; background-color: #232323; border: 0; outline: 0; padding: 10px; text-align: left; cursor: pointer; font-size: 16px; transition: background-color 0.3s; margin-bottom: 1px; align-items: center; display: flex; justify-content: space-between; width: 100%; }.messageFile:hover .time{ color: white; }.messageFile:hover{ background-color: #2e2e2e; }.messageButton{ font-family: 'Roboto'; color: white; background-color: #232323; border: 0; outline: 0; padding: 10px; text-align: left; cursor: pointer; width: 100%; font-size: 16px; transition: background-color 0.3s; margin-bottom: 1px; align-items: center; display: flex; justify-content: space-between; }.messageButton:hover .time{ color: white; }.messageButton:hover{ background-color: #2e2e2e; }.time{ transition: color 0.1s; margin-right: 5px; color: #ededed; }.messageContent{ overflow-wrap: break-word; max-height: 0px; font-family: 'Roboto'; color: #f4f4f4; background-color: #232323; padding: 0px 10px; margin: 0; overflow-x: auto; transition: max-height 0.3s ease-out; overflow-y: hidden; width: 100%; }.messageContent p{ padding: 0; margin: 0; max-height: 500px; overflow-y: auto; }.userInformation{ margin-top: 5px; }.usernameLink{ font-size: 15px; padding: 0; margin: 0; text-decoration: none; border: 0; color: white; float: left; margin-right: 3px; }.usernameLink:hover div{ background-color: #7880be; }.usernameLink div{ white-space: nowrap; padding: 3px 3px; border-radius: 3px; width: min-content; background-color: #494966; transition: background-color 0.3s; }.idLink{ font-size: 15px; padding: 0; margin: 0; text-decoration: none; border: 0; color: white; float: left; margin-right: 3px; }.idLink div{ white-space: nowrap; padding: 3px 3px; border-radius: 3px; width: min-content; background-color: #434c5e; transition: background-color 0.3s; }.idLink:hover div{ background-color: #94acc5; }.userInformationBlock{ white-space: nowrap; font-size: 15px; color: white; float: left; margin-right: 3px; padding: 3px 3px; border-radius: 3px; width: min-content; background-color: #30364a; transition: background-color 0.3s; }</style></head><body><input class='searchBar' id='searchTime' type='text' placeholder='Введите время' oninput='searchByTime()'><nav id='messageList'>";

                            foreach (var senderModel in senderModels)
                            {
                                try
                                {
                                    // Метод для получения имени.
                                    string name = await GetName(senderModel.FirstName, senderModel.LastName);
                                    string titleMessage = null;
                                    bool isMessageText = true;

                                    if (senderModel.Status != "🚫")
                                    {
                                        // Получение имени в зависимости от значения.
                                        if (senderModel.Message != "текст отсутсвует" && senderModel.Type == "📝 текстовое-сообщение")
                                            titleMessage = senderModel.Message.Length > 30 ? $"{senderModel.Message.Substring(0, 30)}..." : $"{senderModel.Message}...";
                                        else
                                        {
                                            if (senderModel.Message == "текст отсутсвует" && senderModel.Type != "📝 текстовое-сообщение")
                                            {
                                                _htmlContent += $"<button class='messageButton'><span>{senderModel.Type}...</span><span class='time'>{senderModel.Time}</span></button><div class='messageContent'><div><a class='usernameLink' href='https://t.me/{senderModel.User}'><div>@{senderModel.User}</div></a><a class='idLink' href='tg://openmessage?user_id={senderModel.Id}'><div>{senderModel.Id}</div></a><div class='userInformationBlock'>{name}</div></div></div>";
                                                isMessageText = false;
                                            }
                                            else
                                                titleMessage = $"{senderModel.Type}...";
                                        }

                                        if (isMessageText == true)
                                            _htmlContent += $"<button class='messageButton'><span>{titleMessage}</span><span class='time'>{senderModel.Time}</span></button><div class='messageContent'><p>{senderModel.Message}</p><div class='userInformation'><a class='usernameLink' href='https://t.me/{senderModel.User}'><div>@{senderModel.User}</div></a><a class='idLink' href='tg://openmessage?user_id={senderModel.Id}'><div>{senderModel.Id}</div></a><div class='userInformationBlock'>{name}</div></div></div>";
                                    }
                                } catch(Exception exception){ Console.WriteLine(exception); }
                            }

                            _htmlContent += "</nav><footer><a href='https://t.me/AutoPitchBot'>Создано с помощью @AutoPitchBot</a></footer></body><script>document.addEventListener('DOMContentLoaded', () => { const buttons = document.querySelectorAll('.messageButton'); const contents = document.querySelectorAll('.messageContent'); buttons.forEach(button => { button.addEventListener('click', () => { const messageContent = button.nextElementSibling; contents.forEach(content => { if (content !== messageContent) { content.style.maxHeight = null; content.style.padding = '0px 10px'; } }); if (messageContent.style.maxHeight) { messageContent.style.maxHeight = null; messageContent.style.padding = '0px 10px'; } else { messageContent.style.maxHeight = messageContent.scrollHeight + 'px'; messageContent.style.padding = '10px 10px'; } }); }); }); function searchByTime() { const input = document.getElementById('searchTime').value.trim(); const messageList = document.getElementById('messageList'); const messageButtons = messageList.querySelectorAll('.messageButton, .messageFile'); let found = false; const noResults = document.querySelector('.noResults'); if (noResults) noResults.remove(); messageButtons.forEach(messageButton => { const timeSpan = messageButton.querySelector('.time'); const time = timeSpan.textContent; if (time.includes(input)) { messageButton.style.display = ''; messageButton.nextElementSibling.style.display = ''; found = true; } else { messageButton.style.display = 'none'; messageButton.nextElementSibling.style.display = 'none'; } }); if (!found) { const noResults = document.createElement('div'); noResults.className = 'noResults'; noResults.textContent = 'По вашему запросу ничего не найдено'; messageList.appendChild(noResults); } }</script></html>";
                            
                            // Создание временного файла.
                            string tempFilePatch = Path.Combine(Path.GetTempPath(), $"Отправители_{dataMessage}_{botName}.html");
                            System.IO.File.WriteAllText(tempFilePatch, _htmlContent);

                            using (FileStream fileStream = new FileStream(tempFilePatch, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                InputOnlineFile inputOnlineFile = new InputOnlineFile(fileStream, $"Отправители_{dataMessage}_{botName}.html");
                                await telegramBotClient.SendDocumentAsync(replyMessage.Chat.Id, inputOnlineFile);
                            }

                            // Удаление временного файла.
                            if (System.IO.File.Exists(tempFilePatch))
                                System.IO.File.Delete(tempFilePatch);
                        } catch(Exception exception){ Console.WriteLine(exception); }
                    }

                    AdministratorMenu administratorMenu = new AdministratorMenu();
                    await administratorMenu.GetAdministratorMenu(telegramBotClient, message, null, administratorStatus, botName);
                }
                else
                    await UnloadListSenders(telegramBotClient, message, "⚠️ *Ошибка в формате даты!* Используйте *DD.MM.YYYY* и попробуйте ввести дату еще раз, или воспользуйтесь командой /today, чтобы показать список за сегодня.", administratorStatus, botName);

                telegramBotClient.OnMessage -= BotOnMessageReceived;
            };} catch(Exception exception) { Console.WriteLine(exception); }
        }

        private async Task<string> GetName(string firstName, string lastName)
        {
            string[,] dataUserName = 
            { { firstName, lastName }, 
            { "username отсутсвует", "LastName отсутсвует" } };

            for (int index = 0; index < dataUserName.GetLength(1); index++)
            {
                if (dataUserName[0, index] == dataUserName[1, index])
                    dataUserName[0, index] = null;
            }

            if (dataUserName[0, 0] == null && dataUserName[0, 1] == null)
                return "Имя отсутсвует";

            return $"{dataUserName[0, 0]} {dataUserName[0, 1]}";
        }
    }
}