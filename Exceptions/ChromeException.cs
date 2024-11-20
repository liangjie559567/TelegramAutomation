namespace TelegramAutomation.Exceptions
{
    public class ChromeException : TelegramAutomationException
    {
        public ChromeException(string message, string errorCode = "", Exception? innerException = null) 
            : base(message, errorCode, innerException)
        {
        }
    }
} 