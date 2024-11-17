using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NLog;

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

        try
        {
            using var client = new HttpClient();
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            
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
                    status.Progress = percentage;
                    status.Status = $"下载中 {percentage}%";
                    progress.Report($"下载进度: {percentage}%");
                }
            }

            status.Status = "下载完成";
            progress.Report("下载完成");
        }
        catch (Exception ex)
        {
            status.Status = $"下载失败: {ex.Message}";
            _logger.Error(ex, "文件下载失败");
            throw;
        }
        finally
        {
            _downloads.TryRemove(downloadId, out _);
        }
    }

    private class DownloadStatus
    {
        public string Status { get; set; } = string.Empty;
        public int Progress { get; set; }
    }
} 