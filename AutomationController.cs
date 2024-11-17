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
using WindowsInput.Core;
using WindowsInput.Native;
using System.Reflection;
using TelegramAutomation.Models;
using TelegramAutomation.Services;

namespace TelegramAutomation
{
    public class AutomationController : IDisposable
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly DownloadConfiguration _config;
        private IWebDriver? _driver;
        private bool _disposed;
        private readonly SemaphoreSlim _downloadSemaphore;
        private readonly IInputSimulator _inputSimulator;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly MessageProcessor _messageProcessor;
        private readonly DownloadManager _downloadManager;

        public AutomationController(DownloadConfiguration? config = null)
        {
            _config = config ?? new DownloadConfiguration();
            _downloadSemaphore = new SemaphoreSlim(_config.MaxConcurrentDownloads);
            _inputSimulator = new InputSimulator();
            _downloadManager = new DownloadManager(_config);
            _messageProcessor = new MessageProcessor(_downloadManager, _config);
        }

        public async Task InitializeBrowser()
        {
            try
            {
                var options = new ChromeOptions();
                options.AddArgument("--start-maximized");
                options.AddArgument("--disable-notifications");
                
                var downloadPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "TelegramDownloads"
                );
                Directory.CreateDirectory(downloadPath);
                
                options.AddUserProfilePreference("download.default_directory", downloadPath);
                options.AddUserProfilePreference("download.prompt_for_download", false);
                
                await Task.Run(() => _driver = new ChromeDriver(options));
                _logger.Info("浏览器初始化成功");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "初始化浏览器失败");
                throw;
            }
        }

        public async Task NavigateToTelegram()
        {
            if (_driver == null) throw new InvalidOperationException("浏览器未初始化");
            await Task.Run(() => _driver.Navigate().GoToUrl("https://web.telegram.org/"));
            _logger.Info("导航到Telegram网页");
        }

        public async Task RequestVerificationCode(string phoneNumber)
        {
            try
            {
                if (_driver == null) throw new InvalidOperationException("浏览器未初始化");

                // 等待手机号码输入框出现
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
                var phoneInput = wait.Until(d => d.FindElement(By.CssSelector("input[type='tel']")));

                // 清除并输入手机号码
                phoneInput.Clear();
                SimulateKeyPress(phoneNumber);
                await Task.Delay(500);

                // 点击下一步按钮
                var nextButton = _driver.FindElement(By.CssSelector("button.btn-primary"));
                nextButton.Click();

                // 等待验证码发送
                await Task.Delay(_config.LoginWaitTime);
                _logger.Info($"已发送验证码到 {phoneNumber}");
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

                // 等待验证码输入框出现
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
                var codeInput = wait.Until(d => d.FindElement(By.CssSelector("input[type='text']")));

                // 输入验证码
                codeInput.Clear();
                SimulateKeyPress(verificationCode);
                await Task.Delay(500);

                // 点击登录按钮
                var loginButton = _driver.FindElement(By.CssSelector("button.btn-primary"));
                loginButton.Click();

                // 等待登录完成
                await Task.Delay(_config.LoginWaitTime);

                // 验证登录状态
                try
                {
                    wait.Until(d => d.FindElement(By.CssSelector(".chat-list")));
                    _logger.Info("登录成功");
                }
                catch (WebDriverTimeoutException)
                {
                    throw new Exception("登录失败，请检查验证码是否正确");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "登录失败");
                throw;
            }
        }

        private void SimulateKeyPress(string text)
        {
            foreach (var c in text)
            {
                _inputSimulator.Keyboard.TextEntry(c.ToString());
                Thread.Sleep(50); // 模拟人工输入的延迟
            }
        }

        public async Task StartAutomation(string channelUrl, string savePath, 
            IProgress<string> progress, CancellationToken cancellationToken)
        {
            try
            {
                if (_driver == null) throw new InvalidOperationException("浏览器未初始化");
                
                await Task.Run(() => _driver.Navigate().GoToUrl(channelUrl));
                progress.Report("已打开频道页面");
                
                // 创建保存目录
                Directory.CreateDirectory(savePath);
                
                // 处理消息
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
                    
                    await _messageProcessor.ProcessMessage(message, messageFolder, progress, cancellationToken);
                }
                
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
    }
}
