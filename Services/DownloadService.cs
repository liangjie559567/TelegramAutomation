using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using NLog;
using TelegramAutomation.Models;
using System.Linq;
using System.Text;
using System.IO;

namespace TelegramAutomation.Services
{
    public class DownloadService : IDisposable
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly IWebDriver _driver;
        private readonly string _savePath;
        private readonly DownloadConfiguration _config;
        private readonly ScrollHelper _scrollHelper;
        private readonly MessageProcessor _messageProcessor;
        private readonly HttpClient _httpClient;
        private readonly List<MessageContent> _messages;
        private readonly HashSet<string> _processedIds = new();
        private readonly IJavaScriptExecutor _js;
        private readonly FileDownloadService _fileDownloadService;

        public DownloadService(IWebDriver driver, string savePath, DownloadConfiguration config)
        {
            _driver = driver;
            _savePath = savePath;
            _config = config;
            _scrollHelper = new ScrollHelper(driver);
            _messageProcessor = new MessageProcessor(driver);
            _httpClient = new HttpClient();
            _messages = new List<MessageContent>();
            _js = (IJavaScriptExecutor)driver;
            _fileDownloadService = new FileDownloadService(driver, savePath);

            Directory.CreateDirectory(_savePath);
        }

        private async Task AssignTempIdsToMessages()
        {
            try
            {
                _js.ExecuteScript(@"
                    const messages = document.querySelectorAll('.bubbles .message');
                    messages.forEach((msg, index) => {
                        if (!msg.hasAttribute('data-temp-id')) {
                            msg.setAttribute('data-temp-id', 'msg_' + Date.now() + '_' + index);
                        }
                    });
                ");
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "为消息添加临时ID时出错");
            }
        }

        private bool IsMessageProcessed(string tempId)
        {
            return string.IsNullOrEmpty(tempId) || _processedIds.Contains(tempId);
        }

        private void MarkAsProcessed(string tempId)
        {
            if (!string.IsNullOrEmpty(tempId))
            {
                _processedIds.Add(tempId);
            }
        }

        private void ClearProcessedIds()
        {
            _processedIds.Clear();
        }

        private async Task ExpandAllMessages()
        {
            try
            {
                _js.ExecuteScript(@"
                    function expandAllMessages() {
                        const containers = document.querySelectorAll('.bubbles .message.is-collapsed');
                        containers.forEach(container => {
                            container.click();
                        });
                    }
                    expandAllMessages();
                ");
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "展开所有消息时出错");
            }
        }

        private async Task RefreshChatView()
        {
            try
            {
                _js.ExecuteScript(@"
                    const container = document.querySelector('.bubbles');
                    if (container) {
                        container.style.opacity = '0.5';
                        setTimeout(() => {
                            container.style.opacity = '1';
                            container.click();
                        }, 100);
                    }
                ");
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "刷新聊天视图时出错");
            }
        }

        public async Task ProcessChannelMessages(
            string channelName,
            IProgress<string> progress,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.Info($"开始处理频道消息: {channelName}");
                
                Console.WriteLine("请输入要提取的消息数量（从最新消息开始，输入数字）：");
                if (!int.TryParse(Console.ReadLine(), out int messageCount) || messageCount <= 0)
                {
                    Console.WriteLine("输入无效，将默认提取10条消息");
                    messageCount = 10;
                }
                
                var messages = await LoadMessageGroups(progress, cancellationToken, maxMessages: messageCount);
                var processedCount = 0;

                foreach (var message in messages)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        // 创建消息组文件夹
                        var folderName = GetSafeFileName(message.Text?.Split('\n').FirstOrDefault() ?? DateTime.Now.Ticks.ToString());
                        var groupFolder = Path.Combine(_savePath, folderName);
                        Directory.CreateDirectory(groupFolder);

                        // 保存文本内容和URL
                        await SaveTextContent(message, groupFolder);

                        // 等待所有文件下载完成
                        if (message.Files.Any())
                        {
                            _logger.Info($"开始下载消息组 '{folderName}' 的文件");
                            foreach (var file in message.Files)
                            {
                                progress.Report($"正在下载: {file.Name}");
                                // 等待文件下载完成
                                var downloadPath = Path.Combine(groupFolder, file.Name);
                                var timeout = TimeSpan.FromMinutes(5);
                                var startTime = DateTime.Now;

                                while (!File.Exists(downloadPath) && DateTime.Now - startTime < timeout)
                                {
                                    await Task.Delay(1000, cancellationToken);
                                }

                                if (File.Exists(downloadPath))
                                {
                                    _logger.Info($"文件下载完成: {file.Name}");
                                    progress.Report($"下载完成: {file.Name}");
                                }
                                else
                                {
                                    _logger.Error($"文件下载超时: {file.Name}");
                                    progress.Report($"下载失败: {file.Name}");
                                }
                            }
                        }

                        processedCount++;
                        progress.Report($"已处理消息组数量: {processedCount}");
                        _logger.Info($"消息组 '{folderName}' 处理完成");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"处理消息组时出错");
                        progress.Report($"处理消息组时出错: {ex.Message}");
                    }

                    // 添加延迟，避免过快处理
                    await Task.Delay(1000, cancellationToken);
                }

                _logger.Info($"已处理消息组数量: {processedCount}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "处理频道消息时出错");
                throw;
            }
        }

        public async Task<List<MessageContent>> LoadMessageGroups(
            IProgress<string> progress,
            CancellationToken cancellationToken,
            int maxMessages = 10)
        {
            var messages = new List<MessageContent>();
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            var retryCount = 0;
            const int maxRetries = 3;
            const int scrollRetryLimit = 20;
            var scrollRetryCount = 0;
            var lastMessageCount = 0;
            var noNewMessageCount = 0;
            
            try
            {
                // 1. 等待消息容器加载
                wait.Until(d => {
                    try {
                        var container = d.FindElement(By.CssSelector(".bubbles"));
                        return container != null && container.Displayed;
                    }
                    catch {
                        return false;
                    }
                });
                await Task.Delay(2000);

                ClearProcessedIds(); // 清除之前的消息追踪记录

                while (messages.Count < maxMessages && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // 2. 为当前页面所有消息添加临时ID
                        await AssignTempIdsToMessages();

                        // 3. 展开所有折叠的消息
                        await ExpandAllMessages();

                        // 4. 获取当前页面所有消息
                        var visibleElements = wait.Until(d => {
                            try {
                                var elements = d.FindElements(By.CssSelector(".message"));
                                return elements.Count > 0 ? elements : null;
                            }
                            catch {
                                return null;
                            }
                        });

                        if (visibleElements == null)
                        {
                            _logger.Debug("未找到可见消息");
                            if (++retryCount >= maxRetries) break;
                            continue;
                        }

                        var visibleMessages = visibleElements.ToList();
                        _logger.Debug($"找到 {visibleMessages.Count} 条可见消息");
                        var processedAnyMessage = false;

                        // 5. 处理每条消息
                        for (int i = visibleMessages.Count - 1; i >= 0 && messages.Count < maxMessages; i--)
                        {
                            try
                            {
                                var message = visibleMessages[i];
                                
                                // 确保消息元素仍然有效
                                try 
                                {
                                    _ = message.Displayed;
                                }
                                catch (StaleElementReferenceException)
                                {
                                    continue;
                                }

                                var tempId = message.GetAttribute("data-temp-id");

                                // 跳过已处理的消息
                                if (IsMessageProcessed(tempId))
                                {
                                    continue;
                                }

                                // 确保消息在视图中
                                try
                                {
                                    _js.ExecuteScript("arguments[0].scrollIntoView({behavior: 'instant', block: 'center'});", message);
                                    await Task.Delay(500);
                                }
                                catch (StaleElementReferenceException)
                                {
                                    continue;
                                }

                                var hasText = false;
                                try
                                {
                                    var textElements = message.FindElements(By.CssSelector(
                                        "span.translatable-message, div.message-content span.text-content"
                                    ));
                                    hasText = textElements != null && textElements.Any();
                                }
                                catch
                                {
                                    continue;
                                }

                                if (hasText)
                                {
                                    // 收集消息组
                                    var messageGroup = new List<IWebElement> { message };
                                    int nextIndex = i + 1;

                                    // 向后收集附件消息
                                    while (nextIndex < visibleMessages.Count)
                                    {
                                        var nextMessage = visibleMessages[nextIndex];
                                        
                                        try
                                        {
                                            _ = nextMessage.Displayed;
                                        }
                                        catch (StaleElementReferenceException)
                                        {
                                            break;
                                        }

                                        var nextTempId = nextMessage.GetAttribute("data-temp-id");
                                        
                                        // 如果下一条消息已处理，跳出循环
                                        if (IsMessageProcessed(nextTempId))
                                        {
                                            break;
                                        }

                                        // 确保下一个消息在视图中
                                        try
                                        {
                                            _js.ExecuteScript("arguments[0].scrollIntoView({behavior: 'instant', block: 'center'});", nextMessage);
                                            await Task.Delay(200);
                                        }
                                        catch (StaleElementReferenceException)
                                        {
                                            break;
                                        }

                                        var nextHasText = false;
                                        try
                                        {
                                            var nextTextElements = nextMessage.FindElements(By.CssSelector(
                                                "span.translatable-message, div.message-content span.text-content"
                                            ));
                                            nextHasText = nextTextElements != null && nextTextElements.Any();
                                        }
                                        catch
                                        {
                                            break;
                                        }

                                        if (nextHasText) break;

                                        var isFileMessage = false;
                                        try
                                        {
                                            var fileElements = nextMessage.FindElements(By.CssSelector(
                                                ".document-container, .media-container, .media-photo-container"
                                            ));
                                            isFileMessage = fileElements != null && fileElements.Any();
                                        }
                                        catch
                                        {
                                            break;
                                        }

                                        if (isFileMessage)
                                        {
                                            messageGroup.Add(nextMessage);
                                            MarkAsProcessed(nextTempId); // 标记附件消息为已处理
                                        }
                                        nextIndex++;
                                    }

                                    // 处理消息组
                                    var content = await ProcessMessageGroup(
                                        messageGroup,
                                        progress,
                                        cancellationToken
                                    );
                                    if (content != null && !string.IsNullOrWhiteSpace(content.Text))
                                    {
                                        messages.Add(content);
                                        MarkAsProcessed(tempId); // 标记主消息为已处理
                                        progress.Report($"已加载消息组数量: {messages.Count}");
                                        _logger.Info($"已加载消息组数量: {messages.Count}");
                                        processedAnyMessage = true;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(ex, "处理单条消息时出错");
                                if (ex is StaleElementReferenceException)
                                {
                                    // 如果元素过期，继续处理下一条
                                    continue;
                                }
                            }
                        }

                        if (messages.Count >= maxMessages) break;

                        // 如果这一页没有处理到任何新消息，增加重试计数
                        if (!processedAnyMessage)
                        {
                            if (++retryCount >= maxRetries)
                            {
                                _logger.Debug("连续多次未处理到新消息，���能已到达顶部");
                                break;
                            }
                        }
                        else
                        {
                            retryCount = 0; // 重置重试计数
                        }

                        // 6. 向上滚动并刷新页面
                        var currentScrollTop = Convert.ToDouble(_js.ExecuteScript(
                            "return document.querySelector('.bubbles').scrollTop;"
                        ));

                        _js.ExecuteScript($@"
                            const container = document.querySelector('.bubbles');
                            if (container) {{
                                const previousScrollTop = {currentScrollTop};
                                container.scrollTo({{
                                    top: Math.max(0, previousScrollTop - container.clientHeight),
                                    behavior: 'instant'
                                }});
                            }}
                        ");

                        // 等待新内容加载
                        await Task.Delay(2000);

                        // 7. 刷新聊天页面
                        await RefreshChatView();

                        // 验证是否真的滚动了
                        var newScrollTop = Convert.ToDouble(_js.ExecuteScript(
                            "return document.querySelector('.bubbles').scrollTop;"
                        ));

                        // 检查是否有新消息被加载
                        if (messages.Count == lastMessageCount)
                        {
                            noNewMessageCount++;
                            if (noNewMessageCount >= 3) // 如果连续3次没有新消息
                            {
                                if (Math.Abs(newScrollTop - currentScrollTop) < 50) // 降低判定阈值
                                {
                                    _logger.Debug("无法继续向上滚动，可能已到达顶部");
                                    if (++scrollRetryCount >= scrollRetryLimit)
                                    {
                                        _logger.Debug("达到最大滚动重试次数");
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            noNewMessageCount = 0; // 重置无新消息计数
                            lastMessageCount = messages.Count;
                            scrollRetryCount = 0; // 重置滚动重试计数
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "处理消息页面时出错");
                        if (++retryCount >= maxRetries)
                        {
                            _logger.Debug("达到最大重试次数");
                            break;
                        }
                        await Task.Delay(1000);
                    }
                }

                return messages;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载频道消息时出错");
                return messages;
            }
        }

        private async Task<MessageContent?> ProcessMessageGroup(
            List<IWebElement> messageGroup,
            IProgress<string> progress,
            CancellationToken cancellationToken)
        {
            try
            {
                // 处理主消息（文本消息）
                var mainMessage = messageGroup.First();
                var content = _messageProcessor.ProcessMessage(mainMessage);

                // 处理所有附加的文件消息
                foreach (var attachmentMessage in messageGroup.Skip(1))
                {
                    var attachmentContent = _messageProcessor.ProcessMessage(attachmentMessage);
                    if (attachmentContent?.Files != null)
                    {
                        content.Files.AddRange(attachmentContent.Files);
                    }
                }

                // 下载消息组的所有内容
                if (content != null)
                {
                    await _fileDownloadService.ProcessMessageGroupDownload(
                        messageGroup,
                        content,
                        progress,
                        cancellationToken
                    );
                }

                return content;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "处理消息组时出错");
                return null;
            }
        }

        private async Task SaveTextContent(MessageContent content, string folderPath)
        {
            try
            {
                var sb = new StringBuilder();
                
                // 添加文本内容
                if (!string.IsNullOrEmpty(content.Text))
                {
                    sb.AppendLine("=== 文本内容 ===");
                    sb.AppendLine(content.Text);
                    sb.AppendLine();
                }

                // 添加链接
                if (content.Links?.Any() == true)
                {
                    sb.AppendLine("=== 链接 ===");
                    foreach (var link in content.Links)
                    {
                        sb.AppendLine(link);
                    }
                }

                // 保存文本文件
                var textFilePath = Path.Combine(folderPath, "content.txt");
                await File.WriteAllTextAsync(textFilePath, sb.ToString());
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "保存文本内容时出错");
            }
        }

        private string GetSafeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = new string(fileName.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
            return safeName.Length > 100 ? safeName.Substring(0, 100) : safeName;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}