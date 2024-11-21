using System;

namespace TelegramAutomation.Exceptions
{
    public class ChromeException : TelegramAutomationException
    {
        public ChromeException(string message, string errorCode) 
            : base(message, errorCode) { }

        public ChromeException(string message, string errorCode, Exception innerException) 
            : base(message, errorCode, innerException) { }
    }
} 