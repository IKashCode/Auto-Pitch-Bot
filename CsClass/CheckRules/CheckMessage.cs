using System;
using System.Text.RegularExpressions;

namespace NotLoveBot.CheckRules
{
    public class CheckMessage
    {
        private string _pattern = @"(http|https|ftp):\/\/[^\s/$.?#].[^\s]*";
        public async Task<string> CheckLink(string replyMessage, string message, bool enabled)
        {
            if (enabled == true)
            {
                Regex _regex = new Regex(_pattern, RegexOptions.IgnoreCase);
                MatchCollection matchCollection = _regex.Matches(message);
                
                if (matchCollection.Count > 0)
                    return $"🚫 *Сообщение не может быть отправлено!* Публикация ссылок запрещена администратором.";
            }
            
            return $"{replyMessage}\n\n{NotLoveBot.Program.Program.Ads}";
        }
    }
}