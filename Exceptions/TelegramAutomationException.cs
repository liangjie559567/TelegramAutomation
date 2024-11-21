using System;

namespace TelegramAutomation.Exceptions
{
    public class TelegramAutomationException : Exception
    {
        public TelegramAutomationException(string message) : base(message) { }
        public TelegramAutomationException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
} 