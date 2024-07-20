using System;
using System.Globalization;
using NotLoveBot.DataBaseProcess;
using NotLoveBot.Models;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace NotLoveBot.AdministrationPanelController
{
    public class UnloadList
    {
        private AdministratorMenu _administratorMenu = new AdministratorMenu();
        private GetDataProcessing _getDataProcessing = new GetDataProcessing();

        private string _htmlContent = @"<!DOCTYPE html><html lang=""ru""><head><meta charset=""UTF-8""><meta name=""viewport"", http-equiv=""Content-type"", content=""width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=0""><title>SENDERS LIST</title><style>@import url('https://fonts.googleapis.com/css2?family=Roboto:ital,wght@0,100;0,300;0,400;0,500;0,700;0,900;1,100;1,300;1,400;1,500;1,700;1,900&display=swap');.roboto-thin{font-family:""Roboto"",sans-serif;font-weight:100;font-style:normal;}.roboto-light{font-family:""Roboto"",sans-serif;font-weight:300;font-style:normal;}.roboto-regular{font-family:""Roboto"",sans-serif;font-weight:400;font-style:normal;}.roboto-medium{font-family:""Roboto"",sans-serif;font-weight:500;font-style:normal;}.roboto-bold{font-family:""Roboto"",sans-serif;font-weight:700;font-style:normal;}.roboto-black{font-family:""Roboto"",sans-serif;font-weight:900;font-style:normal;}.roboto-thin-italic{font-family:""Roboto"",sans-serif;font-weight:100;font-style:italic;}.roboto-light-italic{font-family:""Roboto"",sans-serif;font-weight:300;font-style:italic;}.roboto-regular-italic{font-family:""Roboto"",sans-serif;font-weight:400;font-style:italic;}.roboto-medium-italic{font-family:""Roboto"",sans-serif;font-weight:500;font-style:italic;}.roboto-bold-italic{font-family:""Roboto"",sans-serif;font-weight:700;font-style:italic;}.roboto-black-italic{font-family:""Roboto"",sans-serif;font-weight:900;font-style:italic;}body{display:flex;flex-wrap:wrap;justify-content:center;background-color:#010007;}nav{width:500px;min-width:300px;height:100%;display:flex;flex-wrap:wrap;justify-content:center;}.pageName{color:white;padding:10px 10px 10px 10px;font-weight:500;text-transform:uppercase;font-family:""Roboto"";font-size:25px;}.messageBlock{width:100%;min-width:300px;background:transparent;border:1px solid #1e2022;padding:10px 10px 10px 10px;border-radius:10px;margin-bottom:5px;}.textBlock{background:#212121;padding:10px 10px 10px 10px;border-radius:10px;}.informationBlock{padding:0;left:0;right:0;margin-top:4px;}.usernameBlock{vertical-align:top;display:inline-block;top:0;margin:0;padding:0;right:0;left:0;box-sizing:border-box;}.usernameLink{color:white;text-decoration:none;}.usernameText{display:inline-block;background:#283149;padding:3px 6px 3px 6px;border-radius:5px;color:white;font-family:""Roboto"";font-weight:normal;font-size:18px;transition:200ms;}.usernameText:hover{background-color:#5e63b6;}.userId{left:0;right:0;margin-left:3px;vertical-align:top;display:inline-block;background:#212121;padding:3px 6px 3px 6px;border-radius:5px;color:white;font-family:""Roboto"";font-weight:normal;font-size:18px;box-sizing:border-box;}.messageTypeText{display:block;color:white;font-family:""Roboto"";font-weight:normal;font-size:14px;padding:5px 0px 5px 0px;color:rgba(224,224,224,0.9);}.messageType{display:block;color:white;font-family:""Roboto"";font-weight:normal;text-transform:uppercase;font-size:17px;background:#21212188;color:white;padding:10px 10px 10px 10px;border-radius:10px;}p{word-wrap:break-word;font-family:""Roboto"";font-weight:normal;font-size:17px;color:white;padding:0;margin:0;top:0;}</style></head><body><nav>";

        public async Task UnloadListSenders(TelegramBotClient telegramBotClient, Message message, string dateEntryMessage, string administratorStatus)
        {
            try {
            Message indicateDateMessage = await telegramBotClient.SendTextMessageAsync(message.Chat.Id, dateEntryMessage, parseMode: ParseMode.Markdown);
            long id = message.From.Id;

            telegramBotClient.OnMessage += BotOnMessageReceived;
            async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
            {
                Message replyMessage = messageEventArgs.Message;
                string dataMessage = replyMessage.Text;

                if (replyMessage.From.Id != id)
                    return;

                // Условие, которое позволяет при вызове панели администрации не присылать лишних сообщений.
                if (dataMessage == "/controller")
                {
                    telegramBotClient.OnMessage -= BotOnMessageReceived;
                    return;
                }

                // Проверка даты на валидность.
                if (DateTime.TryParseExact(dataMessage, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) == true || dataMessage == "/today")
                {
                    if (dataMessage == "/today")
                        dataMessage = DateTime.Today.ToString("dd.MM.yyyy");

                    List<SenderModel> senderModels = await _getDataProcessing.GetListSenders(dataMessage);

                    if (senderModels.Count == 0)
                        await telegramBotClient.SendTextMessageAsync(message.Chat.Id, "❌ В базе данных сообщений за указанную дату не обнаружено!", parseMode: ParseMode.Markdown);
                    else
                    {
                        try
                        {
                            _htmlContent += $"<div class=\"pageName\">Список сообщений за {dataMessage}:</div>";

                            foreach (var senderModel in senderModels)
                            {
                                try
                                {
                                    if (senderModel.Type == "📝 текстовое-сообщение")
                                        _htmlContent += $"<div class=\"messageBlock\"><div class=\"textBlock\"><p>{senderModel.Status} | {senderModel.Message}</p></div><div class=\"messageTypeText\">{senderModel.Type}</div><div class=\"informationBlock\"><div class=\"usernameBlock\"><a class=\"usernameLink\" href=\"https://t.me/{senderModel.User}\"><div class=\"usernameText\">@{senderModel.User}</div></a></div><div class=\"userId\">{senderModel.Id}</div></div></div>";
                                    else
                                        _htmlContent += $"<div class=\"messageBlock\"><div class=\"messageType\">{senderModel.Type}</div><div class=\"informationBlock\"><div class=\"usernameBlock\"><a class=\"usernameLink\" href=\"https://t.me/{senderModel.User}\"><div class=\"usernameText\">@{senderModel.User}</div></a></div><div class=\"userId\">{senderModel.Id}</div></div></div>";
                                } catch(Exception exception){ Console.WriteLine(exception); }
                            }

                            _htmlContent += "</nav></body></html>";
                            
                            System.IO.File.WriteAllText("HtmlFiles/Список отправителей.html", _htmlContent);

                            using (FileStream fileStream = new FileStream("HtmlFiles/Список отправителей.html", FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                InputOnlineFile inputOnlineFile = new InputOnlineFile(fileStream, "HtmlFiles/Список отправителей.html");
                                await telegramBotClient.SendDocumentAsync(replyMessage.Chat.Id, inputOnlineFile);
                            }
                        } catch(Exception exception){ Console.WriteLine(exception); }
                    }

                    await _administratorMenu.GetAdministratorMenu(telegramBotClient, message, null, administratorStatus);
                }
                else
                    await UnloadListSenders(telegramBotClient, message, "⚠️ *Ошибка в формате даты!* Используйте *DD.MM.YYYY* и попробуйте ввести дату еще раз, или воспользуйтесь командой /today, чтобы показать список за сегодня.", administratorStatus);

                telegramBotClient.OnMessage -= BotOnMessageReceived;
            };} catch(Exception exception) { Console.WriteLine(exception); }
        }
    }
}