using System;
using Telegram.Bot;

namespace NotLoveBot.Models
{
    public class ConnectionBotModel
    {
        public string? BotName { get; set; }
        public string? Token { get; set; }
        public string? ChannelName { get; set; }
        public string? ChannelId { get; set; }
        public string? UserId { get; set; }
        public TelegramBotClient? BotClient { get; set; }
        public int? Delay { get; set; }
        public string? ReplyMessageText { get; set; }
        public string? StartMessageText { get; set; }
        public int? FilterStatus { get; set; }
    }
}