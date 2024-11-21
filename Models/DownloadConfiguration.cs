namespace TelegramAutomation.Models
{
    public class DownloadConfiguration
    {
        public int MaxConcurrentDownloads { get; set; } = 3;
        public int LoginWaitTime { get; set; } = 5000;
        public bool SaveMessageText { get; set; } = true;
        public bool SaveLinks { get; set; } = true;
        public string[] SupportedFileExtensions { get; set; } = new[]
        {
            ".zip", ".rar", ".7z", ".tar", ".gz"
        };
    }
} 