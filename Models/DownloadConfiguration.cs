public class DownloadConfiguration
{
    public int MaxRetries { get; set; } = 3;
    public int ScrollWaitTime { get; set; } = 1000;
    public int LoginWaitTime { get; set; } = 30000;
    public string[] SupportedFileExtensions { get; set; } = 
        new[] { ".zip", ".rar", ".7z", ".tar", ".gz" };
    public bool SaveMessageText { get; set; } = true;
    public bool SaveLinks { get; set; } = true;
    public int MaxConcurrentDownloads { get; set; } = 3;
} 