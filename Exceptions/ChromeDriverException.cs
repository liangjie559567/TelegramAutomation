namespace TelegramAutomation.Exceptions
{
    public class ChromeDriverException : ChromeException
    {
        public ChromeDriverException(string message, Exception ex) 
            : base(message, "CHROMEDRIVER_ERROR", ex.ToString())
        {
        }

        public ChromeDriverException(string message, string errorCode, string? additionalInfo = null)
            : base(message, errorCode, additionalInfo)
        {
        }
    }
} 