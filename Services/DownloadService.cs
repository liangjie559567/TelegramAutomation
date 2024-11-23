using System;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Interactions;
using SeleniumExtras.WaitHelpers;
using NLog;
using TelegramAutomation.Models;
using System.Text.Json;
using TelegramAutomation.Services;

namespace TelegramAutomation.Services
{
    public class DownloadService
    {
        private readonly IWebDriver _driver;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly ScrollHelper _scrollHelper;
        private readonly MessageProcessor _messageProcessor;
        private readonly string _savePath;
        private readonly DownloadConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly List<MessageContent> _messages;

        public DownloadService(IWebDriver driver, string savePath, DownloadConfiguration config)
        {
            _driver = driver;
            _savePath = savePath;
            _config = config;
            _scrollHelper = new ScrollHelper(driver);
            _messageProcessor = new MessageProcessor(driver);
            _httpClient = new HttpClient();
            _messages = new List<MessageContent>();

            Directory.CreateDirectory(_savePath);
        }

        public async Task<List<MessageContent>> ProcessChannelMessages(
            string channelName,
            IProgress<string> progress,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.Info($"开始处理频道消息: {channelName}");
                
                Console.WriteLine("\n请输入要提取的消息数量（从最新消息开始，输入数字）：");
                if (!int.TryParse(Console.ReadLine(), out int messageCount) || messageCount <= 0)
                {
                    Console.WriteLine("输入无效，将默认提取10条消息");
                    messageCount = 10;
                }
                
                var messages = await LoadChannelMessagesAsync(progress, cancellationToken, maxMessages: messageCount);
                
                Console.WriteLine("\n找到以下消息：");
                Console.WriteLine("----------------------------------------");
                for (int i = 0; i < messages.Count; i++)
                {
                    var msg = messages[i];
                    Console.WriteLine($"[{i + 1}] 标题: {msg.Text.Split('\n')[0]}");
                    Console.WriteLine("内容：");
                    
                    var contentLines = msg.Text.Split('\n').Skip(1)
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();
                    if (contentLines.Any())
                    {
                        Console.WriteLine(string.Join("\n", contentLines));
                    }

                    if (msg.Files.Any())
                    {
                        Console.WriteLine("文件:");
                        foreach (var file in msg.Files)
                        {
                            Console.WriteLine($"{file.Name} ({file.Size})");
                        }
                    }

                    if (msg.Links.Any())
                    {
                        Console.WriteLine("链接:");
                        foreach (var link in msg.Links)
                        {
                            Console.WriteLine(link);
                        }
                    }

                    Console.WriteLine("----------------------------------------");
                }

                Console.WriteLine($"\n共找到 {messages.Count} 条消息");
                return messages;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "处理频道消息失败");
                throw;
            }
        }

        private string CreateMessageFolder(MessageContent message, int index)
        {
            // 使用消息标题创建文件夹名
            var title = message.Text.Split('\n')[0].Trim();
            var safeFolderName = GetSafeFileName($"{index:D2}_{title}");
            var folderPath = Path.Combine(_savePath, safeFolderName);
            
            Directory.CreateDirectory(folderPath);
            return folderPath;
        }

        private async Task SaveMessageContent(MessageContent message, string folderPath, CancellationToken cancellationToken)
        {
            // 保存消息文本
            await File.WriteAllTextAsync(
                Path.Combine(folderPath, "description.txt"),
                message.Text,
                cancellationToken
            );

            // 保存链接
            if (message.Links.Any())
            {
                var validLinks = message.Links
                    .Where(l => !l.Contains("hashtag"))
                    .ToList();
                    
                if (validLinks.Any())
                {
                    await File.WriteAllLinesAsync(
                        Path.Combine(folderPath, "links.txt"),
                        validLinks,
                        cancellationToken
                    );
                }
            }
        }

        private async Task DownloadMessageFiles(
            MessageContent message, 
            string folderPath,
            IProgress<string> progress, 
            CancellationToken cancellationToken)
        {
            var semaphore = new SemaphoreSlim(_config.MaxConcurrentDownloads);
            var downloadTasks = new List<Task>();

            foreach (var file in message.Files)
            {
                downloadTasks.Add(DownloadFileAsync(
                    file,
                    folderPath,
                    semaphore,
                    progress,
                    cancellationToken
                ));
            }

            await Task.WhenAll(downloadTasks);
        }

        private string GetSafeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }

        private async Task DownloadFileAsync(
            MessageContent.FileInfo file,
            string prefix,
            SemaphoreSlim semaphore,
            IProgress<string> progress,
            CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var extension = Path.GetExtension(file.Name);
                var fileName = $"{prefix}_{Path.GetFileNameWithoutExtension(file.Name)}{extension}";
                var filePath = Path.Combine(_savePath, fileName);

                // 如果文件已存在，添加数字后缀
                int counter = 1;
                while (File.Exists(filePath))
                {
                    fileName = $"{prefix}_{Path.GetFileNameWithoutExtension(file.Name)}_{counter}{extension}";
                    filePath = Path.Combine(_savePath, fileName);
                    counter++;
                }

                progress.Report($"开始下载: {fileName}");

                using var response = await _httpClient.GetAsync(file.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var fileStream = File.Create(filePath);
                await stream.CopyToAsync(fileStream, cancellationToken);

                progress.Report($"下载完成: {fileName}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"下载文件失败: {file.Name}");
                progress.Report($"下载失败: {file.Name} - {ex.Message}");
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task SaveMessageTextsAsync(List<MessageContent> messages, string channelName)
        {
            var textFilePath = Path.Combine(_savePath, $"{channelName}_messages.txt");
            var texts = messages
                .Where(m => !string.IsNullOrWhiteSpace(m.Text))
                .Select(m => $"[Message {m.Id}]\n{m.Text}\n\n");

            await File.WriteAllTextAsync(textFilePath, string.Join("", texts));
        }

        private async Task SaveLinksAsync(List<MessageContent> messages, string channelName)
        {
            var linksFilePath = Path.Combine(_savePath, $"{channelName}_links.txt");
            var links = messages
                .SelectMany(m => m.Links)
                .Distinct();

            await File.WriteAllLinesAsync(linksFilePath, links);
        }

        private async Task<List<MessageContent>> LoadChannelMessagesAsync(
            IProgress<string> progress,
            CancellationToken cancellationToken,
            int maxMessages = 10)
        {
            var messages = new List<MessageContent>();
            try
            {
                // 1. 等待消息容器加载
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
                var messageContainer = wait.Until(d => d.FindElement(By.CssSelector(".bubbles")));
                _logger.Debug("找到消息容器");

                // 2. 获取所有可见消息
                var allMessages = _driver.FindElements(By.CssSelector(".message"))
                    .OrderByDescending(m => m.Location.Y)
                    .ToList();
                
                _logger.Debug($"找到 {allMessages.Count} 条可见消息");

                // 3. 处理消息组
                for (int i = 0; i < allMessages.Count && messages.Count < maxMessages;)
                {
                    try
                    {
                        var currentMessage = allMessages[i];
                        await WaitForMessageLoad(currentMessage);

                        // 检查是否是文本消息
                        var hasText = currentMessage.FindElements(By.CssSelector("span.translatable-message")).Any();
                        
                        if (hasText)
                        {
                            // 收集当前文本消息和它下面的所有文件消息
                            var messageGroup = new List<IWebElement> { currentMessage };
                            int j = i + 1;
                            
                            // 继续查找直到遇到下一个文本消息或到达列表末尾
                            while (j < allMessages.Count)
                            {
                                var nextMessage = allMessages[j];
                                var nextHasText = nextMessage.FindElements(By.CssSelector("span.translatable-message")).Any();
                                
                                if (nextHasText)
                                {
                                    break; // 遇到下一个文本消息，停止收集
                                }
                                
                                messageGroup.Add(nextMessage);
                                j++;
                            }

                            // 处理整个消息组
                            var content = ProcessMessageGroup(messageGroup);
                            if (content != null && !string.IsNullOrWhiteSpace(content.Text))
                            {
                                messages.Add(content);
                                progress.Report($"已加载消息数量: {messages.Count}");
                                _logger.Info($"已加载消息数量: {messages.Count}");
                            }

                            i = j; // 跳到下一个文本消息的位置
                        }
                        else
                        {
                            i++; // 如果当前不是文本消息，继续下一个
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "处理消息组时出错");
                        i++; // 发生错误时继续处理下一条消息
                    }
                }

                LogMessages(messages);
                return messages;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载频道消息时出错");
                return messages;
            }
        }

        private bool IsRelatedMessage(IWebElement textMessage, IWebElement fileMessage)
        {
            try
            {
                // 检查时间戳是否接近
                var textTime = GetMessageTimestamp(textMessage);
                var fileTime = GetMessageTimestamp(fileMessage);
                
                if (textTime.HasValue && fileTime.HasValue)
                {
                    // 如果时间差在30秒内，认为是相关消息
                    return Math.Abs((textTime.Value - fileTime.Value).TotalSeconds) <= 30;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "检查消息关联性时出错");
                return false;
            }
        }

        private DateTime? GetMessageTimestamp(IWebElement message)
        {
            try
            {
                var timeElement = message.FindElement(By.CssSelector(".time .time-inner"));
                var timestamp = timeElement.GetAttribute("title");
                if (DateTime.TryParse(timestamp, out DateTime time))
                {
                    return time;
                }
            }
            catch { }
            return null;
        }

        private MessageContent ProcessMessageGroup(List<IWebElement> messageGroup)
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

                // 确保返回非空对象
                return content ?? new MessageContent { Id = Guid.NewGuid().ToString() };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "处理消息组时出错");
                // 返回一个空的消息内容对象而不是 null
                return new MessageContent { Id = Guid.NewGuid().ToString() };
            }
        }

        private void LogMessages(List<MessageContent> messages)
        {
            _logger.Info($"\n找到以下消息：\n----------------------------------------");
            foreach (var msg in messages)
            {
                _logger.Debug($"消息 ID: {msg.Id}");
                _logger.Debug($"标题: {msg.Text.Split('\n')[0]}");
                _logger.Debug($"内容: {msg.Text}");
                _logger.Debug($"链接数: {msg.Links.Count}");
                _logger.Debug($"文件数: {msg.Files.Count}");
                _logger.Debug("----------------------------------------");
            }
        }

        private async Task<bool> CheckIfAtTopAsync()
        {
            try
            {
                var result = await Task.Run(() => 
                {
                    return ((IJavaScriptExecutor)_driver).ExecuteScript(@"
                        const container = document.querySelector('.bubbles');
                        if (!container) return true;
                        return container.scrollTop <= 10;
                    ");
                });
                
                var isAtTop = Convert.ToBoolean(result);
                _logger.Debug($"检查是否到顶部: {isAtTop}");
                return isAtTop;
            }
            catch (Exception ex)
            {
                _logger.Error($"检查是否到顶部失败: {ex.Message}");
                return false;
            }
        }

        private async Task ScrollUpToNextMessage()
        {
            try
            {
                // 1. 获取当前滚动状态
                var scrollStatus = await _scrollHelper.GetScrollStatusAsync();
                if (scrollStatus != null)
                {
                    _logger.Debug($"当前滚动状态: {JsonSerializer.Serialize(scrollStatus)}");
                }

                // 2. 行智能滚动
                await _scrollHelper.SmartScrollAsync();
                await Task.Delay(1000);

                // 3. 验证滚动效果
                var newStatus = await _scrollHelper.GetScrollStatusAsync();
                if (newStatus != null)
                {
                    _logger.Debug($"滚动后状态: {JsonSerializer.Serialize(newStatus)}");

                    // 如果滚动无效，尝试使用备用方法
                    if (Math.Abs(newStatus.ScrollPosition - scrollStatus?.ScrollPosition ?? 0) < 1)
                    {
                        _logger.Debug("滚动无效，尝试备用方法");
                        await Task.Run(() => 
                        {
                            ((IJavaScriptExecutor)_driver).ExecuteScript(@"
                                const container = document.querySelector('.bubbles');
                                if (container) {
                                    const messages = Array.from(container.querySelectorAll('.message'));
                                    const visibleMessages = messages.filter(msg => {
                                        const rect = msg.getBoundingClientRect();
                                        return rect.top >= 0 && rect.bottom <= window.innerHeight;
                                    });
                                    
                                    if (visibleMessages.length > 0) {
                                        const firstVisible = visibleMessages[0];
                                        const index = messages.indexOf(firstVisible);
                                        if (index > 0) {
                                            // 滚动到前一条消息
                                            messages[Math.max(0, index - 1)].scrollIntoView({
                                                behavior: 'instant',
                                                block: 'center'
                                            });
                                        }
                                    }
                                }
                            ");
                        });
                        await Task.Delay(1000);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"向上滚动失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private async Task ScrollToBottom()
        {
            try
            {
                // 使用 ScrollHelper 滚动到底部
                var scrollStatus = await _scrollHelper.GetScrollStatusAsync();
                if (scrollStatus != null && !(scrollStatus.IsAtBottom ?? false))
                {
                    await Task.Run(() => 
                    {
                        ((IJavaScriptExecutor)_driver).ExecuteScript(@"
                            const container = document.querySelector('.bubbles');
                            if (container) {
                                container.scrollTop = container.scrollHeight;
                                container.dispatchEvent(new Event('scroll'));
                            }
                        ");
                    });
                    await Task.Delay(1000);
                }
                _logger.Debug("已滚动到底部");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "滚动到底部失败");
            }
        }

        private bool IsElementVisible(IWebElement element)
        {
            return _scrollHelper.IsElementInViewport(element);
        }

        private async Task ProcessMessageElement(IWebElement messageElement)
        {
            try
            {
                _logger.Debug($"正在处理消息元素: {messageElement.GetAttribute("outerHTML")}");
                
                // 1. 等待消息元素完全加载
                await WaitForMessageLoad(messageElement);
                
                // 2. 处理消息内容
                var content = _messageProcessor.ProcessMessage(messageElement);
                
                // 3. 验证消息内容
                if (!_messageProcessor.ValidateMessageContent(content))
                {
                    _logger.Debug("消息验证失败，跳过处理");
                    return;
                }

                // 4. 记录消息信息
                _messages.Add(content);
                
                _logger.Debug($"处理新消息 ID: {content.Id}");
                _logger.Debug($"消息内容: {content.Text}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "处理消息元素时出错");
            }
        }

        private async Task WaitForMessageLoad(IWebElement messageElement)
        {
            try
            {
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
                await Task.Run(() => {
                    wait.Until(d => {
                        try
                        {
                            // 检查消息是否完全加载
                            var isLoaded = messageElement.FindElements(By.CssSelector(
                                ".document-container, " +
                                ".media-container, " +
                                ".media-photo-container, " +
                                ".translatable-message"
                            )).Any();

                            if (isLoaded)
                            {
                                _logger.Debug("消息元素已加载完成");
                                return true;
                            }
                            return false;
                        }
                        catch
                        {
                            return false;
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "等待消息加载超时");
            }
        }
    }
}