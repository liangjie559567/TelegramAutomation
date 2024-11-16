using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using WindowsInput;
using NLog;
using System.Text.RegularExpressions;

namespace TelegramAutomation
{
    public class AutomationController : IDisposable
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly InputSimulator _inputSimulator;
        private IWebDriver _driver;
        private bool _isRunning;
        private HashSet<string> _processedMessageIds;
        private const int SCROLL_WAIT_TIME = 1000; // 滚动等待时间(毫秒)
        private const int LOGIN_WAIT_TIME = 30000; // 登录等待时间(毫秒)
        private bool _disposed;

        public AutomationController()
        {
            _inputSimulator = new InputSimulator();
            _processedMessageIds = new HashSet<string>();
        }

        public async Task StartAutomation(string channelUrl, string savePath, 
            IProgress<string> progress, CancellationToken cancellationToken)
        {
            try
            {
                _isRunning = true;
                _logger.Info("开始自动化任务");
                InitializeWebDriver();
                
                await NavigateToChannel(channelUrl, progress, cancellationToken);
                await ProcessMessages(savePath, progress, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "自动化任务出错");
                progress.Report($"错误: {ex.Message}");
            }
            finally
            {
                CleanupWebDriver();
            }
        }

        private void InitializeWebDriver()
        {
            var options = new ChromeOptions();
            options.AddArgument("--start-maximized");
            options.AddArgument("--disable-notifications");
            
            // 添加下载设置
            var downloadPath = Path.Combine(Path.GetTempPath(), "TelegramDownloads");
            Directory.CreateDirectory(downloadPath);
            
            options.AddUserProfilePreference("download.default_directory", downloadPath);
            options.AddUserProfilePreference("download.prompt_for_download", false);
            
            _driver = new ChromeDriver(options);
        }

        private async Task NavigateToChannel(string channelUrl, IProgress<string> progress, 
            CancellationToken cancellationToken)
        {
            progress.Report("正在打开Telegram频道...");
            _driver.Navigate().GoToUrl(channelUrl);
            
            // 等待用户登录
            progress.Report("请在浏览器中手动登录Telegram...");
            var wait = new WebDriverWait(_driver, TimeSpan.FromMilliseconds(LOGIN_WAIT_TIME));
            
            try
            {
                // 等待消息列表出现
                wait.Until(d => d.FindElements(By.CssSelector(".message")).Count > 0);
                progress.Report("登录成功，开始处理消息...");
            }
            catch (WebDriverTimeoutException)
            {
                throw new Exception("登录超时，请确保已正确登录Telegram");
            }
        }

        private async Task ProcessMessages(string savePath, IProgress<string> progress, 
            CancellationToken cancellationToken)
        {
            var lastMessageCount = 0;
            var noNewMessagesCount = 0;
            
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                var messages = _driver.FindElements(By.CssSelector(".message"));
                
                if (messages.Count == lastMessageCount)
                {
                    noNewMessagesCount++;
                    if (noNewMessagesCount >= 5) // 连续5次没有新消息，认为已到达底部
                    {
                        progress.Report("已到达频道底部，任务完成");
                        break;
                    }
                }
                else
                {
                    noNewMessagesCount = 0;
                }
                
                lastMessageCount = messages.Count;
                
                foreach (var message in messages)
                {
                    await ProcessSingleMessage(message, savePath, progress, cancellationToken);
                }
                
                await ScrollAndWait(progress, cancellationToken);
            }
        }

        private async Task ProcessSingleMessage(IWebElement message, string savePath, 
            IProgress<string> progress, CancellationToken cancellationToken)
        {
            try
            {
                var messageId = message.GetAttribute("data-message-id");
                if (string.IsNullOrEmpty(messageId) || _processedMessageIds.Contains(messageId))
                {
                    return;
                }

                _processedMessageIds.Add(messageId);
                var messageFolder = Path.Combine(savePath, messageId);
                Directory.CreateDirectory(messageFolder);

                // 保存消息文本
                var messageText = message.FindElement(By.CssSelector(".text-content"))?.Text ?? "";
                if (!string.IsNullOrEmpty(messageText))
                {
                    await File.WriteAllTextAsync(
                        Path.Combine(messageFolder, "message.txt"), 
                        messageText, 
                        cancellationToken
                    );
                }

                // 处理链接
                await ProcessMessageLinks(message, messageFolder, progress, cancellationToken);
                
                progress.Report($"已处理消息: {messageId}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "处理单条消息时出错");
                progress.Report($"处理消息出错: {ex.Message}");
            }
        }

        private async Task ProcessMessageLinks(IWebElement message, string messageFolder, 
            IProgress<string> progress, CancellationToken cancellationToken)
        {
            var links = message.FindElements(By.TagName("a"));
            foreach (var link in links)
            {
                try
                {
                    var href = link.GetAttribute("href");
                    if (string.IsNullOrEmpty(href)) continue;

                    // 检查是否是压缩包链接
                    if (IsDownloadableFile(href))
                    {
                        await DownloadFile(href, messageFolder, progress, cancellationToken);
                    }
                    else
                    {
                        // 保存其他链接到文本文件
                        await File.AppendAllTextAsync(
                            Path.Combine(messageFolder, "links.txt"),
                            $"{href}\n",
                            cancellationToken
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"处理链接时出错: {ex.Message}");
                }
            }
        }

        private bool IsDownloadableFile(string url)
        {
            var fileExtensions = new[] { ".zip", ".rar", ".7z", ".tar", ".gz" };
            return fileExtensions.Any(ext => url.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        }

        private async Task ScrollAndWait(IProgress<string> progress, CancellationToken cancellationToken)
        {
            try
            {
                // 使用JavaScript滚动
                ((IJavaScriptExecutor)_driver).ExecuteScript(
                    "window.scrollTo(0, document.body.scrollHeight);"
                );
                
                await Task.Delay(SCROLL_WAIT_TIME, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "滚动页面时出错");
                progress.Report($"滚动页面出错: {ex.Message}");
            }
        }

        private async Task DownloadFile(string fileUrl, string savePath, IProgress<string> progress,
            CancellationToken cancellationToken)
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromMinutes(30);
                
                var fileName = Path.GetFileName(fileUrl);
                var filePath = Path.Combine(savePath, fileName);
                
                progress.Report($"开始下载: {fileName}");
                
                using var response = await client.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                
                var buffer = new byte[8192];
                var totalBytesRead = 0L;
                int bytesRead;
                
                while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    totalBytesRead += bytesRead;
                    
                    if (totalBytes > 0)
                    {
                        var percentage = (int)((totalBytesRead * 100) / totalBytes);
                        progress.Report($"下载进度 {fileName}: {percentage}%");
                    }
                }
                
                progress.Report($"下载完成: {fileName}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "下载文件时出错");
                progress.Report($"下载文件出错: {ex.Message}");
                throw; // 重新抛出异常以便上层处理
            }
        }

        private void CleanupWebDriver()
        {
            try
            {
                _driver?.Quit();
                _driver?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "清理WebDriver时出错");
            }
            finally
            {
                _driver = null;
                _isRunning = false;
            }
        }

        public void Stop()
        {
            _isRunning = false;
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
    }
} 