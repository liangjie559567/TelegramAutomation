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
        private CancellationTokenSource? _cancellationTokenSource;

        public AutomationController(DownloadConfiguration? config = null)
        {
            _config = config ?? new DownloadConfiguration();
            _downloadSemaphore = new SemaphoreSlim(_config.MaxConcurrentDownloads);
            _inputSimulator = new InputSimulator();
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
        }

        public async Task StartAutomation(string channelUrl, string savePath, 
            IProgress<string> progress, CancellationToken cancellationToken)
        {
            try
            {
                if (_driver == null) throw new InvalidOperationException("浏览器未初始化");
                
                await Task.Run(() => _driver.Navigate().GoToUrl(channelUrl));
                progress.Report("已打开频道页面");
                
                // 处理消息下载
                var messageProcessor = new MessageProcessor(
                    new DownloadManager(_config), 
                    _config
                );

                // 实现消息处理逻辑
                var messages = await Task.Run(() => 
                    _driver.FindElements(By.CssSelector(".message")).ToList(),
                    cancellationToken
                );

                foreach (var message in messages)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    var messageId = message.GetAttribute("data-message-id");
                    var messageFolder = Path.Combine(savePath, messageId);

                    await messageProcessor.ProcessMessage(
                        message,
                        messageFolder,
                        progress,
                        cancellationToken
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "自动化处理失败");
                throw;
            }
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            CleanupWebDriver();
        }

        private void CleanupWebDriver()
        {
            try
            {
                _driver?.Quit();
                _driver?.Dispose();
                _driver = null;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "清理 WebDriver 时出错");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    CleanupWebDriver();
                }
                _disposed = true;
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
                    if (i == maxRetries - 1) throw;
                    _logger.Warn($"操作失败，准备重试 ({i + 1}/{maxRetries}): {ex.Message}");
                    await Task.Delay(1000 * (i + 1)); // 指数退避
                }
            }
            throw new Exception("重试次数超过最大限制");
        }

        private void SimulateKeyPress(string text)
        {
            foreach (char c in text)
            {
                _inputSimulator.Keyboard.TextEntry(c);
            }
        }

        private void SimulateEnterKey()
        {
            _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
        }

        private void SimulateControlC()
        {
            _inputSimulator.Keyboard.ModifiedKeyStroke(
                VirtualKeyCode.CONTROL, 
                VirtualKeyCode.VK_C);
        }

        private void SimulateControlV()
        {
            _inputSimulator.Keyboard.ModifiedKeyStroke(
                VirtualKeyCode.CONTROL, 
                VirtualKeyCode.VK_V);
        }
    }
}
