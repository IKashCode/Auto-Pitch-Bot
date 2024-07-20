using System;
using System.Text;
using Telegram.Bot.Types;

namespace NotLoveBot.CheckRules
{
    public class CheckMessage
    {
        // Все символы, которые запрещены.
        private string[] _forbiddenSymbols
        = {
            "василенко", "востров", "бауков", "грешнов",
            "адм", "adm", "admin", "канал", "ваксаенелюбят", "тгк",
            "/", "http", "t.me",
            "🍔", "❗️",
            "卐", "卍",
        };

        private char[] _punctuationSymbols = 
        {
            '!', ' ', ',', '.', ':', ';', '?', '-', '_', '(', ')', '[', ']', '{', '}', '\'', '\"', '\\', '@', '#', '$', '%', '^', '&', '*', '+', '=', '<', '>', '|', '`', '~',
            '\t', '\n', '\r', '«', '»', '…', '–', '—', '•', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
            '¡', '¢', '£', '¤', '¥', '¦', '§', '¨', '©', 'ª', '«', '¬', '®', '¯', '°', '±', '²', '³', '´', 'µ', '¶', '·', '¸', '¹', 'º', '»', '¼', '½', '¾', '¿', 
            'À', 'Á', 'Â', 'Ã', 'Ä', 'Å', 'Æ', 'Ç', 'È', 'É', 'Ê', 'Ë', 'Ì', 'Í', 'Î', 'Ï', 'Ð', 'Ñ', 'Ò', 'Ó', 'Ô', 'Õ', 'Ö', '×', 'Ø', 'Ù', 'Ú', 'Û', 'Ü', 'Ý', 'Þ', 'ß',
            'à', 'á', 'â', 'ã', 'ä', 'å', 'æ', 'ç', 'è', 'é', 'ê', 'ë', 'ì', 'í', 'î', 'ï', 'ð', 'ñ', 'ò', 'ó', 'ô', 'õ', 'ö', '÷', 'ø', 'ù', 'ú', 'û', 'ü', 'ý', 'þ', 'ÿ'
        };

        public async Task<string> CheckForbiddenSymbols(string message, bool enabled)
        {
            string checkResult = $"✨ *Спасибо за ваше сообщение!* ⏳ Оно будет отправлено в канал через `30 секунд`. 💬 Не переживайте, все полностью анонимно!";

            if (enabled == true)
            {
                if (await CheckLength(message, 4) == false)
                {
                    checkResult = $"*🚫 Для отправки сообщения необходимо ввести не менее* `4 символов`. ✍️ Пожалуйста, добавьте больше текста.";
                    return checkResult;
                }

                message = await GetSolidMessage(message);

                foreach (string forbiddenSymbol in _forbiddenSymbols)
                {
                    if (message.Contains(forbiddenSymbol))
                    {
                        checkResult = $"🚫 *Ваше сообщение не может быть опубликовано из-за содержания запрещенных слов или ссылок!*";
                        break;
                    }
                }
            }
            
            return checkResult;
        }

        // Метод для получения текста сообщения без пробелов и лишних символов.
        public async Task<string> GetSolidMessage(string message)
        {
            StringBuilder resultText = new StringBuilder();

            foreach (char symbol in message)
            {
                if (Array.IndexOf(_punctuationSymbols, symbol) == -1)
                    resultText.Append(symbol);
            }

            return resultText.ToString().ToLower();;
        }

        // Метод для проверки на минимальное колличиство символов.
        public async Task<bool> CheckLength(string message, int minimum)
        {
            if (message.Length < minimum)
                return false;

            return true;
        }
    }
}