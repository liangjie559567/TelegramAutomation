using Xunit;
using TelegramAutomation.ViewModels;
using System.ComponentModel;

namespace TelegramAutomation.Tests.ViewModels
{
    public class ViewModelBaseTests
    {
        private class TestViewModel : ViewModelBase
        {
            private string _testProperty = string.Empty;
            
            public string TestProperty
            {
                get => _testProperty;
                set => SetProperty(ref _testProperty, value);
            }
        }

        [Fact]
        public void PropertyChanged_ShouldRaiseEvent()
        {
            // Arrange
            var viewModel = new TestViewModel();
            var propertyChanged = false;
            viewModel.PropertyChanged += (s, e) => propertyChanged = true;
            
            // Act
            viewModel.TestProperty = "test";
            
            // Assert
            Assert.True(propertyChanged);
        }

        [Fact]
        public void SetProperty_ShouldNotRaiseEvent_WhenValueUnchanged()
        {
            // Arrange
            var viewModel = new TestViewModel();
            viewModel.TestProperty = "test";
            var propertyChanged = false;
            viewModel.PropertyChanged += (s, e) => propertyChanged = true;
            
            // Act
            viewModel.TestProperty = "test";
            
            // Assert
            Assert.False(propertyChanged);
        }

        [Fact]
        public void PropertyChanged_ShouldProvideCorrectPropertyName()
        {
            // Arrange
            var viewModel = new TestViewModel();
            string? raisedPropertyName = null;
            viewModel.PropertyChanged += (s, e) => raisedPropertyName = e.PropertyName;
            
            // Act
            viewModel.TestProperty = "test";
            
            // Assert
            Assert.Equal(nameof(TestViewModel.TestProperty), raisedPropertyName);
        }
    }
} 