using NLog;
using TelegramAutomation.Models;
using System.Diagnostics;

namespace TelegramAutomation.Services
{
    public class ChromeService
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly AppSettings _settings;

        public ChromeService(AppSettings settings)
        {
            _settings = settings;
        }

        public bool IsChromeInstalled()
        {
            try
            {
                var chromePath = FindChromePath();
                if (string.IsNullOrEmpty(chromePath))
                {
                    return false;
                }

                var version = GetChromeVersion(chromePath);
                if (string.IsNullOrEmpty(version))
                {
                    return false;
                }

                _logger.Info($"Chrome 版本: {version}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "检查 Chrome 安装失败");
                return false;
            }
        }

        public string? FindChromePath()
        {
            try
            {
                var paths = new[]
                {
                    @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                    @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                        @"Google\Chrome\Application\chrome.exe")
                };

                foreach (var path in paths.Concat(_settings.ChromeDriver.SearchPaths))
                {
                    if (File.Exists(path))
                    {
                        _logger.Info($"找到 Chrome: {path}");
                        return path;
                    }
                }

                _logger.Warn("未找到 Chrome");
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "查找 Chrome 路径失败");
                return null;
            }
        }

        public string? GetChromeVersion(string chromePath)
        {
            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(chromePath);
                var version = versionInfo.FileVersion;
                
                if (string.IsNullOrEmpty(version))
                {
                    _logger.Warn("无法获取 Chrome 版本");
                    return null;
                }

                _logger.Info($"Chrome 版本: {version}");
                return version;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "获取 Chrome 版本失败");
                return null;
            }
        }
    }
} 