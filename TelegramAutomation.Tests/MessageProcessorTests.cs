using Xunit;
using Moq;
using NLog;
using OpenQA.Selenium;

namespace TelegramAutomation.Tests
{
    public class MessageProcessorTests
    {
        private readonly Mock<ILogger> _loggerMock;
        private readonly Mock<IWebElement> _messageMock;

        public MessageProcessorTests()
        {
            _loggerMock = new Mock<ILogger>();
            _messageMock = new Mock<IWebElement>();
        }

        [Fact]
        public async Task ExtractMessageText_ShouldReturnText()
        {
            // Arrange
            var textElementMock = new Mock<IWebElement>();
            textElementMock.Setup(e => e.Text).Returns("Test message");
            
            _messageMock.Setup(m => m.FindElement(By.CssSelector(".text-content")))
                       .Returns(textElementMock.Object);
            
            var processor = new MessageProcessor(_loggerMock.Object);
            
            // Act
            var result = await processor.ExtractMessageText(_messageMock.Object);
            
            // Assert
            Assert.Equal("Test message", result);
        }

        [Fact]
        public async Task ExtractLinks_ShouldReturnAllLinks()
        {
            // Arrange
            var linkElements = new List<IWebElement>
            {
                CreateLinkElement("https://example.com/1"),
                CreateLinkElement("https://example.com/2")
            };
            
            _messageMock.Setup(m => m.FindElements(By.TagName("a")))
                       .Returns(linkElements);
            
            var processor = new MessageProcessor(_loggerMock.Object);
            
            // Act
            var links = (await processor.ExtractLinks(_messageMock.Object)).ToList();
            
            // Assert
            Assert.Equal(2, links.Count);
            Assert.Contains("https://example.com/1", links);
            Assert.Contains("https://example.com/2", links);
        }

        [Fact]
        public async Task ExtractMessageText_ShouldHandleEmptyMessage()
        {
            // Arrange
            var messageMock = new Mock<IWebElement>();
            messageMock.Setup(m => m.FindElement(By.CssSelector(".text-content")))
                      .Throws<NoSuchElementException>();
            
            var processor = new MessageProcessor(_loggerMock.Object);
            
            // Act
            var result = await processor.ExtractMessageText(messageMock.Object);
            
            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task ExtractLinks_ShouldHandleNoLinks()
        {
            // Arrange
            var messageMock = new Mock<IWebElement>();
            messageMock.Setup(m => m.FindElements(By.TagName("a")))
                      .Returns(new List<IWebElement>());
            
            var processor = new MessageProcessor(_loggerMock.Object);
            
            // Act
            var links = await processor.ExtractLinks(messageMock.Object);
            
            // Assert
            Assert.Empty(links);
        }

        [Fact]
        public async Task ExtractLinks_ShouldFilterInvalidUrls()
        {
            // Arrange
            var linkElements = new List<IWebElement>
            {
                CreateLinkElement("https://example.com"),
                CreateLinkElement("invalid-url"),
                CreateLinkElement("http://test.com")
            };
            
            _messageMock.Setup(m => m.FindElements(By.TagName("a")))
                       .Returns(linkElements);
            
            var processor = new MessageProcessor(_loggerMock.Object);
            
            // Act
            var links = (await processor.ExtractLinks(_messageMock.Object)).ToList();
            
            // Assert
            Assert.Equal(2, links.Count);
            Assert.Contains("https://example.com", links);
            Assert.Contains("http://test.com", links);
        }

        private Mock<IWebElement> CreateLinkElement(string href)
        {
            var linkMock = new Mock<IWebElement>();
            linkMock.Setup(l => l.GetAttribute("href")).Returns(href);
            return linkMock;
        }
    }
} 