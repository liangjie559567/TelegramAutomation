using Xunit;
using TelegramAutomation.Commands;

namespace TelegramAutomation.Tests.Commands
{
    public class RelayCommandTests
    {
        [Fact]
        public void Execute_ShouldInvokeAction()
        {
            // Arrange
            var executed = false;
            var command = new RelayCommand(() => executed = true);
            
            // Act
            command.Execute(null);
            
            // Assert
            Assert.True(executed);
        }

        [Fact]
        public void CanExecute_ShouldReturnTrueByDefault()
        {
            // Arrange
            var command = new RelayCommand(() => { });
            
            // Act & Assert
            Assert.True(command.CanExecute(null));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CanExecute_ShouldRespectPredicate(bool canExecute)
        {
            // Arrange
            var command = new RelayCommand(() => { }, () => canExecute);
            
            // Act & Assert
            Assert.Equal(canExecute, command.CanExecute(null));
        }

        [Fact]
        public void RaiseCanExecuteChanged_ShouldRaiseEvent()
        {
            // Arrange
            var command = new RelayCommand(() => { });
            var raised = false;
            command.CanExecuteChanged += (s, e) => raised = true;
            
            // Act
            command.RaiseCanExecuteChanged();
            
            // Assert
            Assert.True(raised);
        }
    }
} 