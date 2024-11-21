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

                // 处理消息文本
                var messageText = await ExtractMessageText(message);
                if (!string.IsNullOrEmpty(messageText))
                {
                    await File.WriteAllTextAsync(
                        Path.Combine(messageFolder, "message.txt"),
                        messageText,
                        cancellationToken
                    );
                }

                // 处理媒体文件
                var mediaElements = message.FindElements(By.CssSelector(".media-container"));
                foreach (var media in mediaElements)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    await ProcessMediaElement(media, messageFolder, progress, cancellationToken);
                }

                // 处理链接
                var links = message.FindElements(By.CssSelector("a[href]"));
                await ProcessLinks(links, messageFolder, cancellationToken);

                progress.Report($"消息 {Path.GetFileName(messageFolder)} 处理完成");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"处理消息失败: {messageFolder}");
                progress.Report($"处理消息失败: {ex.Message}");
            }
        }

        private async Task<string> ExtractMessageText(IWebElement message)
        {
            try
            {
                var textElement = message.FindElement(By.CssSelector(".text-content"));
                return await Task.FromResult(textElement.Text);
            }
            catch
            {
                return string.Empty;
            }
        }

        private async Task ProcessMediaElement(IWebElement media, string messageFolder,
            IProgress<string> progress, CancellationToken cancellationToken)
        {
            try
            {
                var mediaUrl = media.GetAttribute("src") ?? 
                              media.FindElement(By.CssSelector("img,video")).GetAttribute("src");

                if (!string.IsNullOrEmpty(mediaUrl))
                {
                    await _downloadManager.DownloadFileAsync(
                        mediaUrl,
                        messageFolder,
                        progress,
                        cancellationToken
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "处理媒体元素失败");
            }
        }

        private async Task ProcessLinks(IReadOnlyCollection<IWebElement> links, 
            string messageFolder, CancellationToken cancellationToken)
        {
            var linkList = new List<string>();
            foreach (var link in links)
            {
                var href = link.GetAttribute("href");
                if (!string.IsNullOrEmpty(href))
                {
                    linkList.Add(href);
                }
            }

            if (linkList.Any())
            {
                await File.WriteAllLinesAsync(
                    Path.Combine(messageFolder, "links.txt"),
                    linkList,
                    cancellationToken
                );
            }
        }
    }
} 