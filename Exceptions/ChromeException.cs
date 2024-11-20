namespace TelegramAutomation.Exceptions
{
    public class ChromeException : Exception
    {
        public string ErrorCode { get; }
        public string? AdditionalInfo { get; }

        public ChromeException(string message, string errorCode, string? additionalInfo = null) 
            : base(message)
        {
            ErrorCode = errorCode;
            AdditionalInfo = additionalInfo;
        }
    }
} 