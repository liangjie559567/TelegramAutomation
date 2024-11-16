using Xunit;
using Moq;
using TelegramAutomation.ViewModels;

namespace TelegramAutomation.Tests
{
    public class MainViewModelTests
    {
        private readonly MainViewModel _viewModel;

        public MainViewModelTests()
        {
            _viewModel = new MainViewModel();
        }

        [Fact]
        public void CanStartAutomation_ShouldReturnFalseWhenNotLoggedIn()
        {
            // Arrange
            _viewModel.ChannelUrl = "https://t.me/test";
            _viewModel.SavePath = "C:\\test";
            
            // Act & Assert
            Assert.False(_viewModel.StartCommand.CanExecute(null));
        }

        [Theory]
        [InlineData("", "C:\\test")]
        [InlineData("https://t.me/test", "")]
        [InlineData("", "")]
        public void CanStartAutomation_ShouldReturnFalseWhenInputsInvalid(string url, string path)
        {
            // Arrange
            _viewModel.ChannelUrl = url;
            _viewModel.SavePath = path;
            
            // Act & Assert
            Assert.False(_viewModel.StartCommand.CanExecute(null));
        }

        [Fact]
        public void PropertyChangedShouldRaiseCorrectly()
        {
            // Arrange
            var raised = false;
            _viewModel.PropertyChanged += (s, e) => raised = true;
            
            // Act
            _viewModel.ChannelUrl = "https://t.me/newchannel";
            
            // Assert
            Assert.True(raised);
        }
    }
} 