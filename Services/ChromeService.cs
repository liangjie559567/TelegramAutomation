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
using System.Net.Http;

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

                var version = GetChromeVersion();
                if (CompareVersions(version, MINIMUM_CHROME_VERSION) < 0)
                {
                    throw new ChromeException(
                        $"Chrome 版本过低，需要 {MINIMUM_CHROME_VERSION} 或更高版本",
                        "CHROME_VERSION_LOW"
                    );
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Chrome 环境验证失败");
                throw;
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
            try
            {
                return await Task.Run(() => {
                    var chromePath = GetChromePath();
                    var versionInfo = FileVersionInfo.GetVersionInfo(chromePath);
                    return versionInfo.FileVersion ?? string.Empty;
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "获取Chrome版本失败");
                throw new ChromeException("无法获取Chrome版本", ex);
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                var chromeVersion = await GetChromeVersionAsync();
                var driverVersion = await GetDriverVersionAsync(chromeVersion);
                await SetupChromeDriverAsync(driverVersion);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "ChromeService初始化失败");
                throw;
            }
        }

        private async Task<string> GetDriverVersionAsync(string chromeVersion)
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetStringAsync(
                    $"https://chromedriver.storage.googleapis.com/LATEST_RELEASE_{chromeVersion.Split('.')[0]}");
                return await Task.FromResult(response.Trim());
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "获取ChromeDriver版本失败");
                throw new ChromeDriverException("无法获取ChromeDriver版本", ex);
            }
        }

        private async Task SetupChromeDriverAsync(string version)
        {
            try
            {
                await Task.Run(() => {
                    var manager = new DriverManager();
                    manager.SetUpDriver(
                        new ChromeConfig(),
                        version,
                        Architecture.X64
                    );
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "设置ChromeDriver失败");
                throw;
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

        private string GetChromeVersion()
        {
            try
            {
                var chromePath = DetectChromePath();
                var versionInfo = FileVersionInfo.GetVersionInfo(chromePath);
                return versionInfo.FileVersion ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "获取Chrome版本失败");
                throw new ChromeException("无法获取Chrome版本", "VERSION_ERROR", ex.Message);
            }
        }

        private string GetChromePath()
        {
            var possiblePaths = new[]
            {
                @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"
            }.Concat(_searchPaths);

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }
            throw new ChromeException("未找到Chrome浏览器", "CHROME_NOT_FOUND");
        }

        public async Task<bool> CheckLoginStatusAsync()
        {
            try
            {
                // 实现登录状态检查逻辑
                await Task.Delay(100); // 模拟异步操作
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "检查登录状态失败");
                throw new LoginException("检查登录状态失败", ex.Message);
            }
        }
    }
} 