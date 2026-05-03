using Bunit;
using Messentra.Features.Settings;
using Messentra.Features.Settings.Cache.Components;
using Messentra.Features.Settings.Connections.Components;
using Messentra.Features.Settings.Mcp.Components;
using Messentra.Features.Settings.UserSettings.GetUserSettings;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Settings;

public sealed class SettingsPageShould : ComponentTestBase
{
    public SettingsPageShould()
    {
        MockMediator
            .Setup(m => m.Send(It.IsAny<GetUserSettingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettingsDto(IsDarkMode: false, IsMcpEnabled: false));
    }

    [Fact]
    public void RenderConnectionsComponent()
    {
        // Arrange and Act
        var cut = Render<SettingsPage>();

        // Assert
        cut.FindComponent<ConnectionsComponent>().ShouldNotBeNull();
    }
    
    [Fact]
    public void RenderCacheComponent()
    {
        // Arrange and Act
        var cut = Render<SettingsPage>();

        // Assert
        cut.FindComponent<CacheComponent>().ShouldNotBeNull();
    }

    [Fact]
    public void RenderMcpComponent()
    {
        // Arrange and Act
        var cut = Render<SettingsPage>();

        // Assert
        cut.FindComponent<McpComponent>().ShouldNotBeNull();
    }
}