using Bunit;
using Messentra.Domain;
using Messentra.Features.Explorer;
using Messentra.Features.Explorer.Resources.Components;
using Messentra.Features.Explorer.Resources.Components.Details;
using Messentra.Features.Settings.Connections;
using Messentra.Features.Settings.Connections.GetConnections;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Explorer;

public sealed class ExplorerPageShould : ComponentTestBase
{
    [Fact]
    public void RenderNamespaceTreeComponent()
    {
        // Arrange & Act
        var cut = Render<ExplorerPage>();

        // Assert
        cut.FindComponent<NamespaceTree>().ShouldNotBeNull();
    }

    [Fact]
    public void RenderResourceDetailsComponent()
    {
        // Arrange & Act
        var cut = Render<ExplorerPage>();

        // Assert
        cut.FindComponent<ResourceDetails>().ShouldNotBeNull();
    }

    [Fact]
    public void ExcludeCorruptedConnectionsFromNamespaceTree()
    {
        // Arrange
        var state = GetState<ConnectionState>();
        var valid = new ConnectionDto(1, "Valid", new ConnectionConfigDto(ConnectionType.EntraId, null, "ns.servicebus.windows.net", "tenant", "client"));
        var corrupted = new ConnectionDto(2, "Corrupted", new ConnectionConfigDto(ConnectionType.Corrupted, null, null, null, null));
        state.SetState(new ConnectionState(false, true, [valid, corrupted]));

        // Act
        var cut = Render<ExplorerPage>();
        var namespaceTree = cut.FindComponent<NamespaceTree>();

        // Assert
        namespaceTree.Instance.Connections.ShouldContain(c => c.Name == "Valid");
        namespaceTree.Instance.Connections.ShouldNotContain(c => c.Name == "Corrupted");
    }
}

