using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using NLog;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WindowsInput;
using WindowsInput.Native;
using System.Reflection;
using TelegramAutomation.Models;
using TelegramAutomation.Services;
using System.Diagnostics;

namespace TelegramAutomation
{
    public class AutomationController : IDisposable
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly DownloadConfiguration _config;
        private IWebDriver? _driver;
        private bool _disposed;
        private readonly SemaphoreSlim _downloadSemaphore;
        private readonly InputSimulator _inputSimulator;
        private readonly IKeyboardSimulator _keyboard;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly MessageProcessor _messageProcessor;
        private readonly DownloadManager _downloadManager;

        public AutomationController(DownloadConfiguration? config = null)
        {
            _config = config ?? new DownloadConfiguration();
            _downloadSemaphore = new SemaphoreSlim(_config.MaxConcurrentDownloads);
            _inputSimulator = new InputSimulator();
            _keyboard = _inputSimulator.Keyboard;
            _downloadManager = new DownloadManager(_config);
            _messageProcessor = new MessageProcessor(_downloadManager, _config);
            _cancellationTokenSource = new CancellationTokenSource();
            
            // 记录环境信息
            LogEnvironmentInfo();
        }

        public async Task InitializeBrowser()
        {
            try
            {
                var chromeOptions = new ChromeOptions();
                chromeOptions.AddArguments("--start-maximized");
                
                // 获取 Chrome 版本
                var chromeVersion = GetChromeVersion();
                _logger.Info($"检测到 Chrome 版本: {chromeVersion}");
                
                // 检查 ChromeDriver 版本是否匹配
                var driverPath = await EnsureCorrectChromeDriver(chromeVersion);
                
                var service = ChromeDriverService.CreateDefaultService(
                    Path.GetDirectoryName(driverPath),
                    Path.GetFileName(driverPath)
                );
                
                _driver = new ChromeDriver(service, chromeOptions);
                _logger.Info("浏览器初始化成功");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "初始化浏览器失败");
                throw;
            }
        }

        private string GetChromeVersion()
        {
            try
            {
                // 检查所有可能的 Chrome 安装路径
                var possiblePaths = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Google\Chrome\Application\chrome.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Google\Chrome\Application\chrome.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\Application\chrome.exe"),
                    @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                    @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"
                };

                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        _logger.Info($"找到 Chrome 浏览器: {path}");
                        var versionInfo = FileVersionInfo.GetVersionInfo(path);
                        var version = versionInfo.FileVersion;
                        if (!string.IsNullOrEmpty(version))
                        {
                            _logger.Info($"Chrome 版本: {version}");
                            return version;
                        }
                    }
                }

                // 如果上面的路径都没找到，尝试通过注册表查找
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe"))
                {
                    if (key != null)
                    {
                        var chromePath = key.GetValue(null) as string;
                        if (!string.IsNullOrEmpty(chromePath) && File.Exists(chromePath))
                        {
                            _logger.Info($"通过注册表找到 Chrome: {chromePath}");
                            var versionInfo = FileVersionInfo.GetVersionInfo(chromePath);
                            var version = versionInfo.FileVersion;
                            if (!string.IsNullOrEmpty(version))
                            {
                                _logger.Info($"Chrome 版本: {version}");
                                return version;
                            }
                        }
                    }
                }

                _logger.Error("未找到 Chrome 浏览器，已检查以下路径:");
                foreach (var path in possiblePaths)
                {
                    _logger.Error($"- {path}");
                }
                throw new FileNotFoundException("未找到 Chrome 浏览器，请确保已正确安装 Chrome");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "获取 Chrome 版本失败");
                throw new Exception("获取 Chrome 版本失败，请确保已正确安装 Chrome", ex);
            }
        }

        public async Task NavigateToTelegram()
        {
            if (_driver == null) throw new InvalidOperationException("浏览器未初始化");
            
            try
            {
                await Task.Run(() => 
                {
                    _driver.Navigate().GoToUrl("https://web.telegram.org/");
                    var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));
                    wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
                });
                _logger.Info("导航到Telegram网页成功");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "导航到Telegram网页失败");
                throw;
            }
        }

        public async Task RequestVerificationCode(string phoneNumber)
        {
            try
            {
                if (_driver == null) throw new InvalidOperationException("浏览器未初始化");

                // 等待登录页面加载完成
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));
                wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));

                // 等待手机号输入框出现
                var phoneInput = await RetryOperation(async () =>
                {
                    var input = wait.Until(d => d.FindElement(By.CssSelector("input[type='tel']")));
                    if (!input.Displayed || !input.Enabled)
                        throw new ElementNotInteractableException("手机号输入框不可交互");
                    return input;
                }, maxRetries: 3);

                // 清除并输入手机号码
                phoneInput.Clear();
                await Task.Delay(500); // 等待清除完
                
                // 模拟人工输
                foreach (var c in phoneNumber)
                {
                    SimulateKeyPress(c.ToString());
                    await Task.Delay(Random.Shared.Next(50, 150)); // 随机延迟
                }

                // 等待并点击下一步按钮
                var nextButton = await RetryOperation(async () =>
                {
                    var button = wait.Until(d => d.FindElement(By.CssSelector("button[type='submit']")));
                    if (!button.Displayed || !button.Enabled)
                        throw new ElementNotInteractableException("下一步按钮不可点击");
                    return button;
                }, maxRetries: 3);

                nextButton.Click();
                
                // 验证是否成功发送验证码
                try
                {
                    wait.Until(d => d.FindElement(By.CssSelector("input[type='text']")));
                    _logger.Info($"已成功发送验证码到 {phoneNumber}");
                }
                catch (WebDriverTimeoutException)
                {
                    throw new Exception("验证码发送失，请检查手机号码是否正确");
                }
            }
            catch (WebDriverException ex)
            {
                _logger.Error(ex, "浏览器操作失败");
                throw new Exception("浏览器操作失败，请检查网络连接", ex);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "请求验证码失败");
                throw;
            }
        }

        public async Task Login(string phoneNumber, string verificationCode)
        {
            try
            {
                if (_driver == null) throw new InvalidOperationException("浏览器未初始化");

                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));

                // 等待验证码输入框出现
                var codeInput = await RetryOperation(async () =>
                {
                    var input = wait.Until(d => d.FindElement(By.CssSelector("input[type='text']")));
                    if (!input.Displayed || !input.Enabled)
                        throw new ElementNotInteractableException("验证码输入框不可交互");
                    return input;
                }, maxRetries: 3);

                // 清除并输入验证码
                codeInput.Clear();
                await Task.Delay(500);

                // 模拟人工输入验证码
                foreach (var c in verificationCode)
                {
                    SimulateKeyPress(c.ToString());
                    await Task.Delay(Random.Shared.Next(50, 150));
                }

                // 等待登录按钮可点击
                var loginButton = await RetryOperation(async () =>
                {
                    var button = wait.Until(d => d.FindElement(By.CssSelector("button[type='submit']")));
                    if (!button.Displayed || !button.Enabled)
                        throw new ElementNotInteractableException("登录按钮不可点击");
                    return button;
                }, maxRetries: 3);

                loginButton.Click();

                // 等待登录完成
                await Task.Delay(_config.LoginWaitTime);

                // 验证登录状态
                try
                {
                    // 检查多个可能的登录成功标志
                    var isLoggedIn = await RetryOperation(async () =>
                    {
                        try
                        {
                            // 尝试查找聊天列表
                            wait.Until(d => d.FindElement(By.CssSelector(".chat-list")));
                            return true;
                        }
                        catch
                        {
                            try
                            {
                                // 尝试查找其他登录成功标志
                                wait.Until(d => d.FindElement(By.CssSelector(".messages-container")));
                                return true;
                            }
                            catch
                            {
                                return false;
                            }
                        }
                    }, maxRetries: 3);

                    if (!isLoggedIn)
                    {
                        throw new Exception("登录失败，请检查验证码是否正确");
                    }

                    _logger.Info("登录成功");
                }
                catch (WebDriverTimeoutException)
                {
                    throw new Exception("登录超时，请重试");
                }
            }
            catch (WebDriverException ex)
            {
                _logger.Error(ex, "浏览器操作失败");
                throw new Exception("浏览器操作失败，请检查网络连接", ex);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "登录失败");
                throw;
            }
        }

        private async Task<T> RetryOperation<T>(Func<Task<T>> operation, int maxRetries = 3)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    _logger.Warn($"操作失败，重试 {i + 1}/{maxRetries}: {ex.Message}");
                    if (i == maxRetries - 1) throw;
                    await Task.Delay(1000 * (i + 1));
                }
            }
            throw new Exception($"操作失败，已重试 {maxRetries} 次");
        }

        private async Task<IWebElement> WaitForElement(By by, int timeoutSeconds = 30)
        {
            if (_driver == null) throw new InvalidOperationException("浏览器未初始化");

            return await Task.Run(() =>
            {
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
                return wait.Until(d => d.FindElement(by));
            });
        }

        private async Task<bool> WaitForElementVisible(By by, int timeoutSeconds = 30)
        {
            try
            {
                await Task.Yield();
                var element = await WaitForElement(by, timeoutSeconds);
                return element.Displayed && element.Enabled;
            }
            catch
            {
                return false;
            }
        }

        private async Task SimulateKeyPress(string text)
        {
            await Task.Yield();
            foreach (var c in text)
            {
                if (char.IsDigit(c))
                {
                    _keyboard.TextEntry(c);
                }
                else
                {
                    _keyboard.TextEntry(c.ToString());
                }
                await Task.Delay(Random.Shared.Next(50, 150));
            }
        }

        private async Task<bool> CheckLoginStatus()
        {
            try
            {
                var chatList = await WaitForElementVisible(By.CssSelector(".chat-list"), 5);
                if (chatList) return true;

                var messagesContainer = await WaitForElementVisible(By.CssSelector(".messages-container"), 5);
                return messagesContainer;
            }
            catch
            {
                return false;
            }
        }

        public async Task StartAutomation(string channelUrl, string savePath, 
            IProgress<string> progress, CancellationToken cancellationToken)
        {
            try
            {
                if (_driver == null) throw new InvalidOperationException("浏览器未初始化");
                
                await Task.Run(() => 
                {
                    _driver.Navigate().GoToUrl(channelUrl);
                    
                    var messages = _driver.FindElements(By.CssSelector(".message"));
                    foreach (var message in messages)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            progress.Report("操作已取消");
                            return;
                        }

                        var messageId = message.GetAttribute("data-message-id");
                        var messageFolder = Path.Combine(savePath, messageId);
                        
                        _messageProcessor.ProcessMessage(message, messageFolder, progress, cancellationToken)
                            .GetAwaiter().GetResult();
                    }
                }, cancellationToken);
                
                progress.Report("自动化任务完成");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "自动化任务失败");
                throw;
            }
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            _logger.Info("已停止自动化任务");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _driver?.Quit();
                _driver?.Dispose();
                _downloadSemaphore.Dispose();
                _cancellationTokenSource?.Dispose();
                _disposed = true;
            }
        }

        private void LogEnvironmentInfo()
        {
            try
            {
                _logger.Info("=== 环境信息 ===");
                _logger.Info($"操作系统: {Environment.OSVersion}");
                _logger.Info($"64位系统: {Environment.Is64BitOperatingSystem}");
                _logger.Info($"64位进程: {Environment.Is64BitProcess}");
                _logger.Info($"当前目录: {Environment.CurrentDirectory}");
                _logger.Info($"程序目录: {AppDomain.CurrentDomain.BaseDirectory}");
                _logger.Info($"系统目录: {Environment.SystemDirectory}");
                _logger.Info($"临时目录: {Path.GetTempPath()}");
                _logger.Info($"用户配置文件: {Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}");
                _logger.Info($"程序文件目录: {Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}");
                _logger.Info($"程序文件(x86)目录: {Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}");
                _logger.Info($"本地应用数据: {Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}");
                _logger.Info("=== 环境变量 ===");
                foreach (var env in Environment.GetEnvironmentVariables().Keys)
                {
                    _logger.Info($"{env}: {Environment.GetEnvironmentVariable(env?.ToString() ?? "")}");
                }
                _logger.Info("===============");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "获取环境信息失败");
            }
        }

        private async Task<string> EnsureCorrectChromeDriver(string chromeVersion)
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                _logger.Info($"当前应用程序目录: {baseDir}");
                
                var driverPath = Path.Combine(baseDir, "chromedriver.exe");
                _logger.Info($"目标 ChromeDriver 路径: {driverPath}");

                // 获取 NuGet 包根目录
                var nugetRoot = Environment.GetEnvironmentVariable("NUGET_PACKAGES") 
                    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
                _logger.Info($"NuGet 包根目录: {nugetRoot}");

                // 检查所有可能的路径
                var possiblePaths = new[]
                {
                    // 当前目录
                    driverPath,
                    
                    // NuGet 包路径
                    Path.Combine(
                        nugetRoot,
                        "selenium.webdriver.chromedriver",
                        "132.0.6834.600-beta",
                        "driver",
                        "win32",
                        "chromedriver.exe"
                    ),
                    
                    // 发布目录
                    Path.Combine(baseDir, "publish", "chromedriver.exe"),
                    
                    // 构建输出目录
                    Path.Combine(baseDir, "bin", "Release", "net6.0-windows", "chromedriver.exe"),
                    Path.Combine(baseDir, "bin", "Debug", "net6.0-windows", "chromedriver.exe"),
                    
                    // 相对路径
                    "chromedriver.exe",
                    @".\chromedriver.exe",
                    @"..\chromedriver.exe"
                };

                if (!File.Exists(driverPath))
                {
                    _logger.Warn("ChromeDriver 不存在，尝试从可能的位置查找");
                    
                    foreach (var path in possiblePaths)
                    {
                        _logger.Info($"检查路径: {path}");
                        try
                        {
                            if (File.Exists(path))
                            {
                                _logger.Info($"找到 ChromeDriver: {path}");
                                var fileInfo = new FileInfo(path);
                                _logger.Info($"文件大小: {fileInfo.Length:N0} 字节");
                                _logger.Info($"创建时间: {fileInfo.CreationTime}");
                                _logger.Info($"修改时间: {fileInfo.LastWriteTime}");
                                
                                try
                                {
                                    await Task.Run(() => File.Copy(path, driverPath, true));
                                    _logger.Info($"已复制 ChromeDriver 到: {driverPath}");
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    _logger.Error(ex, $"复制 ChromeDriver 失败: {path} -> {driverPath}");
                                    _logger.Error($"错误详情: {ex.Message}");
                                    if (ex.InnerException != null)
                                    {
                                        _logger.Error($"内部错误: {ex.InnerException.Message}");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, $"检查路径失败: {path}");
                        }
                    }
                }

                if (!File.Exists(driverPath))
                {
                    var error = "未找到 ChromeDriver，已检查以下路径:";
                    _logger.Error(error);
                    foreach (var path in possiblePaths)
                    {
                        _logger.Error($"- {path}");
                    }
                    throw new FileNotFoundException("未找到 ChromeDriver，请确保程序完整性或重新安装程序");
                }

                // 验证 ChromeDriver 是否可用
                try
                {
                    var driverInfo = await Task.Run(() => FileVersionInfo.GetVersionInfo(driverPath));
                    _logger.Info($"ChromeDriver 版本: {driverInfo.FileVersion}");
                    
                    // 检查文件权限
                    var fileInfo = new FileInfo(driverPath);
                    _logger.Info($"文件属性: {fileInfo.Attributes}");
                    
                    if ((fileInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        fileInfo.Attributes &= ~FileAttributes.ReadOnly;
                        _logger.Info("已移除只读属性");
                    }

                    // 验证文件完整性
                    await using (var fs = new FileStream(driverPath, FileMode.Open, FileAccess.Read))
                    {
                        _logger.Info($"文件可以正常打开，大小: {fs.Length:N0} 字节");
                    }
                    
                    return driverPath;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "验证 ChromeDriver 失败");
                    _logger.Error($"错误详情: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        _logger.Error($"内部错误: {ex.InnerException.Message}");
                    }
                    throw new Exception("ChromeDriver 验证失败，请确保程序完整性", ex);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "确保 ChromeDriver 版本匹配失败");
                _logger.Error($"错误详情: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"内部错误: {ex.InnerException.Message}");
                }
                throw;
            }
        }
    }
}
