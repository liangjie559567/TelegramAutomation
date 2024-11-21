using System;

namespace TelegramAutomation.Exceptions
{
    public class LoginException : TelegramAutomationException
    {
        public LoginException(string message) : base(message) { }
        public LoginException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
} 