using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using NLog;
using TelegramAutomation.Models;

namespace TelegramAutomation.Services
{
    public class MessageProcessor
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly DownloadManager _downloadManager;
        private readonly DownloadConfiguration _config;

        public MessageProcessor(DownloadManager downloadManager, DownloadConfiguration config)
        {
            _downloadManager = downloadManager;
            _config = config;
        }

        public async Task ProcessMessage(IWebElement message, string messageFolder, 
            IProgress<string> progress, CancellationToken cancellationToken)
        {
            try
            {
                Directory.CreateDirectory(messageFolder);

                // 保存消息文本
                if (_config.SaveMessageText)
                {
                    var messageText = message.FindElement(By.CssSelector(".text-content"))?.Text ?? "";
                    if (!string.IsNullOrEmpty(messageText))
                    {
                        await File.WriteAllTextAsync(
                            Path.Combine(messageFolder, "message.txt"),
                            messageText,
                            cancellationToken
                        );
                    }
                }

                // 保存链接
                if (_config.SaveLinks)
                {
                    var links = message.FindElements(By.TagName("a"))
                        .Select(a => a.GetAttribute("href"))
                        .Where(href => !string.IsNullOrEmpty(href))
                        .ToList();

                    if (links.Any())
                    {
                        await File.WriteAllLinesAsync(
                            Path.Combine(messageFolder, "links.txt"),
                            links,
                            cancellationToken
                        );
                    }
                }

                // 处理下载文件
                await ProcessDownloadableFiles(message, messageFolder, progress, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "处理消息时出错");
                throw;
            }
        }

        private async Task ProcessDownloadableFiles(IWebElement message, string messageFolder,
            IProgress<string> progress, CancellationToken cancellationToken)
        {
            try
            {
                var downloadLinks = new List<string>();
                
                // 查找直接下载链接
                var directLinks = message.FindElements(By.CssSelector("a[href]"))
                    .Select(a => a.GetAttribute("href"))
                    .Where(href => !string.IsNullOrEmpty(href) && 
                           _config.SupportedFileExtensions.Any(ext => 
                               href.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                downloadLinks.AddRange(directLinks);
                
                // 查找媒体文件
                var mediaElements = message.FindElements(By.CssSelector(".media-container"));
                foreach (var media in mediaElements)
                {
                    try
                    {
                        var downloadButton = media.FindElement(By.CssSelector(".download-button"));
                        var downloadUrl = downloadButton.GetAttribute("href");
                        if (!string.IsNullOrEmpty(downloadUrl))
                        {
                            downloadLinks.Add(downloadUrl);
                        }
                    }
                    catch (NoSuchElementException)
                    {
                        // 忽略没有下载按钮的媒体元素
                        continue;
                    }
                }

                foreach (var link in downloadLinks)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    
                    var fileName = Path.GetFileName(new Uri(link).LocalPath);
                    if (string.IsNullOrEmpty(fileName))
                    {
                        fileName = $"file_{Guid.NewGuid()}{Path.GetExtension(link)}";
                    }
                    
                    var filePath = Path.Combine(messageFolder, fileName);
                    
                    try
                    {
                        await _downloadManager.DownloadFile(link, filePath, progress, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"下载文件失败: {link}");
                        progress.Report($"下载失败: {fileName} - {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "处理可下载文件时出错");
                throw;
            }
        }
    }
} 