using System;

namespace TelegramAutomation.Exceptions
{
    public class ChromeException : TelegramAutomationException
    {
        public ChromeException(string message) : base(message) { }
        public ChromeException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
} 