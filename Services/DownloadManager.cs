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
        private readonly DownloadConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly SemaphoreSlim _semaphore;

        public DownloadManager(DownloadConfiguration config)
        {
            _config = config;
            _httpClient = new HttpClient();
            _semaphore = new SemaphoreSlim(config.MaxConcurrentDownloads);
        }

        public async Task DownloadFileAsync(string url, string folder, 
            IProgress<string> progress, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var fileName = Path.GetFileName(new Uri(url).LocalPath);
                var filePath = Path.Combine(folder, fileName);

                progress.Report($"开始下载: {fileName}");

                using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var fileStream = File.Create(filePath);
                await stream.CopyToAsync(fileStream, cancellationToken);

                progress.Report($"下载完成: {fileName}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"下载失败: {url}");
                progress.Report($"下载失败: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }
    } 
} 