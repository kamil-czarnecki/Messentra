using AutoFixture;
using Messentra.Features.Settings.Connections;
using Messentra.Features.Settings.Connections.GetConnections;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Settings.Connections;

public sealed class ConnectionReducersShould
{
    private readonly Fixture _fixture = new();
    
    [Fact]
    public void ReduceFetchConnectionsAction()
    {
        // Arrange
        var initialState = new ConnectionState(false, false, []);
        var action = new FetchConnectionsAction();

        // Act
        var newState = ConnectionReducers.Reduce(initialState, action);

        // Assert
        newState.IsLoading.ShouldBeTrue();
        newState.IsLoaded.ShouldBeFalse();
        newState.Connections.ShouldBeEmpty();
    }
    
    [Fact]
    public void ReduceFetchConnectionsSuccess()
    {
        // Arrange
        var initialState = new ConnectionState(true, false, []);
        var connection = _fixture.Create<ConnectionDto>();
        var action = new FetchConnectionsSuccessAction([connection]);

        // Act
        var newState = ConnectionReducers.Reduce(initialState, action);

        // Assert
        newState.IsLoading.ShouldBeFalse();
        newState.IsLoaded.ShouldBeTrue();
        newState.Connections.Count().ShouldBe(1);
        newState.Connections.First().ShouldBe(connection);
    }
    
    [Fact]
    public void ReduceFetchConnectionsFailureAction()
    {
        // Arrange
        var initialState = new ConnectionState(true, false, []);
        var action = new FetchConnectionsFailureAction();

        // Act
        var newState = ConnectionReducers.Reduce(initialState, action);

        // Assert
        newState.IsLoading.ShouldBeFalse();
        newState.IsLoaded.ShouldBeFalse();
        newState.Connections.ShouldBeEmpty();
    }
    
    [Fact]
    public void ReduceCreateConnectionAction()
    {
        // Arrange
        var initialState = new ConnectionState(false, false, []);
        var connection = _fixture.Create<ConnectionDto>();
        var action = new CreateConnectionAction(connection);

        // Act
        var newState = ConnectionReducers.Reduce(initialState, action);

        // Assert
        newState.IsLoading.ShouldBeTrue();
        newState.IsLoaded.ShouldBeFalse();
        newState.Connections.ShouldBeEmpty();
    }
    
    [Fact]
    public void ReduceCreateConnectionSuccessAction()
    {
        // Arrange
        var initialState = new ConnectionState(true, false, []);
        var action = new CreateConnectionSuccessAction();

        // Act
        var newState = ConnectionReducers.Reduce(initialState, action);

        // Assert
        newState.IsLoading.ShouldBeFalse();
        newState.IsLoaded.ShouldBeFalse();
        newState.Connections.ShouldBe(initialState.Connections);
    }
    
    [Fact]
    public void ReduceCreateConnectionFailureAction()
    {
        // Arrange
        var initialState = new ConnectionState(true, false, []);
        var action = new CreateConnectionFailureAction();

        // Act
        var newState = ConnectionReducers.Reduce(initialState, action);

        // Assert
        newState.IsLoading.ShouldBeFalse();
        newState.IsLoaded.ShouldBeFalse();
        newState.Connections.ShouldBeEmpty();
    }
    
    [Fact]
    public void ReduceUpdateConnectionAction()
    {
        // Arrange
        var initialState = new ConnectionState(false, false, []);
        var connection = _fixture.Create<ConnectionDto>();
        var action = new UpdateConnectionAction(connection);

        // Act
        var newState = ConnectionReducers.Reduce(initialState, action);

        // Assert
        newState.IsLoading.ShouldBeTrue();
        newState.IsLoaded.ShouldBeFalse();
        newState.Connections.ShouldBeEmpty();
    }
    
    [Fact]
    public void ReduceUpdateConnectionSuccessAction()
    {
        // Arrange
        var initialState = new ConnectionState(true, false, []);
        var action = new UpdateConnectionSuccessAction();

        // Act
        var newState = ConnectionReducers.Reduce(initialState, action);

        // Assert
        newState.IsLoading.ShouldBeFalse();
        newState.IsLoaded.ShouldBeFalse();
        newState.Connections.ShouldBe(initialState.Connections);
    }
    
    [Fact]
    public void ReduceUpdateConnectionFailureAction()
    {
        // Arrange
        var initialState = new ConnectionState(true, false, []);
        var action = new UpdateConnectionFailureAction();

        // Act
        var newState = ConnectionReducers.Reduce(initialState, action);

        // Assert
        newState.IsLoading.ShouldBeFalse();
        newState.IsLoaded.ShouldBeFalse();
        newState.Connections.ShouldBeEmpty();
    }
    
    [Fact]
    public void ReduceDeleteConnectionAction()
    {
        // Arrange
        var initialState = new ConnectionState(false, false, []);
        var action = new DeleteConnectionAction(1L);

        // Act
        var newState = ConnectionReducers.Reduce(initialState, action);

        // Assert
        newState.IsLoading.ShouldBeTrue();
        newState.IsLoaded.ShouldBeFalse();
        newState.Connections.ShouldBeEmpty();
    }
    
    [Fact]
    public void ReduceDeleteConnectionSuccessAction()
    {
        // Arrange
        var initialState = new ConnectionState(true, false, []);
        var action = new DeleteConnectionSuccessAction();

        // Act
        var newState = ConnectionReducers.Reduce(initialState, action);

        // Assert
        newState.IsLoading.ShouldBeFalse();
        newState.IsLoaded.ShouldBeFalse();
        newState.Connections.ShouldBe(initialState.Connections);
    }
    
    [Fact]
    public void ReduceDeleteConnectionFailureAction()
    {
        // Arrange
        var initialState = new ConnectionState(true, false, []);
        var action = new DeleteConnectionFailureAction();

        // Act
        var newState = ConnectionReducers.Reduce(initialState, action);

        // Assert
        newState.IsLoading.ShouldBeFalse();
        newState.IsLoaded.ShouldBeFalse();
        newState.Connections.ShouldBeEmpty();
    }
}