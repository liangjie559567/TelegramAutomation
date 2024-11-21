using System;

namespace TelegramAutomation.Exceptions
{
    public class TelegramAutomationException : Exception
    {
        public string ErrorCode { get; }

        public TelegramAutomationException(string message, string errorCode) 
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public TelegramAutomationException(string message, string errorCode, Exception innerException) 
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
} 