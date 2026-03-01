using Bunit;
using Messentra.Domain;
using Messentra.Features.Explorer.Resources;
using Messentra.Features.Explorer.Resources.Components;
using Messentra.Features.Settings.Connections.GetConnections;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Explorer.Resources.Components;

public sealed class NamespaceTreeShould : ComponentTestBase
{
    private static ConnectionDto BuildEntraIdConnection(string name = "Test Namespace") =>
        new(
            Id: 1,
            Name: name,
            ConnectionConfig: new ConnectionConfigDto(
                ConnectionType.EntraId,
                null,
                "test.servicebus.windows.net",
                "tenant-id",
                "client-id"));

    [Fact]
    public void ShowEmptyStateWhenNoResourcesConnected()
    {
        // Arrange & Act
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, [])
            .Add(x => x.Connections, []));

        // Assert
        cut.Markup.ShouldContain("No connection selected");
    }

    [Fact]
    public void ShowSavedConnectionsInMenu()
    {
        // Arrange
        var connection = BuildEntraIdConnection();
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, [])
            .Add(x => x.Connections, [connection]));

        // Act - open menu to render popover content
        cut.Find(".mud-menu button").Click();

        // Assert
        MudPopover.Markup.ShouldContain(connection.Name);
    }

    [Fact]
    public void DispatchFetchResourcesActionWhenConnectionSelected()
    {
        // Arrange
        var connection = BuildEntraIdConnection();
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, [])
            .Add(x => x.Connections, [connection]));

        // Act - open menu then click the connection item
        cut.Find(".mud-menu button").Click();
        MudPopover.Find(".mud-menu-item:last-child").Click();

        // Assert
        MockDispatcher.Verify(x => x.Dispatch(It.IsAny<FetchResourcesAction>()), Times.Once);
    }

    [Fact]
    public void NavigateToOptionsWhenAddConnectionClicked()
    {
        // Arrange
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<NamespaceTree>(p => p
            .Add(x => x.Resources, [])
            .Add(x => x.Connections, []));

        // Act - open menu then click "Add connection"
        cut.Find(".mud-menu button").Click();
        MudPopover.Find(".mud-menu-item").Click();

        // Assert
        navigationManager.Uri.ShouldContain("/options");
    }
}
