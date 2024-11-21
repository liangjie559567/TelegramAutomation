using System;

namespace TelegramAutomation.Exceptions
{
    public class TelegramAutomationException : Exception
    {
        public string ErrorCode { get; }

        public TelegramAutomationException(string message, string errorCode = "", Exception? innerException = null) 
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
} 