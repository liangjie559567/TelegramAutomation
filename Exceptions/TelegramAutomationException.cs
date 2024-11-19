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

    public class ChromeException : TelegramAutomationException
    {
        public ChromeException(string message, string errorCode = "", Exception? innerException = null) 
            : base(message, errorCode, innerException)
        {
        }
    }

    public class LoginException : TelegramAutomationException
    {
        public LoginException(string message, string errorCode = "", Exception? innerException = null) 
            : base(message, errorCode, innerException)
        {
        }
    }
} 