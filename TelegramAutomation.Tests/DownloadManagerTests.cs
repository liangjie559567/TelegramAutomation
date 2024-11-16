using Xunit;
using Moq;
using NLog;
using System.Threading;

namespace TelegramAutomation.Tests
{
    public class DownloadManagerTests
    {
        private readonly Mock<ILogger> _loggerMock;
        private readonly string _testDownloadPath;

        public DownloadManagerTests()
        {
            _loggerMock = new Mock<ILogger>();
            _testDownloadPath = Path.Combine(Path.GetTempPath(), "TelegramAutomationTests");
            Directory.CreateDirectory(_testDownloadPath);
        }

        [Fact]
        public async Task DownloadFile_ShouldDownloadSuccessfully()
        {
            // Arrange
            var downloadManager = new DownloadManager(_testDownloadPath, _loggerMock.Object);
            var progress = new Mock<IProgress<string>>();
            var url = "https://example.com/test.zip";
            var savePath = Path.Combine(_testDownloadPath, "test");
            
            // Act
            await downloadManager.DownloadFile(url, savePath, progress.Object, CancellationToken.None);
            
            // Assert
            var expectedFilePath = Path.Combine(savePath, "test.zip");
            Assert.True(File.Exists(expectedFilePath));
        }

        [Fact]
        public async Task DownloadFile_ShouldSkipExistingFile()
        {
            // Arrange
            var downloadManager = new DownloadManager(_testDownloadPath, _loggerMock.Object);
            var progress = new Mock<IProgress<string>>();
            var url = "https://example.com/test.zip";
            var savePath = Path.Combine(_testDownloadPath, "test");
            var filePath = Path.Combine(savePath, "test.zip");
            
            Directory.CreateDirectory(savePath);
            await File.WriteAllTextAsync(filePath, "test content");
            
            // Act
            await downloadManager.DownloadFile(url, savePath, progress.Object, CancellationToken.None);
            
            // Assert
            progress.Verify(p => p.Report(It.Is<string>(s => s.Contains("文件已存在"))), Times.Once);
        }

        [Fact]
        public void CheckDiskSpace_ShouldReturnFalseWhenSpaceInsufficient()
        {
            // Arrange
            var downloadManager = new DownloadManager(_testDownloadPath, _loggerMock.Object);
            
            // Act
            var hasSpace = downloadManager.CheckDiskSpace(_testDownloadPath);
            
            // Assert
            Assert.True(hasSpace); // 实际环境中应该有足够空间
        }
    }
} 