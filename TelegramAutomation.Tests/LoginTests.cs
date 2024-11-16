using Xunit;
using Moq;
using NLog;
using OpenQA.Selenium;
using System.Threading.Tasks;

namespace TelegramAutomation.Tests
{
    public class LoginTests
    {
        private readonly Mock<ILogger> _loggerMock;
        private readonly Mock<IWebDriver> _driverMock;
        private readonly Mock<IWebElement> _phoneInputMock;
        private readonly Mock<IWebElement> _codeInputMock;
        private readonly Mock<IWebElement> _submitButtonMock;

        public LoginTests()
        {
            _loggerMock = new Mock<ILogger>();
            _driverMock = new Mock<IWebDriver>();
            _phoneInputMock = new Mock<IWebElement>();
            _codeInputMock = new Mock<IWebElement>();
            _submitButtonMock = new Mock<IWebElement>();
        }

        [Fact]
        public async Task RequestVerificationCode_ShouldSendCode()
        {
            // Arrange
            var navigation = new Mock<INavigation>();
            _driverMock.Setup(d => d.Navigate()).Returns(navigation.Object);
            
            _driverMock.Setup(d => d.FindElement(By.CssSelector("input[type='tel']")))
                      .Returns(_phoneInputMock.Object);
            
            _driverMock.Setup(d => d.FindElement(By.CssSelector("button[type='submit']")))
                      .Returns(_submitButtonMock.Object);
            
            var controller = new AutomationController(_driverMock.Object, _loggerMock.Object);
            var phoneNumber = "+8613800138000";

            // Act
            await controller.RequestVerificationCode(phoneNumber);

            // Assert
            _phoneInputMock.Verify(e => e.Clear(), Times.Once);
            _phoneInputMock.Verify(e => e.SendKeys(phoneNumber), Times.Once);
            _submitButtonMock.Verify(e => e.Click(), Times.Once);
        }

        [Fact]
        public async Task Login_ShouldSucceedWithValidCode()
        {
            // Arrange
            _driverMock.Setup(d => d.FindElement(By.CssSelector("input[type='text']")))
                      .Returns(_codeInputMock.Object);
            
            _driverMock.Setup(d => d.FindElements(By.CssSelector(".messages-container")))
                      .Returns(new[] { Mock.Of<IWebElement>() });
            
            var controller = new AutomationController(_driverMock.Object, _loggerMock.Object);
            var verificationCode = "12345";

            // Act
            await controller.Login("+8613800138000", verificationCode);

            // Assert
            _codeInputMock.Verify(e => e.Clear(), Times.Once);
            _codeInputMock.Verify(e => e.SendKeys(verificationCode), Times.Once);
            _loggerMock.Verify(l => l.Info("登录成功"), Times.Once);
        }

        [Fact]
        public async Task Login_ShouldFailWithInvalidCode()
        {
            // Arrange
            _driverMock.Setup(d => d.FindElement(By.CssSelector("input[type='text']")))
                      .Returns(_codeInputMock.Object);
            
            _driverMock.Setup(d => d.FindElements(By.CssSelector(".messages-container")))
                      .Returns(Array.Empty<IWebElement>());
            
            var controller = new AutomationController(_driverMock.Object, _loggerMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => 
                controller.Login("+8613800138000", "invalid-code"));
        }

        [Fact]
        public async Task Login_ShouldHandleTimeout()
        {
            // Arrange
            _driverMock.Setup(d => d.FindElement(By.CssSelector("input[type='text']")))
                      .Returns(_codeInputMock.Object);
            
            _driverMock.Setup(d => d.FindElements(By.CssSelector(".messages-container")))
                      .Throws<WebDriverTimeoutException>();
            
            var controller = new AutomationController(_driverMock.Object, _loggerMock.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => 
                controller.Login("+8613800138000", "12345"));
            
            Assert.Contains("登录超时", exception.Message);
        }

        [Fact]
        public async Task Login_ShouldSaveSession()
        {
            // Arrange
            _driverMock.Setup(d => d.FindElement(By.CssSelector("input[type='text']")))
                      .Returns(_codeInputMock.Object);
            
            _driverMock.Setup(d => d.FindElements(By.CssSelector(".messages-container")))
                      .Returns(new[] { Mock.Of<IWebElement>() });
            
            var jsExecutor = _driverMock.As<IJavaScriptExecutor>();
            jsExecutor.Setup(j => j.ExecuteScript(It.IsAny<string>()))
                     .Returns("test-session-token");
            
            var controller = new AutomationController(_driverMock.Object, _loggerMock.Object);

            // Act
            await controller.Login("+8613800138000", "12345");

            // Assert
            var sessionFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TelegramAutomation",
                "session.json"
            );
            Assert.True(File.Exists(sessionFile));
            Assert.Equal("test-session-token", await File.ReadAllTextAsync(sessionFile));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("+")]
        [InlineData("12345")]
        public async Task RequestVerificationCode_ShouldValidatePhoneNumber(string phoneNumber)
        {
            // Arrange
            var controller = new AutomationController(_driverMock.Object, _loggerMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                controller.RequestVerificationCode(phoneNumber));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("123")]
        [InlineData("abcdef")]
        public async Task Login_ShouldValidateVerificationCode(string code)
        {
            // Arrange
            var controller = new AutomationController(_driverMock.Object, _loggerMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                controller.Login("+8613800138000", code));
        }
    }
} 