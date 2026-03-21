using Bunit;
using Messentra.Features.Settings.Cache;
using Messentra.Features.Settings.Cache.Components;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Settings.Cache.Components;

public sealed class CacheComponentShould : ComponentTestBase
{
    [Fact]
    public void RenderCacheSectionAndButton()
    {
        // Arrange

        // Act
        var cut = Render<CacheComponent>();

        // Assert
        cut.Markup.ShouldContain("Token Cache");
        cut.Markup.ShouldContain("Clear");
        cut.Markup.ShouldContain("Clear the token cache and restart the application.");
    }

    [Fact]
    public async Task ClickClearButtonSendsClearCacheCommand()
    {
        // Arrange
        var cut = Render<CacheComponent>();

        // Act
        await cut.Find("button").ClickAsync();

        // Assert
        MockMediator.Verify(x => x.Send(It.IsAny<ClearCacheCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}


