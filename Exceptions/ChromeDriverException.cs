using System;

namespace TelegramAutomation.Exceptions
{
    public class ChromeDriverException : ChromeException
    {
        public ChromeDriverException(string message, string errorCode) 
            : base(message, errorCode) { }

        public ChromeDriverException(string message, string errorCode, Exception innerException) 
            : base(message, errorCode, innerException) { }
    }
} 