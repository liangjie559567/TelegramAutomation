using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using NLog;
using TelegramAutomation.Models;

namespace TelegramAutomation.Services
{
    public class DownloadManager
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly ConcurrentDictionary<string, DownloadStatus> _downloads = new();
        private readonly DownloadConfiguration _config;

        public DownloadManager(DownloadConfiguration config)
        {
            _config = config;
        }

        public async Task DownloadFile(string url, string destinationPath, IProgress<string> progress, CancellationToken cancellationToken)
        {
            var downloadId = Guid.NewGuid().ToString();
            var status = new DownloadStatus { Status = "准备下载" };
            _downloads[downloadId] = status;

            for (int retry = 0; retry < 3; retry++)
            {
                try
                {
                    using var client = new HttpClient();
                    client.Timeout = TimeSpan.FromMinutes(30);
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/122.0.0.0");
                    
                    using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920);
                    
                    var buffer = new byte[81920];
                    var totalBytesRead = 0L;
                    var lastProgressReport = DateTime.MinValue;

                    while (true)
                    {
                        var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
                        if (bytesRead == 0) break;
                        
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                        totalBytesRead += bytesRead;

                        if (totalBytes > 0 && DateTime.Now - lastProgressReport > TimeSpan.FromMilliseconds(100))
                        {
                            var percentage = (int)((totalBytesRead * 100) / totalBytes);
                            status.Progress = percentage;
                            status.Status = $"下载中 {percentage}%";
                            progress.Report($"下载进度: {percentage}% - {Path.GetFileName(destinationPath)}");
                            lastProgressReport = DateTime.Now;
                        }
                    }

                    status.Status = "下载完成";
                    progress.Report($"下载完成: {Path.GetFileName(destinationPath)}");
                    return;
                }
                catch (Exception ex) when (retry < 2)
                {
                    status.Status = $"下载重试 ({retry + 1}/3)";
                    _logger.Warn(ex, $"下载失败，准备重试 ({retry + 1}/3)");
                    await Task.Delay(1000, cancellationToken);
                }
                catch (Exception ex)
                {
                    status.Status = $"下载失败: {ex.Message}";
                    _logger.Error(ex, "文件下载失败");
                    throw;
                }
            }
        }

        private class DownloadStatus
        {
            public string Status { get; set; } = string.Empty;
            public int Progress { get; set; }
        }
    } 
} 