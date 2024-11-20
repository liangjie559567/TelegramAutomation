using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using NLog;
using TelegramAutomation.Models;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace TelegramAutomation.Services
{
    public class ChromeService
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly string[] _searchPaths;
        private const string MINIMUM_CHROME_VERSION = "131.0.6778.86";
        private readonly AppSettings _settings;

        public ChromeService(AppSettings settings)
        {
            _settings = settings;
            _searchPaths = settings.ChromeDriver.SearchPaths;
        }

        public bool ValidateChromeEnvironment()
        {
            try
            {
                var chromePath = DetectChromePath();
                if (string.IsNullOrEmpty(chromePath))
                {
                    throw new ChromeException("未找到 Chrome 浏览器", "CHROME_NOT_FOUND");
                }

                var version = GetChromeVersion(chromePath);
                if (CompareVersions(version, MINIMUM_CHROME_VERSION) < 0)
                {
                    throw new ChromeException($"Chrome 版本过低，需要 {MINIMUM_CHROME_VERSION} 或更高版本", "CHROME_VERSION_MISMATCH");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Chrome 环境验证失败");
                return false;
            }
        }

        private IWebDriver SetUpDriver(string downloadPath)
        {
            var options = new ChromeOptions();
            options.AddUserProfilePreference("download.default_directory", downloadPath);
            foreach (var option in _settings.ChromeDriver.Options)
            {
                options.AddArgument($"--{option.Key}={option.Value}");
            }
            return new ChromeDriver(options);
        }

        public IWebDriver InitializeDriver()
        {
            try
            {
                var options = new ChromeOptions();
                
                // 添加 Chrome 选项
                options.AddArgument("--disable-gpu");
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");
                options.AddArgument("--disable-extensions");
                
                if (_settings.ChromeDriver.Headless)
                {
                    options.AddArgument("--headless");
                }

                // 设置下载首选项
                options.AddUserProfilePreference("download.default_directory", _settings.DefaultSavePath);
                options.AddUserProfilePreference("download.prompt_for_download", false);
                options.AddUserProfilePreference("download.directory_upgrade", true);

                var service = ChromeDriverService.CreateDefaultService();
                service.HideCommandPromptWindow = true;

                _logger.Info("正在初始化 ChromeDriver");
                var driver = new ChromeDriver(service, options);
                _logger.Info("ChromeDriver 初始化成功");

                return driver;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "初始化 ChromeDriver 失败");
                throw new ChromeException(
                    "初始化 ChromeDriver 失败",
                    "CHROMEDRIVER_INIT_FAILED",
                    ex
                );
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

        public async Task<string> GetChromeVersionAsync()
        {
            return await Task.Run(() => GetChromeVersion());
        }

        private string GetChromeVersion()
        {
            // 实现版本获取逻辑
            return "131.0.6778.86";
        }

        public async Task<bool> VerifyVersionAsync()
        {
            var version = await GetChromeVersionAsync();
            return CompareVersions(version, "100.0.0.0") >= 0;
        }

        private async Task<string> GetLatestChromeDriverVersion()
        {
            try
            {
                var config = new ChromeConfig();
                return await config.GetMatchingBrowserVersion();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "获取最新 ChromeDriver 版本失败");
                throw new ChromeException("获取 ChromeDriver 版本失败", "DRIVER_VERSION_ERROR", ex);
            }
        }

        private async Task<bool> ValidateDriverVersion(string driverPath, string expectedVersion)
        {
            try
            {
                var driverService = ChromeDriverService.CreateDefaultService(Path.GetDirectoryName(driverPath));
                driverService.HideCommandPromptWindow = true;

                using var driver = new ChromeDriver(driverService);
                var currentVersion = driver.Capabilities.GetCapability("chrome").ToString();
                
                driver.Quit();
                
                return currentVersion?.StartsWith(expectedVersion) ?? false;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "驱动版本验证失败");
                return false;
            }
        }

        private async Task CleanupOldDrivers(string driverPath)
        {
            try
            {
                var directory = Path.GetDirectoryName(driverPath);
                if (directory == null) return;

                var files = Directory.GetFiles(directory, "chromedriver*.*");
                foreach (var file in files)
                {
                    try
                    {
                        if (file != driverPath)
                        {
                            File.Delete(file);
                            _logger.Info($"清理旧版本驱动: {file}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn(ex, $"清理旧驱动失败: {file}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "清理旧驱动文件失败");
            }
        }

        private string DetectChromePath()
        {
            // 实现 Chrome 路径检测逻辑
            return @"C:\Program Files\Google\Chrome\Application\chrome.exe";
        }

        private int CompareVersions(string v1, string v2)
        {
            // 实现版本比较逻辑
            return Version.Parse(v1).CompareTo(Version.Parse(v2));
        }
    }
} 