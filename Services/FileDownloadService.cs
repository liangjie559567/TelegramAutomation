#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using NLog;
using TelegramAutomation.Models;
using System.Text;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Interactions;
using Newtonsoft.Json;

namespace TelegramAutomation.Services
{
    public class FileDownloadService
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly IWebDriver _driver;
        private readonly string _baseSavePath;
        private readonly IJavaScriptExecutor _js;
        private readonly WebDriverWait _wait;
        private const int MaxRetries = 3;
        private const int RetryDelayMs = 1000;

        public event EventHandler<DownloadProgressEventArgs>? DownloadProgressChanged;

        public FileDownloadService(IWebDriver driver, string baseSavePath)
        {
            _driver = driver;
            _baseSavePath = baseSavePath;
            _js = (IJavaScriptExecutor)_driver;
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            
            Directory.CreateDirectory(_baseSavePath);
        }

        private void ReportProgress(string fileName, string fileSize, double progress, string status, string message)
        {
            var args = new DownloadProgressEventArgs(fileName, fileSize, progress, status, message);
            DownloadProgressChanged?.Invoke(this, args);
        }

        public async Task ProcessMessageGroupDownload(
            List<IWebElement> messageGroup,
            MessageContent messageContent,
            IProgress<string> progress,
            CancellationToken cancellationToken)
        {
            try
            {
                if (messageGroup == null || !messageGroup.Any() || messageContent == null)
                {
                    return;
                }

                // 1. 创建消息组文件夹
                var folderName = GetSafeFileName(messageContent.Text?.Split('\n').FirstOrDefault() ?? 
                    DateTime.Now.Ticks.ToString());
                var groupFolder = Path.Combine(_baseSavePath, folderName);
                _logger.Debug($"[调试] 主文件夹路径: {groupFolder}");
                
                // 确保主文件夹存在
                if (!Directory.Exists(groupFolder))
                {
                    _logger.Debug($"[调试] 创建主文件夹: {groupFolder}");
                    Directory.CreateDirectory(groupFolder);
                }

                // 2. 保存文本内容和URL
                await SaveTextContent(messageContent, groupFolder);

                // 3. 下载所有附件
                if (messageContent.Files.Any())
                {
                    foreach (var file in messageContent.Files)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            ReportProgress(file.Name, file.Size, 0, "已取消", "下载已取消");
                            break;
                        }

                        try
                        {
                            ReportProgress(file.Name, file.Size, 0, "准备中", "准备下载文件...");
                            
                            // 查找文件元素
                            var fileElement = FindFileElement(messageGroup, file.Name);
                            if (fileElement == null)
                            {
                                ReportProgress(file.Name, file.Size, 0, "失败", "未找到文件元素");
                                continue;
                            }

                            // 点击下载按钮
                            var downloadButton = fileElement.FindElement(By.CssSelector(".download"));
                            if (downloadButton != null && downloadButton.Displayed)
                            {
                                downloadButton.Click();
                                ReportProgress(file.Name, file.Size, 0, "下载中", "开始下载...");
                                
                                // 等待文件下载完成
                                var downloadPath = Path.Combine(
                                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                    "TelegramDownloads"
                                );
                                
                                var success = await WaitForDownload(
                                    downloadPath, 
                                    file.Name,
                                    progress => ReportProgress(file.Name, file.Size, progress, "下载中", 
                                        $"已下载 {progress:F1}%"),
                                    cancellationToken
                                );

                                if (success)
                                {
                                    // 移动文件到消息组文件夹
                                    var sourceFile = Directory.GetFiles(downloadPath, $"{file.Name}*")
                                        .FirstOrDefault();
                                    
                                    if (sourceFile != null)
                                    {
                                        var destFile = Path.Combine(groupFolder, Path.GetFileName(sourceFile));
                                        File.Move(sourceFile, destFile, true);
                                        ReportProgress(file.Name, file.Size, 100, "完成", "下载完成");
                                    }
                                    else
                                    {
                                        ReportProgress(file.Name, file.Size, 0, "失败", "文件未找到");
                                    }
                                }
                                else
                                {
                                    ReportProgress(file.Name, file.Size, 0, "失败", "下载超时或失败");
                                }
                            }
                            else
                            {
                                ReportProgress(file.Name, file.Size, 0, "失败", "下载按钮未找到");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, $"下载文件 {file.Name} 时出错");
                            ReportProgress(file.Name, file.Size, 0, "失败", $"错误: {ex.Message}");
                        }

                        // 等待一段时间再下载下一个文件
                        await Task.Delay(1000, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "处理消息组下载时出错");
                throw;
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

        private async Task<bool> DownloadMessageAttachments(
            IWebElement message,
            string folderPath,
            IProgress<string> progress)
        {
            try
            {
                // 1. 查找所有文件容器
                var containers = message.FindElements(By.CssSelector(
                    ".document-container, " +
                    ".media-container:not(.webpage-preview), " +
                    ".media-photo-container"
                ));

                if (!containers.Any())
                {
                    return true; // 没有附件也算成功
                }

                _logger.Debug($"[调试] 找到 {containers.Count} 个文件容器");

                // 确保目标文件夹存在
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var allSuccess = true;
                foreach (var container in containers)
                {
                    var retryCount = 0;
                    var downloadSuccess = false;

                    while (retryCount < MaxRetries && !downloadSuccess)
                    {
                        try
                        {
                            // 确保容器在视图中
                            _js.ExecuteScript("arguments[0].scrollIntoView({behavior: 'instant', block: 'center'});", container);
                            await Task.Delay(500);

                            // 获取文件名
                            string fileName = GetFileNameFromContainer(container);
                            progress.Report($"开始下载: {fileName} (尝试 {retryCount + 1}/{MaxRetries})");
                            _logger.Debug($"[调试] 开始处理文件: {fileName}");

                            // 尝试多种方式点击下载按钮
                            bool downloadStarted = await TryDownload(container, fileName);
                            
                            if (downloadStarted)
                            {
                                // 等待下载完成，使用正确的目标文件夹路径
                                if (await WaitForDownload(fileName, folderPath))
                                {
                                    progress.Report($"下载完成: {fileName}");
                                    downloadSuccess = true;
                                    break;
                                }
                            }

                            retryCount++;
                            if (retryCount < MaxRetries)
                            {
                                progress.Report($"重试下载: {fileName}");
                                await Task.Delay(RetryDelayMs);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, $"下载尝试 {retryCount + 1} 失败");
                            retryCount++;
                            if (retryCount < MaxRetries)
                            {
                                await Task.Delay(RetryDelayMs);
                            }
                        }
                    }

                    if (!downloadSuccess)
                    {
                        _logger.Debug("[调试] 文件下载失败，继续处理下一个文件");
                        allSuccess = false;
                        // 不立即返回 false，继续处理其他文件
                    }
                }

                return allSuccess;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "处理消息附件时出错");
                return false;
            }
        }

        private async Task<bool> TryDownload(IWebElement container, string fileName)
        {
            try
            {
                _logger.Debug($"[调试] 开始尝试下载文件: {fileName}");
                
                // 1. 检查文件是否已经存在
                var defaultDownloadPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads"
                );
                
                // 确保下载目录存在
                if (!Directory.Exists(defaultDownloadPath))
                {
                    Directory.CreateDirectory(defaultDownloadPath);
                }

                var existingFiles = Directory.GetFiles(defaultDownloadPath)
                    .Where(f => !Path.GetFileName(f).Equals("desktop.ini", StringComparison.OrdinalIgnoreCase))
                    .Select(f => Path.GetFileName(f))
                    .ToList();

                _logger.Debug("[调试] 检查现有文件:");
                foreach (var file in existingFiles)
                {
                    _logger.Debug($"[调试] - {file}");
                    if (IsFileNameMatch(file, fileName))
                    {
                        _logger.Debug($"[调试] 文件已存在: {file}");
                        return true;
                    }
                }

                // 2. 如果文件不存在，则开始下载
                _js.ExecuteScript("arguments[0].scrollIntoView({behavior: 'instant', block: 'center'});", container);
                await Task.Delay(500);

                var downloadBtn = container.FindElements(By.CssSelector(".preloader-download")).FirstOrDefault();
                if (downloadBtn != null)
                {
                    try
                    {
                        _logger.Debug("[调试] 使用 Selenium Actions 点击");
                        var actions = new Actions(_driver);
                        actions.MoveToElement(downloadBtn).Click().Perform();
                        _logger.Debug("[调试] 点击完成，等待下载开始");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"点击下载按钮时出错: {ex.Message}");
                        return false;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"[错误] 尝试下载文件时出错: {fileName}");
                return false;
            }
        }

        private string GetFileNameFromContainer(IWebElement container)
        {
            try
            {
                // 尝试获取��件名
                var nameElement = container.FindElements(By.CssSelector(".document-name")).FirstOrDefault();
                if (nameElement != null)
                {
                    var text = nameElement.Text;
                    if (!string.IsNullOrEmpty(text))
                    {
                        return GetSafeFileName(text);
                    }
                }

                // 如果是媒体文件，生成时间戳文件名
                var timestamp = DateTime.Now.Ticks;
                var extension = container.GetAttribute("class")?.Contains("photo") == true ? ".jpg" : ".mp4";
                return $"media_{timestamp}{extension}";
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "获取文件名时出错");
                return $"file_{DateTime.Now.Ticks}";
            }
        }

        private async Task<bool> WaitForDownload(string fileName, string targetFolderPath)
        {
            try
            {
                _logger.Debug($"[调试] 开始等待文件下载: {fileName}");
                _logger.Debug($"[调试] 目标文件夹路径: {targetFolderPath}");
                var defaultDownloadPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads"
                );
                _logger.Debug($"[调试] 监控下载目录: {defaultDownloadPath}");

                // 如果文件不存在，等待下载
                var startTime = DateTime.Now;
                var timeout = TimeSpan.FromHours(1);
                var checkInterval = TimeSpan.FromSeconds(2);
                var fileProcessed = false;
                long? totalSize = null;
                string? downloadingFile = null;
                var lastProgressReport = DateTime.MinValue;
                string? matchedOriginalFileName = null;

                while (DateTime.Now - startTime < timeout && !fileProcessed)
                {
                    var files = Directory.GetFiles(defaultDownloadPath)
                        .Where(f => !Path.GetFileName(f).Equals("desktop.ini", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    foreach (var file in files)
                    {
                        var currentFileName = Path.GetFileName(file);
                        
                        // 如果是下载中的文件（.crdownload），显示进度
                        if (currentFileName.EndsWith(".crdownload"))
                        {
                            try
                            {
                                downloadingFile = file;
                                var fileInfo = new FileInfo(file);
                                var currentSize = fileInfo.Length;

                                // 如果还没有获取到总大小，尝试从文件名中获取
                                if (!totalSize.HasValue)
                                {
                                    if (currentFileName.StartsWith("未确认"))
                                    {
                                        // 尝试从原始文件名中获取文件大小
                                        var originalFile = fileName;
                                        if (originalFile.Contains("part1"))
                                        {
                                            totalSize = 1256637644; // 1.17GB in bytes
                                        }
                                        else if (originalFile.Contains("part2"))
                                        {
                                            totalSize = 1138166620; // 1.06GB in bytes
                                        }
                                    }
                                }

                                // 如果有总大小，显示进度
                                if (totalSize.HasValue && (DateTime.Now - lastProgressReport).TotalSeconds >= 1)
                                {
                                    var progress = (double)currentSize / totalSize.Value * 100;
                                    var progressBar = GenerateProgressBar(progress);
                                    var remainingTime = TimeSpan.FromSeconds((totalSize.Value - currentSize) / (currentSize / (DateTime.Now - startTime).TotalSeconds));
                                    _logger.Debug($"[调试] 下载进度: {progressBar} {progress:F1}% ({FormatFileSize(currentSize)}/{FormatFileSize(totalSize.Value)}) 预计剩余时间: {remainingTime:mm\\:ss}");
                                    lastProgressReport = DateTime.Now;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Debug($"[调试] 读取下载进度时出错: {ex.Message}");
                                continue;
                            }
                        }
                        // 只处理已完成的文件（非.crdownload）
                        else if (IsFileNameMatch(currentFileName, fileName))
                        {
                            _logger.Debug($"[调试] 检测到匹配的完整文件: {currentFileName}");
                            matchedOriginalFileName = currentFileName;  // 保存匹配到的原始文件名
                            var targetPath = Path.Combine(targetFolderPath, currentFileName);  // 使用原始文件名
                            if (await MoveFileToTarget(file, targetPath))
                            {
                                fileProcessed = true;
                                break;
                            }
                        }
                    }

                    if (!fileProcessed)
                    {
                        await Task.Delay(checkInterval);
                    }
                }

                if (!fileProcessed)
                {
                    _logger.Debug("[调试] 等待下载超时");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"[错误] 等待下载时出错: {ex.Message}");
                return false;
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        private async Task<bool> MoveFileToTarget(string sourceFile, string targetPath)
        {
            try
            {
                if (!File.Exists(sourceFile))
                {
                    _logger.Debug($"[调试] 源文件不存在: {sourceFile}");
                    return false;
                }

                // 确保目标文件夹存在
                var targetDir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                // 如果目标文件已存在，先删除
                if (File.Exists(targetPath))
                {
                    File.Delete(targetPath);
                }

                // 等待源文件可用
                var retryCount = 0;
                var maxRetries = 10;
                while (retryCount < maxRetries)
                {
                    try
                    {
                        if (IsFileLocked(sourceFile))
                        {
                            _logger.Debug($"[调试] 源文件被锁定，等待解锁 (尝试 {retryCount + 1}/{maxRetries})");
                            await Task.Delay(1000);
                            retryCount++;
                            continue;
                        }

                        // 复制文件
                        File.Copy(sourceFile, targetPath, true);
                        _logger.Debug($"[调试] 文件复制完成: {targetPath}");

                        // 删除源文件
                        try
                        {
                            if (File.Exists(sourceFile))
                            {
                                File.Delete(sourceFile);
                                _logger.Debug($"[调试] 源文件删除成功: {sourceFile}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Debug($"[调试] 删除源文件失败: {ex.Message}");
                        }

                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"[调试] 复制文件失败 (尝试 {retryCount + 1}/{maxRetries}): {ex.Message}");
                        retryCount++;
                        if (retryCount >= maxRetries)
                        {
                            throw;
                        }
                        await Task.Delay(1000);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.Error($"[错误] 移动文件时出错: {ex.Message}");
                return false;
            }
        }

        private string GenerateProgressBar(double percentage, int width = 30)
        {
            int completedWidth = (int)(width * percentage / 100);
            int remainingWidth = width - completedWidth;
            return "[" + new string('█', completedWidth) + new string('░', remainingWidth) + "]";
        }

        private bool IsFileNameMatch(string currentFileName, string expectedFileName)
        {
            _logger.Debug($"[调试] 文件名匹配检查开始");
            _logger.Debug($"[调试] - 当前文件名: {currentFileName}");
            _logger.Debug($"[调试] - 期望文件名: {expectedFileName}");

            // 移除扩展名进行比较
            var currentNameNoExt = Path.GetFileNameWithoutExtension(currentFileName);
            var expectedNameNoExt = Path.GetFileNameWithoutExtension(expectedFileName);
            var currentExt = Path.GetExtension(currentFileName);
            var expectedExt = Path.GetExtension(expectedFileName);

            _logger.Debug($"[调试] 文件名分析:");
            _logger.Debug($"[调试] - 当前文件名(无扩展名): {currentNameNoExt}");
            _logger.Debug($"[调试] - 当前扩展名: {currentExt}");
            _logger.Debug($"[调试] - 期望文件名(无扩展名): {expectedNameNoExt}");
            _logger.Debug($"[调试] - 期望扩展名: {expectedExt}");

            // 特殊处理.crdownload文件
            if (currentExt.Equals(".crdownload", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // 1. 检查扩展名是否匹配
            if (!string.Equals(currentExt, expectedExt, StringComparison.OrdinalIgnoreCase))
            {
                _logger.Debug($"[调试] 扩展名不匹配: {currentExt} != {expectedExt}");
                return false;
            }

            // 2. 检查分卷文件
            var currentPartMatch = System.Text.RegularExpressions.Regex.Match(currentNameNoExt, @"part(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var expectedPartMatch = System.Text.RegularExpressions.Regex.Match(expectedNameNoExt, @"part(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (currentPartMatch.Success && expectedPartMatch.Success)
            {
                var currentPart = int.Parse(currentPartMatch.Groups[1].Value);
                var expectedPart = int.Parse(expectedPartMatch.Groups[1].Value);

                // 如果分卷号不匹配，直接返回false
                if (currentPart != expectedPart)
                {
                    _logger.Debug($"[调试] 分卷号不匹配: part{currentPart} != part{expectedPart}");
                    return false;
                }

                // 提取分卷号之前的基本文件名
                var currentBaseName = currentNameNoExt.Substring(0, currentPartMatch.Index).Trim();
                var expectedBaseName = expectedNameNoExt.Substring(0, expectedPartMatch.Index).Trim();

                // 如果期望的文件名包含省略号
                if (expectedBaseName.Contains("…"))
                {
                    var expectedParts = expectedBaseName.Split(new[] { '…' }, StringSplitOptions.RemoveEmptyEntries);
                    if (expectedParts.Length == 2)
                    {
                        var prefix = expectedParts[0].Trim();
                        var suffix = expectedParts[1].Trim();

                        // 检查当前文件名是否包含前缀和后缀
                        if (currentBaseName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                            currentBaseName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.Debug("[调试] 省略号匹配成功");
                            return true;
                        }
                    }
                    else
                    {
                        // 如果省略号前后没有分割，则直接比较省略号前的部分
                        var expectedPrefix = expectedBaseName.Split('…')[0].Trim();
                        if (currentBaseName.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.Debug("[调试] 省略号前缀匹配成功");
                            return true;
                        }
                    }
                }
                else
                {
                    // 如果期望的文件名是完整的，则需要完全匹配
                    if (string.Equals(currentBaseName, expectedBaseName, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.Debug("[调试] 完整文件名匹配成功");
                        return true;
                    }
                }

                // 如果上述匹配都失败，尝试模糊匹配
                var normalizedCurrent = currentBaseName.Replace(" ", "").Replace("-", "").Replace("_", "").ToLower();
                var normalizedExpected = expectedBaseName.Replace(" ", "").Replace("-", "").Replace("_", "").Replace("…", "").ToLower();

                if (normalizedCurrent.Contains(normalizedExpected) || normalizedExpected.Contains(normalizedCurrent))
                {
                    _logger.Debug("[调试] 模糊匹配成功");
                    return true;
                }
            }

            _logger.Debug("[调试] 所有匹配方式均失败");
            return false;
        }

        private List<string> SplitIntoWords(string input)
        {
            // 将驼峰命名转换为空格分隔
            var words = System.Text.RegularExpressions.Regex.Replace(input, "([a-z])([A-Z])", "$1 $2");
            // 分割并清理单词
            return words.Split(new[] { ' ', '-', '_', '.' }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(w => w.ToLower())
                       .Where(w => w.Length > 1) // 忽略单个字符
                       .ToList();
        }

        private bool IsFileReady(string filePath)
        {
            try
            {
                using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return true;
                }
            }
            catch (IOException)
            {
                return false;
            }
        }

        private bool IsFileLocked(string filePath)
        {
            try
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                return false;
            }
            catch (IOException)
            {
                return true;
            }
        }

        private string GetSafeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return $"file_{DateTime.Now.Ticks}";
            }

            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = new string(fileName.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
            return safeName.Length > 100 ? safeName.Substring(0, 100) : safeName;
        }

        private IWebElement? FindFileElement(List<IWebElement> messageGroup, string fileName)
        {
            foreach (var message in messageGroup)
            {
                try
                {
                    var fileElements = message.FindElements(By.CssSelector(
                        ".document-container, .media-container, .media-photo-container"));

                    foreach (var element in fileElements)
                    {
                        try
                        {
                            var nameElement = element.FindElement(By.CssSelector(".document-name"));
                            if (nameElement.Text.Trim() == fileName)
                            {
                                return element;
                            }
                        }
                        catch (NoSuchElementException)
                        {
                            continue;
                        }
                    }
                }
                catch (StaleElementReferenceException)
                {
                    continue;
                }
            }
            return null;
        }

        private async Task<bool> WaitForDownload(string downloadPath, string fileName, 
            Action<double> progressCallback, CancellationToken cancellationToken)
        {
            var startTime = DateTime.Now;
            var timeout = TimeSpan.FromMinutes(30); // 30分钟超时
            var lastProgress = 0.0;

            while (DateTime.Now - startTime < timeout && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // 检查下载中的文件
                    var crdownloadFile = Directory.GetFiles(downloadPath, "*.crdownload")
                        .FirstOrDefault(f => Path.GetFileName(f).Contains(fileName));

                    if (crdownloadFile != null)
                    {
                        var fileInfo = new FileInfo(crdownloadFile);
                        if (fileInfo.Length > 0)
                        {
                            // 获取下载进度
                            var progress = CalculateDownloadProgress(crdownloadFile);
                            if (progress > lastProgress)
                            {
                                lastProgress = progress;
                                progressCallback(progress);
                            }
                        }
                    }
                    else
                    {
                        // 检查是否下载完成
                        var completedFile = Directory.GetFiles(downloadPath, fileName + "*")
                            .FirstOrDefault(f => !f.EndsWith(".crdownload"));

                        if (completedFile != null)
                        {
                            progressCallback(100);
                            return true;
                        }
                    }

                    await Task.Delay(1000, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "检查下载进度时出错");
                    return false;
                }
            }

            return false;
        }

        private double CalculateDownloadProgress(string crdownloadFile)
        {
            try
            {
                var fileInfo = new FileInfo(crdownloadFile);
                var downloadedSize = fileInfo.Length;
                
                // 从文件名中提取总大小信息
                var fileName = Path.GetFileName(crdownloadFile);
                var sizeMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"_(\d+)_bytes");
                if (sizeMatch.Success && long.TryParse(sizeMatch.Groups[1].Value, out long totalSize))
                {
                    if (totalSize > 0)
                    {
                        return (downloadedSize * 100.0) / totalSize;
                    }
                }
                
                // 如果无法获取总大小，返回一个估计的进度
                return Math.Min((downloadedSize * 100.0) / (1024 * 1024), 99); // 限制最大99%
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "计算下载进度时出错");
                return 0;
            }
        }
    }
}

#nullable restore 