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
            var downloadLinks = message.FindElements(By.CssSelector("a[href]"))
                .Select(a => a.GetAttribute("href"))
                .Where(href => _config.SupportedFileExtensions.Any(ext => 
                    href?.EndsWith(ext, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();

            foreach (var link in downloadLinks)
            {
                var fileName = Path.GetFileName(new Uri(link).LocalPath);
                var filePath = Path.Combine(messageFolder, fileName);

                await _downloadManager.DownloadFile(link, filePath, progress, cancellationToken);
            }
        }
    }
} 