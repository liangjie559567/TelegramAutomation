using System;

namespace TelegramAutomation.Exceptions
{
    public class LoginException : TelegramAutomationException
    {
        public LoginException(string message, string errorCode) 
            : base(message, errorCode) { }

        public LoginException(string message, string errorCode, Exception innerException) 
            : base(message, errorCode, innerException) { }
    }
} 