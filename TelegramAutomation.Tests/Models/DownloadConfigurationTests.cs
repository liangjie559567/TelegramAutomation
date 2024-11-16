using Xunit;
using TelegramAutomation.Models;

namespace TelegramAutomation.Tests.Models
{
    public class DownloadConfigurationTests
    {
        [Fact]
        public void DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var config = new DownloadConfiguration();
            
            // Assert
            Assert.Equal(3, config.MaxRetries);
            Assert.Equal(1000, config.ScrollWaitTime);
            Assert.Equal(30000, config.LoginWaitTime);
            Assert.Equal(3, config.MaxConcurrentDownloads);
            Assert.True(config.SaveMessageText);
            Assert.True(config.SaveLinks);
            Assert.Contains(".zip", config.SupportedFileExtensions);
            Assert.Contains(".rar", config.SupportedFileExtensions);
            Assert.Contains(".7z", config.SupportedFileExtensions);
        }

        [Fact]
        public void Properties_ShouldBeSettable()
        {
            // Arrange
            var config = new DownloadConfiguration();
            
            // Act
            config.MaxRetries = 5;
            config.ScrollWaitTime = 2000;
            config.SaveMessageText = false;
            
            // Assert
            Assert.Equal(5, config.MaxRetries);
            Assert.Equal(2000, config.ScrollWaitTime);
            Assert.False(config.SaveMessageText);
        }
    }
} 