using System;

namespace TelegramAutomation.Exceptions
{
    public class ChromeDriverException : ChromeException
    {
        public ChromeDriverException(string message) : base(message) { }
        public ChromeDriverException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
} 