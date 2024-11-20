using System;

namespace TelegramAutomation.Exceptions
{
    public class LoginException : Exception
    {
        public string ErrorCode { get; }
        public string AdditionalInfo { get; }

        public LoginException() : base() { }
        
        public LoginException(string message) : base(message) { }
        
        public LoginException(string message, Exception innerException) 
            : base(message, innerException) { }

        public LoginException(string message, string errorCode, string additionalInfo = null) 
            : base(message)
        {
            ErrorCode = errorCode;
            AdditionalInfo = additionalInfo;
        }
    }
} 