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
using System.Collections.Concurrent;
using System.Diagnostics;

namespace TelegramAutomation
{
    public class AutomationController : IDisposable
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly InputSimulator _inputSimulator;
        private IWebDriver? _driver;
        private bool _isRunning;
        private HashSet<string> _processedMessageIds;
        private const int SCROLL_WAIT_TIME = 1000; // 滚动等待时间(毫秒)
        private const int LOGIN_WAIT_TIME = 30000; // 登录等待时间(毫秒)
        private bool _disposed;
        private const string TELEGRAM_WEB_URL = "https://web.telegram.org/";
        private const int VERIFICATION_CODE_TIMEOUT = 60000; // 60秒验证码超时
        private readonly IWebDriverWait _wait;
        private string? _sessionToken;
        private readonly DownloadManager _downloadManager;

        public AutomationController()
        {
            _inputSimulator = new InputSimulator();
            _processedMessageIds = new HashSet<string>();
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));
            _downloadManager = new DownloadManager(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TelegramDownloads"),
                _logger
            );
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

        private IWebDriver InitializeWebDriver()
        {
            try
            {
                var chromeOptions = new ChromeOptions();
                chromeOptions.AddArgument("--disable-gpu");
                chromeOptions.AddArgument("--no-sandbox");
                chromeOptions.AddArgument("--disable-dev-shm-usage");
                
                // 获取当前程序运行目录
                var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var chromeDriverPath = Path.Combine(currentDirectory, "chromedriver.exe");
                
                _logger.Info($"Chrome Driver 路径: {chromeDriverPath}");
                
                if (!File.Exists(chromeDriverPath))
                {
                    throw new Exception($"Chrome Driver 文件不存在: {chromeDriverPath}");
                }
                
                var service = ChromeDriverService.CreateDefaultService(currentDirectory);
                service.HideCommandPromptWindow = true;
                
                _logger.Info("正在初始化 Chrome Driver...");
                return new ChromeDriver(service, chromeOptions);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "初始化 Chrome Driver 失败");
                throw new Exception($"初始化Chrome Driver失败: {ex.Message}", ex);
            }
        }

        private async Task NavigateToChannel(string channelUrl, IProgress<string> progress, 
            CancellationToken cancellationToken)
        {
            if (_driver == null)
            {
                throw new InvalidOperationException("WebDriver 未初始化");
            }

            progress.Report("正在打开Telegram频道...");
            await Task.Run(() => _driver.Navigate().GoToUrl(channelUrl), cancellationToken);
            
            // 等待用户登录
            progress.Report("请在浏览器中手动登录Telegram...");
            var wait = new WebDriverWait(_driver, TimeSpan.FromMilliseconds(LOGIN_WAIT_TIME));
            
            try
            {
                // 等待消息列表出现
                await Task.Run(() => 
                    wait.Until(d => d.FindElements(By.CssSelector(".message")).Count > 0), 
                    cancellationToken);
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
            if (_driver == null)
            {
                throw new InvalidOperationException("WebDriver 未初始化");
            }

            try
            {
                var messageProcessor = new MessageProcessor(_logger);
                var progressManager = new ProgressManager(savePath);
                
                // 加载已处理的消息ID
                _processedMessageIds = await progressManager.LoadProgress();
                
                var lastMessageCount = 0;
                var noNewMessagesCount = 0;
                var retryCount = 0;
                const int MAX_RETRIES = 3;
                
                while (_isRunning && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // 获取所有消息元素
                        var messages = await RetryOperation(() => 
                            Task.Run(() => _driver.FindElements(By.CssSelector(".message")), 
                            cancellationToken));
                        
                        // 检查是否到达底部
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
                            retryCount = 0; // 重置重试计数
                        }
                        
                        lastMessageCount = messages.Count;
                        
                        // 处理每条消息
                        foreach (var message in messages)
                        {
                            if (cancellationToken.IsCancellationRequested) break;
                            
                            try
                            {
                                await ProcessSingleMessage(message, savePath, progress, messageProcessor, cancellationToken);
                                await progressManager.SaveProgress(_processedMessageIds);
                            }
                            catch (StaleElementReferenceException)
                            {
                                _logger.Warn("消息元素已过期，跳过处理");
                                continue;
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(ex, "处理单条消息时出错");
                                progress.Report($"处理消息出错: {ex.Message}");
                                
                                if (++retryCount >= MAX_RETRIES)
                                {
                                    throw new Exception("连续处理消息失败次数过多，任务终止");
                                }
                                
                                await Task.Delay(1000 * retryCount, cancellationToken); // 指数退避
                                continue;
                            }
                        }
                        
                        // 滚动到下一页
                        await ScrollAndWait(progress, cancellationToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.Error(ex, "处理消息时出错");
                        progress.Report($"处理消息出错: {ex.Message}");
                        
                        if (++retryCount >= MAX_RETRIES)
                        {
                            throw new Exception("连续处理失败次数过多，任务终止");
                        }
                        
                        await Task.Delay(1000 * retryCount, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                progress.Report("任务已取消");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "处理消息时出错");
                progress.Report($"处理消息出错: {ex.Message}");
                throw;
            }
        }

        private async Task ProcessSingleMessage(IWebElement message, string savePath, 
            IProgress<string> progress, MessageProcessor messageProcessor, CancellationToken cancellationToken)
        {
            var messageId = await RetryOperation(() => 
                Task.FromResult(message.GetAttribute("id") ?? 
                throw new InvalidOperationException("消息ID不能为空")));
            
            if (_processedMessageIds.Contains(messageId))
            {
                return; // 跳过已处理的消息
            }

            var messageFolder = Path.Combine(savePath, messageId);
            Directory.CreateDirectory(messageFolder);

            try
            {
                // 处理消息文本
                var messageText = await messageProcessor.ExtractMessageText(message);
                if (!string.IsNullOrEmpty(messageText))
                {
                    await File.WriteAllTextAsync(
                        Path.Combine(messageFolder, "message.txt"), 
                        messageText, 
                        cancellationToken
                    );
                }

                // 处理链接
                var links = await messageProcessor.ExtractLinks(message);
                if (links.Any())
                {
                    await File.WriteAllLinesAsync(
                        Path.Combine(messageFolder, "links.txt"),
                        links,
                        cancellationToken
                    );

                    // 处理下载链接
                    foreach (var link in links.Where(l => IsDownloadableFile(l)))
                    {
                        await DownloadFile(link, messageFolder, progress, cancellationToken);
                    }
                }

                _processedMessageIds.Add(messageId);
                progress.Report($"已处理消息: {messageId}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"处理消息 {messageId} 时出错");
                throw;
            }
        }

        private bool IsDownloadableFile(string url)
        {
            var fileExtensions = new[] { ".zip", ".rar", ".7z", ".tar", ".gz" };
            return fileExtensions.Any(ext => url.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        }

        private async Task ScrollAndWait(IProgress<string> progress, CancellationToken cancellationToken)
        {
            if (_driver == null)
            {
                throw new InvalidOperationException("WebDriver 未初始化");
            }

            try
            {
                // 使用类型转换前先检查
                if (_driver is IJavaScriptExecutor jsExecutor)
                {
                    await Task.Run(() => 
                        jsExecutor.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);"),
                        cancellationToken
                    );
                }
                else
                {
                    throw new InvalidOperationException("WebDriver 不支持 JavaScript 执行");
                }
                
                await Task.Delay(SCROLL_WAIT_TIME, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "滚动页面时出错");
                progress.Report($"滚动页面出错: {ex.Message}");
                throw;
            }
        }

        private async Task DownloadFile(string fileUrl, string savePath, IProgress<string> progress, 
            CancellationToken cancellationToken)
        {
            await _downloadManager.DownloadFile(fileUrl, savePath, progress, cancellationToken);
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
            _downloadManager.CancelAllDownloads();
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
                    _downloadManager.CancelAllDownloads();
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

        public async Task RequestVerificationCode(string phoneNumber)
        {
            try
            {
                if (_driver == null)
                {
                    InitializeWebDriver();
                }

                // 导航到 Telegram Web
                await RetryOperation(async () =>
                {
                    _driver!.Navigate().GoToUrl(TELEGRAM_WEB_URL);
                    return Task.CompletedTask;
                });

                // 等待手机号输入框
                var phoneInput = _wait.Until(d => d.FindElement(By.CssSelector("input[type='tel']")));
                
                // 清除并输入手机号
                phoneInput.Clear();
                phoneInput.SendKeys(phoneNumber);

                // 点击下一步按钮
                var nextButton = _wait.Until(d => d.FindElement(By.CssSelector("button[type='submit']")));
                nextButton.Click();

                // 等待验证码输入框出现，确认验证码已发送
                _wait.Until(d => d.FindElement(By.CssSelector("input[type='text']")));
                
                _logger.Info($"已向 {phoneNumber} 发送验证码");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "请求验证码失败");
                throw new Exception("请求验证码失败，请稍后重试", ex);
            }
        }

        public async Task Login(string phoneNumber, string verificationCode)
        {
            try
            {
                if (_driver == null)
                {
                    throw new InvalidOperationException("浏览器未初始化");
                }

                // 等待验证码输入框
                var codeInput = _wait.Until(d => d.FindElement(By.CssSelector("input[type='text']")));
                
                // 输入验证码
                codeInput.Clear();
                codeInput.SendKeys(verificationCode);

                // 等待登录完成
                await WaitForLoginComplete();

                // 保存会信息
                SaveSession();

                _logger.Info("登录成功");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "登录失败");
                throw new Exception("登录失败，请检查验证码是否正确", ex);
            }
        }

        private async Task WaitForLoginComplete()
        {
            try
            {
                // 等待主界面加载完成
                await Task.WhenAny(
                    Task.Run(() => _wait.Until(d => d.FindElement(By.CssSelector(".messages-container")))),
                    Task.Delay(LOGIN_WAIT_TIME)
                );

                if (!IsLoggedIn())
                {
                    throw new Exception("登录超时");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "等待登录完成失败");
                throw;
            }
        }

        private bool IsLoggedIn()
        {
            try
            {
                // 检查是否存在消息容器元素
                return _driver?.FindElements(By.CssSelector(".messages-container")).Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private void SaveSession()
        {
            try
            {
                // 保存登录会话信息
                _sessionToken = GetSessionToken();
                var sessionFile = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "TelegramAutomation",
                    "session.json"
                );

                Directory.CreateDirectory(Path.GetDirectoryName(sessionFile)!);
                File.WriteAllText(sessionFile, _sessionToken);
                
                _logger.Info("会话信息已保存");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "保存会话信息失败");
            }
        }

        private string? GetSessionToken()
        {
            try
            {
                // 获取 localStorage 中的会话信息
                var token = ((IJavaScriptExecutor)_driver!)
                    .ExecuteScript("return window.localStorage.getItem('telegram-auth-token');") as string;
                return token;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "获取会话令牌失败");
                return null;
            }
        }
    }

    // 添加消息处理器类
    private class MessageProcessor
    {
        private readonly ILogger _logger;
        
        public MessageProcessor(ILogger logger)
        {
            _logger = logger;
        }
        
        public async Task<string> ExtractMessageText(IWebElement message)
        {
            try
            {
                var textElement = message.FindElement(By.CssSelector(".text-content"));
                return await Task.FromResult(textElement?.Text ?? string.Empty);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "提取消息文本失败");
                return string.Empty;
            }
        }
        
        public async Task<IEnumerable<string>> ExtractLinks(IWebElement message)
        {
            try
            {
                var links = message.FindElements(By.TagName("a"))
                    .Select(a => a.GetAttribute("href"))
                    .Where(href => !string.IsNullOrEmpty(href))
                    .ToList();
                
                return await Task.FromResult(links);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "提取链接失败");
                return Enumerable.Empty<string>();
            }
        }
    }

    // 添加进度管理器类
    private class ProgressManager
    {
        private readonly string _progressFile;
        
        public ProgressManager(string savePath)
        {
            _progressFile = Path.Combine(savePath, "progress.json");
        }
        
        public async Task<HashSet<string>> LoadProgress()
        {
            try
            {
                if (File.Exists(_progressFile))
                {
                    var json = await File.ReadAllTextAsync(_progressFile);
                    return System.Text.Json.JsonSerializer.Deserialize<HashSet<string>>(json) 
                        ?? new HashSet<string>();
                }
            }
            catch { }
            
            return new HashSet<string>();
        }
        
        public async Task SaveProgress(HashSet<string> processedIds)
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(processedIds);
                await File.WriteAllTextAsync(_progressFile, json);
            }
            catch { }
        }
    }

    // 添加下载管理器类
    private class DownloadManager
    {
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _downloadThrottle;
        private readonly ConcurrentDictionary<string, DownloadStatus> _downloads;
        private readonly string _downloadPath;
        private readonly int _maxConcurrentDownloads;
        private readonly int _maxRetries;
        
        public DownloadManager(string downloadPath, ILogger logger, int maxConcurrentDownloads = 3, int maxRetries = 3)
        {
            _downloadPath = downloadPath;
            _logger = logger;
            _maxConcurrentDownloads = maxConcurrentDownloads;
            _maxRetries = maxRetries;
            _downloadThrottle = new SemaphoreSlim(maxConcurrentDownloads);
            _downloads = new ConcurrentDictionary<string, DownloadStatus>();
        }
        
        public async Task DownloadFile(string url, string savePath, IProgress<string> progress, CancellationToken cancellationToken)
        {
            await _downloadThrottle.WaitAsync(cancellationToken);
            
            try
            {
                var fileName = Path.GetFileName(url);
                var filePath = Path.Combine(savePath, fileName);
                var tempPath = filePath + ".tmp";
                
                // 检查文件是否已存在
                if (File.Exists(filePath))
                {
                    _logger.Info($"文件已存在，跳过下载: {fileName}");
                    progress.Report($"文件已存在: {fileName}");
                    return;
                }
                
                // 检查磁盘空间
                if (!CheckDiskSpace(savePath))
                {
                    throw new Exception("磁盘空间不足");
                }
                
                var downloadStatus = new DownloadStatus { FilePath = filePath, TempPath = tempPath };
                _downloads[url] = downloadStatus;
                
                for (int retry = 0; retry <= _maxRetries; retry++)
                {
                    try
                    {
                        using var client = new HttpClient();
                        client.Timeout = TimeSpan.FromMinutes(30);
                        
                        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                        response.EnsureSuccessStatusCode();
                        
                        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                        var totalMB = totalBytes / (1024.0 * 1024.0);
                        
                        _logger.Info($"开始下载: {fileName} ({totalMB:F2} MB)");
                        progress.Report($"开始下载: {fileName} ({totalMB:F2} MB)");
                        
                        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                        using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                        
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
                                var downloadedMB = totalBytesRead / (1024.0 * 1024.0);
                                progress.Report($"下载进度 {fileName}: {percentage}% ({downloadedMB:F2}/{totalMB:F2} MB)");
                            }
                        }
                        
                        // 下载完成，重命名临时文件
                        fileStream.Close();
                        File.Move(tempPath, filePath);
                        
                        _logger.Info($"下载完成: {fileName}");
                        progress.Report($"下载完成: {fileName}");
                        _downloads.TryRemove(url, out _);
                        return;
                    }
                    catch (Exception ex) when (retry < _maxRetries)
                    {
                        _logger.Warn($"下载失败，准备重试 ({retry + 1}/{_maxRetries}): {ex.Message}");
                        progress.Report($"下载失败，准备重试: {fileName}");
                        await Task.Delay(1000 * (retry + 1), cancellationToken);
                    }
                }
                
                throw new Exception($"下载失败，已达到最大重试次数: {fileName}");
            }
            finally
            {
                _downloadThrottle.Release();
            }
        }
        
        private bool CheckDiskSpace(string path)
        {
            try
            {
                var drive = new DriveInfo(Path.GetPathRoot(path));
                var freeSpace = drive.AvailableFreeSpace;
                var minRequiredSpace = 1024L * 1024L * 1024L; // 1GB
                
                return freeSpace > minRequiredSpace;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "检查磁盘空间失败");
                return true; // 如果无法检查，默认允许下载
            }
        }
        
        public void CancelAllDownloads()
        {
            foreach (var download in _downloads)
            {
                try
                {
                    if (File.Exists(download.Value.TempPath))
                    {
                        File.Delete(download.Value.TempPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"清理临时文件失败: {download.Value.TempPath}");
                }
            }
            
            _downloads.Clear();
        }
        
        private class DownloadStatus
        {
            public string FilePath { get; set; }
            public string TempPath { get; set; }
            public long TotalBytes { get; set; }
            public long DownloadedBytes { get; set; }
        }
    }
}
