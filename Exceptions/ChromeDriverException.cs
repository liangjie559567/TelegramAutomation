namespace TelegramAutomation.Exceptions
{
    public class ChromeDriverException : ChromeException
    {
        public ChromeDriverException(string message, string errorCode = "", Exception? innerException = null) 
            : base(message, errorCode, innerException)
        {
        }
    }
} 