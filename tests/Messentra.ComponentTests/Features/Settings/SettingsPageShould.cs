using Bunit;
using Messentra.Features.Settings;
using Messentra.Features.Settings.Cache.Components;
using Messentra.Features.Settings.Connections.Components;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Settings;

public sealed class SettingsPageShould : ComponentTestBase
{
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
}