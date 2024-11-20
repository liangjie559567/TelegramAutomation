using System;
using System.IO;
using System.Text.Json;
using TelegramAutomation.Models;

namespace TelegramAutomation.Models
{
    public class AppSettings
    {
        public DownloadConfiguration DownloadConfig { get; set; } = new();
        public ChromeDriverConfig ChromeDriver { get; set; } = new();
        public string DefaultSavePath { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "TelegramDownloads"
        );
        public int WaitTimeout { get; set; }
        public int LoginWaitTime { get; set; }
        
        public static AppSettings Load()
        {
            var configPath = "appsettings.json";
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            return new AppSettings();
        }

        public void Save()
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            File.WriteAllText("appsettings.json", json);
        }
    }

    public class ChromeDriverConfig
    {
        public bool Headless { get; set; } = false;
        public string[] SearchPaths { get; set; } = Array.Empty<string>();
        public Dictionary<string, string> Options { get; set; } = new()
        {
            { "disable-gpu", "" },
            { "no-sandbox", "" },
            { "disable-dev-shm-usage", "" },
            { "disable-extensions", "" }
        };
    }
} 