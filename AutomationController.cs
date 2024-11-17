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

namespace TelegramAutomation
{
    public class AutomationController : IDisposable
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly DownloadConfiguration _config;
        private IWebDriver? _driver;
        private bool _disposed;
        private readonly SemaphoreSlim _downloadSemaphore;

        public AutomationController(DownloadConfiguration? config = null)
        {
            _config = config ?? new DownloadConfiguration();
            _downloadSemaphore = new SemaphoreSlim(_config.MaxConcurrentDownloads);
        }

        public async Task InitializeBrowser()
        {
            var options = new ChromeOptions();
            options.AddArgument("--start-maximized");
            options.AddArgument("--disable-notifications");
            
            // 设置下载选项
            var downloadPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "TelegramDownloads"
            );
            Directory.CreateDirectory(downloadPath);
            
            options.AddUserProfilePreference("download.default_directory", downloadPath);
            options.AddUserProfilePreference("download.prompt_for_download", false);
            
            _driver = new ChromeDriver(options);
        }

        public async Task NavigateToTelegram()
        {
            if (_driver == null) throw new InvalidOperationException("浏览器未初始化");
            await Task.Run(() => _driver.Navigate().GoToUrl("https://web.telegram.org/"));
        }

        public async Task RequestVerificationCode(string phoneNumber)
        {
            if (_driver == null) throw new InvalidOperationException("浏览器未初始化");

            // 等待手机号输入框出现
            var phoneInput = await WaitForElement(By.CssSelector("input[name='phone']"));
            phoneInput.Clear();
            phoneInput.SendKeys(phoneNumber);

            // 点击下一步按钮
            var nextButton = await WaitForElement(By.CssSelector("button[type='submit']"));
            nextButton.Click();

            // 等待验证码发送
            await Task.Delay(_config.LoginWaitTime);
        }

        public async Task Login(string phoneNumber, string verificationCode)
        {
            if (_driver == null) throw new InvalidOperationException("浏览器未初始化");

            // 等待验证码输入框出现
            var codeInput = await WaitForElement(By.CssSelector("input[name='code']"));
            codeInput.Clear();
            codeInput.SendKeys(verificationCode);

            // 等待登录完成
            await Task.Delay(_config.LoginWaitTime);
            
            // 验证登录状态
            try
            {
                await WaitForElement(By.CssSelector(".chat-list"));
            }
            catch (Exception)
            {
                throw new Exception("登录失败，请检查验证码是否正确");
            }
        }

        private async Task<IWebElement> WaitForElement(By by, int timeoutSeconds = 30)
        {
            if (_driver == null) throw new InvalidOperationException("浏览器未初始化");

            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
            return await Task.Run(() => wait.Until(d => d.FindElement(by)));
        }

        // 其他方法...

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
