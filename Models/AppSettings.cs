using System;
using System.IO;
using System.Text.Json;
using TelegramAutomation.Models;

namespace TelegramAutomation.Models
{
    public class AppSettings
    {
        public DownloadConfiguration DownloadConfig { get; set; } = new();
        public string DefaultSavePath { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "TelegramDownloads"
        );
        
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
} 