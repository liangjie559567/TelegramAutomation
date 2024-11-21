using Xunit;
using TelegramAutomation.Configuration;

namespace TelegramAutomation.Tests;

public class AppSettingsTests
{
    [Fact]
    public void ChromeDriverConfig_DefaultValues_ShouldBeCorrect()
    {
        var config = new ChromeDriverConfig();
        
        Assert.False(config.Headless);
        Assert.Empty(config.SearchPaths);
        Assert.NotNull(config.Options);
        Assert.Contains("disable-gpu", config.Options.Keys);
    }
} 