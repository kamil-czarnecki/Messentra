using AutoFixture;
using Bunit;
using Messentra.Features.Settings.Connections;
using Messentra.Features.Settings.Connections.Components;
using Messentra.Features.Settings.Connections.GetConnections;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Settings.Connections;

public sealed class ConnectionsComponentShould : ComponentTestBase
{
    [Fact]
    public void RenderNoConnectionsAndFetch()
    {
        // Assert and Act
        var cut = Render<ConnectionsComponent>();

        // Assert
        cut.Markup.ShouldContain("No connections found");
        MockDispatcher.Verify(x => x.Dispatch(It.IsAny<FetchConnectionsAction>()), Times.Once);
    }
    
    [Fact]
    public void RenderLoadingState()
    {
        // Arrange
        var state = GetState<ConnectionState>();
        state.SetState(new ConnectionState(true, false, []));

        // Act
        var cut = Render<ConnectionsComponent>();

        // Assert
        cut.Markup.ShouldContain("Loading connections...");
    }
    
    [Fact]
    public void RenderConnectionsList()
    {
        // Arrange
        var state = GetState<ConnectionState>();
        var connections = Fixture.CreateMany<ConnectionDto>(2).ToArray();
        state.SetState(new ConnectionState(false, true, connections));

        // Act
        var cut = Render<ConnectionsComponent>();

        // Assert
        cut.Markup.ShouldContain(connections[0].Name);
        cut.Markup.ShouldContain(connections[1].Name);
        MockDispatcher.Verify(x => x.Dispatch(It.IsAny<FetchConnectionsAction>()), Times.Never);
    }

    [Fact]
    public void RenderConnectionDetails()
    {
        // Arrange
        var state = GetState<ConnectionState>();
        var connection = Fixture.Create<ConnectionDto>();
        state.SetState(new ConnectionState(false, true, [connection]));
        
        // Act
        var cut = Render<ConnectionsComponent>();
        
        // Assert
        cut.Markup.ShouldContain(connection.Name);
        cut.Markup.ShouldContain(connection.ConnectionConfig.ConnectionType.ToString());
    }
    
    [Fact]
    public void OpenAddConnectionDialog()
    {
        // Arrange
        var cut = Render<ConnectionsComponent>();

        // Act
        cut.Find("button").Click();

        // Assert
        MudDialog.FindComponent<ConnectionDialog>().ShouldNotBeNull();
    }
    
    [Fact]
    public void RenderEditConnectionDialog()
    {
        // Arrange
        var state = GetState<ConnectionState>();
        var connection = Fixture.Create<ConnectionDto>();
        state.SetState(new ConnectionState(false, true, [connection]));
        
        var cut = Render<ConnectionsComponent>();

        // Act
        cut.Find("button[title=\"Edit\"]").Click();

        // Assert
        MudDialog.FindComponent<ConnectionDialog>().ShouldNotBeNull();
    }
    
    [Fact]
    public void RenderDeleteConnectionDialog()
    {
        // Arrange
        var state = GetState<ConnectionState>();
        var connection = Fixture.Create<ConnectionDto>();
        state.SetState(new ConnectionState(false, true, [connection]));
        
        var cut = Render<ConnectionsComponent>();

        // Act
        cut.Find("button[title=\"Delete\"]").Click();

        // Assert
        MudDialog.Markup.ShouldContain("Delete Connection");
    }
}