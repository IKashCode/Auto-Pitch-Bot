using System;
using NotLoveBot.Program;
using Telegram.Bot.Types;

namespace NotLoveBot.AdministrationPanelController
{
    public class KeyboardMessagesController
    {
        // Метод, который удаляет все сообщения с клавиатурой.
        public async Task DeleteKeyboardMessages(long chatId)
        {
            if (ConnectionController.KeyboardMessages.ContainsKey(chatId))
            {
                foreach(var messageId in ConnectionController.KeyboardMessages[chatId])
                {
                    try{
                        await ConnectionController._telegramBotClient.DeleteMessageAsync(chatId, messageId);
                    } catch (Exception exception) { Console.WriteLine("Message not found!"); }
                }

                ConnectionController.KeyboardMessages[chatId].Clear();
            }
        }

        // Метод, который добавляет в список все сообщения с клавиатурой у разных пользователей.
        public async Task AddKeyboardMessages(Message message)
        {
            if (!ConnectionController.KeyboardMessages.ContainsKey(message.Chat.Id))
                ConnectionController.KeyboardMessages[message.Chat.Id] = new List<int>();

            ConnectionController.KeyboardMessages[message.Chat.Id].Add(message.MessageId);
        }
    }
}