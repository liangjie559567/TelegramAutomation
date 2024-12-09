using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using NLog;
using TelegramAutomation.Models;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;

namespace TelegramAutomation.Services
{
    public class MessageProcessor
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly IWebDriver _driver;

        public MessageProcessor(IWebDriver driver)
        {
            _driver = driver;
        }

        private (string title, string text) ExtractTitleAndContent(IWebElement messageElement)
        {
            try
            {
                _logger.Debug("开始提取消息内容...");
                string extractedTitle = string.Empty;
                string extractedContent = string.Empty;

                // 1. 尝试获取消息文本内容
                var textElements = messageElement.FindElements(By.CssSelector(
                    "div.message-content span.text-content, " +
                    "div.message div[dir='auto'], " +
                    "span.translatable-message"
                ));

                if (textElements.Any())
                {
                    var allText = string.Join("\n", textElements.Select(e => e.Text.Trim()));
                    var lines = allText.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                     .Select(l => l.Trim())
                                     .Where(l => !string.IsNullOrEmpty(l))
                                     .Where(l => !l.StartsWith("http"))
                                     .Where(l => !l.Contains("https://"))
                                     .Where(l => !l.StartsWith("#"))
                                     .ToList();

                    if (lines.Any())
                    {
                        // 提取标题（第一行）
                        extractedTitle = lines[0];
                        
                        // 提取内容（剩余行，排除链接和标签）
                        if (lines.Count > 1)
                        {
                            var contentLines = lines.Skip(1)
                                .Select(line => CleanContentLine(line))
                                .Where(line => !string.IsNullOrWhiteSpace(line));
                            
                            extractedContent = string.Join("\n", contentLines);
                        }
                    }
                }

                _logger.Debug($"提取到的标题: {extractedTitle}");
                _logger.Debug($"提取到的内容: {extractedContent}");
                
                return (extractedTitle, extractedContent);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "提取消息内容时出错");
                return (string.Empty, string.Empty);
            }
        }

        private string CleanContentLine(string line)
        {
            try
            {
                // 1. 移除 URL
                var urlPatterns = new[] {
                    @"(http|https):\/\/[\w\-_]+(\.[\w\-_]+)+",
                    @"Fab\.com",
                    @"www\.\w+\.\w+"
                };
                
                // 2. 移除标签
                var hashtagPattern = @"#\w+";
                
                // 3. 移除版本号
                var versionPatterns = new[] {
                    @"v\d+(\.\d+)*",
                    @"UE\d+(\.\d+)*",
                    @"version\s+\d+(\.\d+)*"
                };
                
                // 4. 移除无用文本
                var removeTexts = new[] {
                    "NEW", "OVERVIEW", "VIDEO",
                    "also", "Tutorial", "Download"
                };
                
                foreach (var pattern in urlPatterns)
                {
                    line = Regex.Replace(line, pattern, string.Empty, RegexOptions.IgnoreCase);
                }
                
                line = Regex.Replace(line, hashtagPattern, string.Empty, RegexOptions.IgnoreCase);
                
                foreach (var pattern in versionPatterns)
                {
                    line = Regex.Replace(line, pattern, string.Empty);
                }
                
                foreach (var text in removeTexts)
                {
                    line = Regex.Replace(line, $@"\b{text}\b", string.Empty, RegexOptions.IgnoreCase);
                }

                // 5. 清理结果
                line = line.Trim();
                line = Regex.Replace(line, @"\s+", " ");  // 再次合并空格
                line = Regex.Replace(line, @"^[-–—]\s*", string.Empty);  // 再次清理行首
                line = Regex.Replace(line, @"\s*[-–—]\s*$", string.Empty);  // 清理行尾

                return line;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "清理内容行时出错");
                return line;
            }
        }

        public MessageContent ProcessMessage(IWebElement messageElement)
        {
            var content = new MessageContent { Id = Guid.NewGuid().ToString() };
            
            // 1. 提取文本内容
            var (title, text) = ExtractTitleAndContent(messageElement);
            content.Text = string.IsNullOrEmpty(text) ? title : $"{title}\n{text}";
            
            // 2. 提取文件
            content.Files = ExtractFiles(messageElement);
            
            // 3. 提取链接
            content.Links = ExtractLinks(messageElement);
            
            return content;
        }

        public bool ValidateMessageContent(MessageContent content)
        {
            try
            {
                // 检查是否有任何有效内容
                bool hasContent = !string.IsNullOrWhiteSpace(content.Text) || 
                                 content.Files.Any() || 
                                 content.Links.Any();

                if (!hasContent)
                {
                    _logger.Debug("消息没有任何有效内容");
                    return false;
                }

                // 检查标题是否为文件名
                var title = content.Text?.Split('\n').FirstOrDefault()?.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(title))
                {
                    var isJustFileName = content.Files.Any(f => 
                        Path.GetFileNameWithoutExtension(f.Name).Equals(title, StringComparison.OrdinalIgnoreCase));
                    
                    if (isJustFileName)
                    {
                        _logger.Debug("标题不能仅为文件名");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "验证消息内容时出错");
                return false;
            }
        }

        private string FormatMessageContent(MessageContent content)
        {
            var sb = new StringBuilder();

            // 1. 标题
            var title = content.Text?.Split('\n').FirstOrDefault()?.Trim() ?? string.Empty;
            sb.AppendLine($"标题: {title}");

            // 2. 内容
            sb.AppendLine("内容：");
            var description = content.Text?.Split('\n')
                .Skip(1)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Where(l => !l.StartsWith("http"))
                .Where(l => !l.Contains("https://"))
                .Where(l => !l.StartsWith("#"))
                .ToList();

            if (description?.Any() == true)
            {
                sb.AppendLine(string.Join("\n", description));
            }

            // 3. 链接
            if (content.Links.Any())
            {
                sb.AppendLine("链接:");
                foreach (var link in content.Links)
                {
                    sb.AppendLine(link);
                }
            }

            // 4. 文件
            if (content.Files.Any())
            {
                sb.AppendLine("文件:");
                foreach (var file in content.Files)
                {
                    sb.AppendLine($"{file.Name} ({file.Size})");
                }
            }

            return sb.ToString();
        }

        private void LogMessageContent(MessageContent content)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"处理消息 ID: {content.Id}");
            
            // 分离标题和内容
            var lines = content.Text?.Split('\n', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            var title = lines.FirstOrDefault() ?? string.Empty;
            
            sb.AppendLine($"标题：{title}");
            sb.AppendLine("内容：");
            
            // 添加剩余内容
            if (lines.Length > 1)
            {
                sb.AppendLine(string.Join("\n", lines.Skip(1)));
            }

            // 添加文件信息
            if (content.Files.Any())
            {
                sb.AppendLine("文件:");
                foreach (var file in content.Files)
                {
                    sb.AppendLine($"{file.Name} ({file.Size})");
                }
            }

            // 添加链接信息
            if (content.Links.Any())
            {
                sb.AppendLine("链接:");
                foreach (var link in content.Links)
                {
                    sb.AppendLine(link);
                }
            }

            _logger.Debug(sb.ToString());
        }

        public List<MessageContent.FileInfo> ExtractFiles(IWebElement messageElement)
        {
            var files = new List<MessageContent.FileInfo>();
            try
            {
                // 1. 查找所有文件容器
                var containers = messageElement.FindElements(By.CssSelector(
                    ".document-container, " +
                    ".media-container:not(.webpage-preview), " +
                    ".media-photo-container"
                ));

                _logger.Debug($"找到 {containers.Count} 个文件容器");

                foreach (var container in containers)
                {
                    _logger.Debug($"处理容器: {container.GetAttribute("outerHTML")}");

                    // 处理文档类型
                    if (container.GetAttribute("class").Contains("document-container"))
                    {
                        var nameElement = container.FindElement(By.CssSelector(".document-name"));
                        var sizeElement = container.FindElement(By.CssSelector(".document-size .i18n"));
                        var extElement = container.FindElement(By.CssSelector(".document"));
                        
                        var fileName = nameElement.Text.Trim();
                        var fileSize = sizeElement.Text.Trim();
                        var fileType = extElement.GetAttribute("class")
                            .Split(' ')
                            .FirstOrDefault(c => c.StartsWith("ext-"))
                            ?.Replace("ext-", "")
                            .ToUpper() ?? "UNKNOWN";

                        files.Add(new MessageContent.FileInfo
                        {
                            Name = fileName,
                            Size = fileSize,
                            Type = fileType
                        });
                        _logger.Debug($"添加文档: {fileName} ({fileSize}) [{fileType}]");
                    }
                    // 处理图片类型
                    else if (container.GetAttribute("class").Contains("media"))
                    {
                        var imgElement = container.FindElement(By.CssSelector("img"));
                        var src = imgElement.GetAttribute("src");
                        var fileSize = GetImageSize(imgElement);
                        var fileName = $"image_{DateTime.Now.Ticks}.jpg";

                        files.Add(new MessageContent.FileInfo
                        {
                            Name = fileName,
                            Size = fileSize,
                            Type = "IMAGE",
                            Url = src
                        });
                        _logger.Debug($"添加图片: {fileName} ({fileSize})");
                    }
                }

                _logger.Debug($"文件提取完成，共找到 {files.Count} 个文件");
                return files;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "提取文件时出错");
                return files;
            }
        }

        private string GetImageSize(IWebElement imgElement)
        {
            try
            {
                var naturalWidth = ((IJavaScriptExecutor)_driver).ExecuteScript("return arguments[0].naturalWidth;", imgElement);
                var naturalHeight = ((IJavaScriptExecutor)_driver).ExecuteScript("return arguments[0].naturalHeight;", imgElement);
                
                if (naturalWidth != null && naturalHeight != null)
                {
                    return $"{naturalWidth}x{naturalHeight}";
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "获取图片尺寸失败");
            }
            return "未知";
        }

        public List<string> ExtractLinks(IWebElement messageElement)
        {
            var links = new HashSet<string>();
            try
            {
                // 修改链接选择器
                var linkElements = messageElement.FindElements(By.CssSelector(
                    "a[href]:not([href^='tg://']):not([href*='hashtag']):not([href*='javascript'])"
                ));

                foreach (var linkElement in linkElements)
                {
                    var href = linkElement.GetAttribute("href")?.Trim();
                    if (!string.IsNullOrEmpty(href) && 
                        !href.Contains("hashtag") && 
                        !href.Contains("javascript:") &&
                        Uri.TryCreate(href, UriKind.Absolute, out _))
                    {
                        links.Add(href);
                    }
                }

                _logger.Debug($"提取到的接数量: {links.Count}");
                foreach (var link in links)
                {
                    _logger.Debug($"找到链接: {link}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "提取链接时出错");
            }
            return links.ToList();
        }

        public MessageContent? ProcessMessageGroup(List<IWebElement> messageGroup)
        {
            if (messageGroup == null || !messageGroup.Any())
                return null;

            var content = new MessageContent();
            var mainMessage = messageGroup.First();

            try
            {
                _logger.Debug("开始提取消息内容...");
                
                // 提取标题和内容
                var textElements = mainMessage.FindElements(By.CssSelector(
                    "span.translatable-message, div.message-content span.text-content"
                ));

                if (textElements != null && textElements.Any())
                {
                    var fullText = string.Join("\n", textElements.Select(e => e.Text.Trim()));
                    var lines = fullText.Split('\n')
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();

                    if (lines.Any())
                    {
                        content.Title = lines[0].Trim();
                        _logger.Debug($"提取到的标题: {content.Title}");

                        if (lines.Count > 1)
                        {
                            content.Text = string.Join("\n", lines);
                            _logger.Debug($"提取到的内容: {content.Text}");
                        }
                    }
                }

                // 处理所有消息中的文件
                foreach (var message in messageGroup)
                {
                    try
                    {
                        var fileContainers = message.FindElements(By.CssSelector(
                            ".document-container, .media-container, .media-photo-container"
                        ));
                        
                        _logger.Debug($"找到 {fileContainers?.Count ?? 0} 个文件容器");
                        
                        if (fileContainers != null && fileContainers.Any())
                        {
                            foreach (var container in fileContainers)
                            {
                                _logger.Debug($"处理容器: {container.GetAttribute("outerHTML")}");
                                
                                var file = new MessageContent.FileInfo();
                                
                                // 获取文件名
                                try
                                {
                                    var nameElement = container.FindElement(By.CssSelector(".document-name"));
                                    file.Name = nameElement.Text.Trim();
                                }
                                catch (Exception ex)
                                {
                                    _logger.Debug($"提取文件名时出错: {ex.Message}");
                                    continue;
                                }

                                // 获取文件大小
                                try
                                {
                                    var sizeElement = container.FindElement(By.CssSelector(".document-size"));
                                    file.Size = sizeElement.Text.Split('·')[0].Trim();
                                }
                                catch (Exception ex)
                                {
                                    _logger.Debug($"提取文件大小时出错: {ex.Message}");
                                }

                                // 获取文件类型
                                try
                                {
                                    var typeElement = container.FindElement(By.CssSelector(".document-ico-text"));
                                    file.Type = typeElement.Text.Trim().ToUpper();
                                }
                                catch (Exception ex)
                                {
                                    _logger.Debug($"提取文件类型时出错: {ex.Message}");
                                    // 尝试从文件名获取类型
                                    file.Type = Path.GetExtension(file.Name)?.TrimStart('.')?.ToUpper() ?? "未知";
                                }

                                if (!string.IsNullOrWhiteSpace(file.Name))
                                {
                                    _logger.Debug($"添加文档: {file.Name} ({file.Size}) [{file.Type}]");
                                    content.Files.Add(file);
                                }
                            }
                        }
                        
                        _logger.Debug($"文件提取完成，共找到 {content.Files.Count} 个文件");

                        // 提取链接
                        var links = message.FindElements(By.CssSelector("a.anchor-url"))
                            .Select(a => a.GetAttribute("href"))
                            .Where(href => !string.IsNullOrWhiteSpace(href))
                            .ToList();

                        _logger.Debug($"提取到的链接数量: {links.Count}");
                        foreach (var link in links)
                        {
                            _logger.Debug($"找到链接: {link}");
                            if (!content.Links.Contains(link))
                            {
                                content.Links.Add(link);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "处理消息中的文件和链接时出错");
                    }
                }

                return content;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "处理消息组时出错");
                return null;
            }
        }
    }
} 