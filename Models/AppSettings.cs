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
            try
            {
                var configPath = "appsettings.json";
                if (!File.Exists(configPath))
                {
                    throw new FileNotFoundException("配置文件不存在", configPath);
                }

                var jsonString = File.ReadAllText(configPath);
                var settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(jsonString);
                
                if (settings == null)
                {
                    throw new InvalidOperationException("配置文件格式错误");
                }

                return settings;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("加载配置文件失败", ex);
            }
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