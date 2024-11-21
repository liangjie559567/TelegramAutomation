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
using Microsoft.Win32;

namespace TelegramAutomation.Services
{
    public class ChromeService : IDisposable
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private IWebDriver? _driver;
        private bool _isInitialized;
        private readonly AppSettings _settings;
        private readonly string[] _searchPaths;
        private const string MINIMUM_CHROME_VERSION = "131.0.6778.86";

        public bool IsInitialized => _isInitialized;

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
                    throw new ChromeException(
                        "未找到 Chrome 浏览器",
                        ErrorCodes.CHROME_NOT_FOUND
                    );
                }

                var version = GetChromeVersion();
                if (CompareVersions(version, MINIMUM_CHROME_VERSION) < 0)
                {
                    throw new ChromeException(
                        $"Chrome 版本过低，需要 {MINIMUM_CHROME_VERSION} 或更高版本",
                        ErrorCodes.CHROME_VERSION_MISMATCH
                    );
                }

                return true;
            }
            catch (Exception ex) when (ex is not ChromeException)
            {
                _logger.Error(ex, "Chrome 环境验证失败");
                throw new ChromeException(
                    "Chrome 环境验证失败",
                    ErrorCodes.CHROME_DRIVER_ERROR,
                    ex
                );
            }
        }

        private async Task InitializeDriverAsync()
        {
            try
            {
                await Task.Run(() => {
                    var options = new ChromeOptions();
                    
                    foreach (var option in _settings.ChromeDriver.Options)
                    {
                        options.AddArgument(option.Value);
                    }
                    
                    if (_settings.ChromeDriver.Headless)
                    {
                        options.AddArgument("--headless");
                    }
                    
                    _driver = new ChromeDriver(options);
                });
                
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "初始化 ChromeDriver 失败");
                throw new ChromeDriverException(
                    "初始化 ChromeDriver 失败",
                    ErrorCodes.INITIALIZATION_ERROR,
                    ex
                );
            }
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
                throw new LoginException(
                    "检查登录状态失败",
                    ErrorCodes.LOGIN_FAILED,
                    ex
                );
            }
        }

        private string DetectChromePath()
        {
            foreach (var path in _searchPaths)
            {
                if (File.Exists(path))
                    return path;
            }

            // 从注册表查找
            var registryPath = GetChromePathFromRegistry();
            if (!string.IsNullOrEmpty(registryPath))
                return registryPath;

            return string.Empty;
        }

        private string GetChromePathFromRegistry()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe");
                if (key != null)
                {
                    var path = key.GetValue(null) as string;
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                        return path;
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "从注册表获取Chrome路径失败");
            }
            return string.Empty;
        }

        private int CompareVersions(string version1, string version2)
        {
            var v1Parts = version1.Split('.').Select(int.Parse).ToArray();
            var v2Parts = version2.Split('.').Select(int.Parse).ToArray();
            
            for (int i = 0; i < Math.Min(v1Parts.Length, v2Parts.Length); i++)
            {
                if (v1Parts[i] != v2Parts[i])
                    return v1Parts[i].CompareTo(v2Parts[i]);
            }
            
            return v1Parts.Length.CompareTo(v2Parts.Length);
        }

        private string GetChromeVersion()
        {
            try
            {
                var chromePath = DetectChromePath();
                if (string.IsNullOrEmpty(chromePath))
                    throw new ChromeException("未找到Chrome浏览器", ErrorCodes.CHROME_NOT_FOUND);

                var versionInfo = FileVersionInfo.GetVersionInfo(chromePath);
                return versionInfo.FileVersion ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "获取Chrome版本失败");
                throw new ChromeException("无法获取Chrome版本", ErrorCodes.CHROME_VERSION_ERROR, ex);
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

        public async Task InitializeAsync()
        {
            try
            {
                // 添加重试机制
                int maxRetries = 3;
                for (int i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        var chromeVersion = await GetChromeVersionAsync();
                        var driverVersion = await GetDriverVersionAsync(chromeVersion);
                        await SetupChromeDriverAsync(driverVersion);
                        break;
                    }
                    catch (Exception) when (i < maxRetries - 1)
                    {
                        await Task.Delay(1000); // 重试延迟
                        continue;
                    }
                }
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
                var majorVersion = string.Join(".", chromeVersion.Split('.').Take(3));
                var response = await client.GetStringAsync(
                    $"https://chromedriver.storage.googleapis.com/LATEST_RELEASE_{majorVersion}");
                return await Task.FromResult(response.Trim());
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "获取ChromeDriver版本失败");
                return await GetLocalChromeDriverVersionAsync();
            }
        }

        private async Task<string> GetLocalChromeDriverVersionAsync()
        {
            var defaultVersion = "131.0.6778.86";
            return await Task.FromResult(defaultVersion);
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
                throw new ChromeDriverException(
                    "设置ChromeDriver失败",
                    ErrorCodes.CHROME_DRIVER_ERROR,
                    ex
                );
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
                throw new ChromeException(
                    "无法获取Chrome版本",
                    ErrorCodes.CHROME_VERSION_MISMATCH,
                    ex
                );
            }
        }

        public void Dispose()
        {
            _driver?.Quit();
            _driver = null;
            _isInitialized = false;
        }

        public async Task RequestVerificationCode(string phoneNumber)
        {
            try
            {
                if (_driver == null)
                {
                    await InitializeDriverAsync();
                }

                if (_driver == null) throw new ChromeException("浏览器初始化失败", ErrorCodes.CHROME_LAUNCH_ERROR);

                // 导航到 Telegram Web
                await Task.Run(() => _driver.Navigate().GoToUrl("https://web.telegram.org/k/"));
                
                // 等待加载完成
                await WaitForElementAsync(By.CssSelector(".auth-form"), TimeSpan.FromSeconds(20));
                
                // 等待电话号码输入框出现
                var phoneInput = await WaitForElementAsync(By.CssSelector("input[type='tel']"), TimeSpan.FromSeconds(10));
                
                // 清除并输入电话号码
                await Task.Run(() => {
                    phoneInput.Clear();
                    // 使用 JavaScript 设置值，避免输入问题
                    ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].value = arguments[1]", phoneInput, phoneNumber);
                    // 触发 input 事件
                    ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].dispatchEvent(new Event('input'))", phoneInput);
                });

                await Task.Delay(500); // 短暂延迟

                // 查找并点击"下一步"或"发送验证码"按钮
                var nextButton = await WaitForElementAsync(By.CssSelector("button[type='submit']"), TimeSpan.FromSeconds(5));
                await Task.Run(() => {
                    // 使用 JavaScript 点击，避免可能的覆盖问题
                    ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click()", nextButton);
                });

                // 等待验证码输入框出现，确认验证码已发送
                await WaitForElementAsync(By.CssSelector("input[inputmode='numeric']"), TimeSpan.FromSeconds(10));
            }
            catch (WebDriverTimeoutException ex)
            {
                _logger.Error(ex, "等待元素超时");
                throw new LoginException("页面加载超时，请检查网络连接", ErrorCodes.LOGIN_TIMEOUT, ex);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "请求验证码失败");
                throw new LoginException("请求验证码失败", ErrorCodes.LOGIN_FAILED, ex);
            }
        }

        private async Task<IWebElement> WaitForElementAsync(By by, TimeSpan timeout)
        {
            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(_driver, timeout);
            return await Task.Run(() => wait.Until(d => d.FindElement(by)));
        }

        private async Task SimulateTyping(IWebElement element, string text)
        {
            foreach (char c in text)
            {
                element.SendKeys(c.ToString());
                await Task.Delay(Random.Shared.Next(50, 150)); // 随机延迟，模拟人工输入
            }
        }
    }
} 
} 