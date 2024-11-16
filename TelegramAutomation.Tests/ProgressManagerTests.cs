using Xunit;
using System.Text.Json;

namespace TelegramAutomation.Tests
{
    public class ProgressManagerTests
    {
        private readonly string _testPath;

        public ProgressManagerTests()
        {
            _testPath = Path.Combine(Path.GetTempPath(), "TelegramAutomationTests");
            Directory.CreateDirectory(_testPath);
        }

        [Fact]
        public async Task SaveAndLoadProgress_ShouldWorkCorrectly()
        {
            // Arrange
            var manager = new ProgressManager(_testPath);
            var processedIds = new HashSet<string> { "msg1", "msg2", "msg3" };
            
            // Act
            await manager.SaveProgress(processedIds);
            var loadedIds = await manager.LoadProgress();
            
            // Assert
            Assert.Equal(processedIds.Count, loadedIds.Count);
            Assert.All(processedIds, id => Assert.Contains(id, loadedIds));
        }

        [Fact]
        public async Task LoadProgress_ShouldReturnEmptySetWhenFileNotExists()
        {
            // Arrange
            var manager = new ProgressManager(Path.Combine(_testPath, "nonexistent"));
            
            // Act
            var result = await manager.LoadProgress();
            
            // Assert
            Assert.Empty(result);
        }
    }
} 