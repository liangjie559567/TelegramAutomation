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
        private readonly string[] _searchPaths;
        private const string MINIMUM_CHROME_VERSION = "131.0.6778.86";

        public ChromeService(AppSettings settings)
        {
            _searchPaths = settings.SearchPaths;
        }

        public async Task<bool> ValidateChromeEnvironment()
        {
            try
            {
                // 检查 Chrome 是否安装
                var chromePath = await DetectChromePath();
                if (string.IsNullOrEmpty(chromePath))
                {
                    throw new Exception("CHROME_NOT_FOUND");
                }

                // 检查 Chrome 版本
                var version = GetChromeVersion(chromePath);
                if (CompareVersions(version, MINIMUM_CHROME_VERSION) < 0)
                {
                    throw new Exception($"CHROME_VERSION_MISMATCH: 当前版本 {version}, 需要 {MINIMUM_CHROME_VERSION} 或更高版本");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Chrome 环境验证失败");
                throw;
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

                var allPaths = defaultPaths.Concat(_searchPaths);
                
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