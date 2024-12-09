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
using System.Threading.Tasks;
using TelegramAutomation.Exceptions;
using System.Threading;
using OpenQA.Selenium.Support.UI;
using TelegramAutomation.ViewModels;
using TelegramAutomation.Services;
using System.Collections.Generic;

namespace TelegramAutomation.Services
{
    public class ChromeService : IDisposable, IAsyncDisposable
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly AppSettings _settings;
        private IWebDriver? _driver;
        private bool _isInitialized;
        private const string MINIMUM_CHROME_VERSION = "90.0.0.0";

        public ChromeService(AppSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task InitializeAsync()
        {
            try
            {
                if (_isInitialized)
                {
                    _logger.Info("Chrome 服务已初始化");
                    return;
                }

                _logger.Info("正在初始化 Chrome 服务...");
                
                var options = CreateChromeOptions();
                
                _driver = new ChromeDriver(options);
                _driver.Manage().Window.Maximize();
                _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
                _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(0);
                
                _logger.Info("正在访问 Telegram Web...");
                _driver.Navigate().GoToUrl("https://web.telegram.org/k/");
                await Task.Delay(8000);

                // 检查是否需要登录
                while (NeedsLogin())
                {
                    _logger.Info("需要登录，开始登录流程...");
                    var loginViewModel = new LoginViewModel(_driver);
                    
                    try
                    {
                        // 1. 点击登录按钮
                        await loginViewModel.ClickLoginButton();
                        await Task.Delay(2000);

                        // 2. 输入手机号
                        await loginViewModel.EnterPhoneNumber("+8619122903869");
                        await Task.Delay(2000);

                        bool loginSuccess = false;
                        while (!loginSuccess)
                        {
                            try
                            {
                                // 3. 等待用户输入验证码
                                var verificationCode = await loginViewModel.WaitForVerificationCode();
                                
                                // 4. 输入验证码
                                await loginViewModel.EnterVerificationCode(verificationCode);
                                await Task.Delay(5000); // 等待登录完成
                                
                                loginSuccess = true;
                            }
                            catch (ChromeException ex) when (ex.ErrorCode == ErrorCodes.LOGIN_FAILED)
                            {
                                _logger.Error($"验证码错误或登录失败: {ex.Message}");
                                Console.WriteLine("验证码错误或登录失败，请重试");
                                // 继续循环，重新输入验证码
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "登录过程出错");
                        throw;
                    }
                }

                _logger.Info("已经登录，无需重新登录");
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "初始化 Chrome 服务时出错");
                throw;
            }
        }

        private ChromeOptions CreateChromeOptions()
        {
            var options = new ChromeOptions();
            options.AddArguments(
                "--disable-gpu",
                "--disable-software-rasterizer",
                "--log-level=3",  // 只显示致命错误
                "--silent",
                "--disable-logging",
                "--disable-dev-shm-usage"
            );

            // 设置用户数据目录，保存登录状态
            var userDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TelegramAutomation",
                "ChromeProfile"
            );
            Directory.CreateDirectory(userDataDir);
            options.AddArgument($"--user-data-dir={userDataDir}");

            // 设置下载路径为指定目录
            var downloadPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "TelegramDownloads"
            );
            Directory.CreateDirectory(downloadPath);

            var prefs = new Dictionary<string, object>
            {
                { "download.default_directory", downloadPath },
                { "download.prompt_for_download", false },
                { "download.directory_upgrade", true },
                { "safebrowsing.enabled", true },
                { "profile.default_content_settings.popups", 0 }
            };
            options.AddUserProfilePreference("download", prefs);

            return options;
        }

        private bool NeedsLogin()
        {
            try
            {
                if (_driver == null)
                {
                    _logger.Error("Chrome driver 未初始化");
                    return true;
                }

                // 等待页面加载
                Thread.Sleep(3000);

                // 检查是否已经登录（在聊天界面）
                var chatElements = _driver.FindElements(By.CssSelector(
                    ".chat-list, .dialogs-container, .messages-container, " +
                    ".sidebar-header, .chat-background, .new-message-button"
                ));

                if (chatElements.Any(e => e.Displayed))
                {
                    _logger.Info("检测到聊天界面，已经登录");
                    return false;
                }

                // 如果没有检测到聊天界面，再检查登录相关元素
                var loginElements = _driver.FindElements(By.XPath(
                    "//*[contains(text(), 'Log in to Telegram by QR Code')] | " +
                    "//*[contains(text(), 'Sign in to Telegram')] | " +
                    "//*[contains(text(), 'LOG IN BY PHONE NUMBER')] | " +
                    "//input[@type='tel']"
                ));

                var needsLogin = loginElements.Any(e => e.Displayed);
                _logger.Info(needsLogin ? "需要登录" : "无需登录");
                return needsLogin;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "检查登录状态时出错");
                return true;
            }
        }

        public bool IsInitialized => _isInitialized;

        private string GetChromePath()
        {
            _logger.Info("正在查找 Chrome 浏览器...");
            
            if (_settings.ChromeDriver?.SearchPaths != null)
            {
                foreach (var basePath in _settings.ChromeDriver.SearchPaths)
                {
                    _logger.Debug($"搜索路径: {basePath}");
                    if (Directory.Exists(basePath))
                    {
                        var chromePath = Path.Combine(basePath, "chrome.exe");
                        if (File.Exists(chromePath))
                        {
                            _logger.Info($"找到 Chrome 浏览器: {chromePath}");
                            return chromePath;
                        }
                    }
                }
            }

            var defaultPaths = new[]
            {
                @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                            @"Google\Chrome\Application\chrome.exe")
            };

            foreach (var path in defaultPaths)
            {
                _logger.Debug($"检查默认路径: {path}");
                if (File.Exists(path))
                {
                    _logger.Info($"找到 Chrome 浏览器: {path}");
                    return path;
                }
            }

            _logger.Error("未找到 Chrome 浏览器");
            throw new ChromeException(
                "未找到 Chrome 浏览器，请确保已安装 Google Chrome",
                ErrorCodes.CHROME_NOT_FOUND
            );
        }

        private string GetChromeVersion()
        {
            try
            {
                var chromePath = GetChromePath();
                var versionInfo = FileVersionInfo.GetVersionInfo(chromePath);
                var version = versionInfo.FileVersion;
                
                if (string.IsNullOrEmpty(version))
                {
                    throw new ChromeException(
                        "无法获取 Chrome 版本",
                        ErrorCodes.CHROME_VERSION_MISMATCH
                    );
                }

                return version;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "获取 Chrome 版本失败");
                throw new ChromeException(
                    "无法获取 Chrome 版",
                    ErrorCodes.CHROME_VERSION_MISMATCH,
                    ex
                );
            }
        }

        public void Dispose()
        {
            try
            {
                if (_driver != null)
                {
                    _driver.Quit();
                    _driver.Dispose();
                    _driver = null;
                }
                _isInitialized = false;
                _logger.Info("Chrome 服务已释放");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "释放 Chrome 服务时发生错误");
            }
        }

        public async Task StartDownloadingAsync(string channelName)
        {
            try
            {
                if (_driver == null)
                {
                    throw new ChromeException("Chrome driver 未初始化", ErrorCodes.INITIALIZATION_ERROR);
                }

                var savePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "TelegramDownloads",
                    channelName.Replace("@", "").Replace("/", "_")
                );

                // 创建下载配置
                var downloadConfig = new DownloadConfiguration
                {
                    MaxConcurrentDownloads = 3,
                    SaveMessageText = true,
                    SaveLinks = true,
                    SupportedFileExtensions = new[]
                    {
                        ".zip", ".rar", ".7z", ".tar", ".gz",
                        ".mp4", ".avi", ".mkv", ".mov",
                        ".jpg", ".jpeg", ".png", ".gif",
                        ".pdf", ".doc", ".docx", ".xls", ".xlsx"
                    }
                };

                var progress = new Progress<string>(message => _logger.Info(message));
                var downloadService = new DownloadService(_driver, savePath, downloadConfig);
                _logger.Info($"开始下载频道内容到: {savePath}");
                
                await downloadService.ProcessChannelMessages(
                    channelName,
                    progress,
                    CancellationToken.None
                );
                
                _logger.Info("频道内容下载完成");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "下载频道内容时出错");
                throw;
            }
        }

        public async Task NavigateToChannel(string channelName)
        {
            if (_driver == null)
            {
                throw new ChromeException("Chrome driver 未初始化", ErrorCodes.INITIALIZATION_ERROR);
            }

            var loginViewModel = new LoginViewModel(_driver);
            await loginViewModel.NavigateToChannel(channelName);
        }

        public async ValueTask DisposeAsync()
        {
            if(_driver != null)
            {
                await Task.Run(() => _driver.Quit());
                _driver.Dispose();
            }
        }
    }
} 