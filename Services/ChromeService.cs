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

namespace TelegramAutomation.Services
{
    public class ChromeService : IDisposable
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
                
                var options = new ChromeOptions();
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");
                options.AddArgument("--disable-gpu");
                options.AddArgument("--disable-extensions");
                options.AddArgument("--start-maximized");
                options.AddArgument("--disable-blink-features=AutomationControlled");
                options.AddArgument("--disable-web-security");
                options.AddArgument("--disable-features=IsolateOrigins,site-per-process");
                
                // 设置 user-agent
                options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");

                options.AddArgument("--disable-notifications");
                options.AddArgument("--disable-popup-blocking");
                options.AddArgument("--disable-infobars");
                options.AddArgument("--ignore-certificate-errors");

                _driver = new ChromeDriver(options);
                _driver.Manage().Window.Maximize();
                _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
                _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(0);
                
                // 使用 /k/ 版本的 Telegram Web
                _logger.Info("正在访问 Telegram Web...");
                _driver.Navigate().GoToUrl("https://web.telegram.org/k/");
                await Task.Delay(8000);

                try
                {
                    var loginViewModel = new LoginViewModel(_driver);
                    
                    // 1. 点击登录按钮
                    await loginViewModel.ClickLoginButton();
                    await Task.Delay(2000);

                    // 2. 输入手机号
                    await loginViewModel.EnterPhoneNumber("+18479005288");
                    await Task.Delay(2000);

                    // 3. 等待用户输入验证码
                    var verificationCode = await loginViewModel.WaitForVerificationCode();
                    
                    // 4. 输入验证码
                    await loginViewModel.EnterVerificationCode(verificationCode);
                    await Task.Delay(5000); // 等待登录完成

                    // 5. 切换到指定频道
                    Console.WriteLine("\n请输入要访问的频道名称: ");
                    var channelName = Console.ReadLine()?.Trim();
                    if (!string.IsNullOrEmpty(channelName))
                    {
                        await loginViewModel.NavigateToChannel(channelName);
                    }
                    
                    _isInitialized = true;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "登录操作失败");
                    throw new ChromeException(
                        "登录操作失败: " + ex.Message,
                        ErrorCodes.LOGIN_FAILED,
                        ex
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Chrome 初始化失败");
                throw new ChromeException(
                    $"Chrome 初始化失败: {ex.Message}",
                    ErrorCodes.INITIALIZATION_ERROR,
                    ex
                );
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
    }
} 