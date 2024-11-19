using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using NLog;
using TelegramAutomation.Models;

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
                return !string.IsNullOrEmpty(chromePath);
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
                var defaultPaths = new[]
                {
                    @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                    @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                        @"Google\Chrome\Application\chrome.exe")
                };

                var allPaths = defaultPaths.Concat(_settings.ChromeDriver.SearchPaths);
                
                foreach (var path in allPaths)
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
                if (File.Exists(chromePath))
                {
                    var versionInfo = FileVersionInfo.GetVersionInfo(chromePath);
                    var version = versionInfo.FileVersion;
                    
                    if (!string.IsNullOrEmpty(version))
                    {
                        _logger.Info($"Chrome 版本: {version}");
                        return version;
                    }
                }
                
                _logger.Warn("无法获取 Chrome 版本");
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "获取 Chrome 版本失败");
                return null;
            }
        }
    }
} 