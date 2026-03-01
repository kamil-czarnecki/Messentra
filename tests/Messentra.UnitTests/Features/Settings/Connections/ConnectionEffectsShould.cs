using AutoFixture;
using Fluxor;
using Mediator;
using Messentra.Features.Settings.Connections;
using Messentra.Features.Settings.Connections.CreateConnection;
using Messentra.Features.Settings.Connections.DeleteConnection;
using Messentra.Features.Settings.Connections.GetConnections;
using Messentra.Features.Settings.Connections.UpdateConnection;
using Moq;
using Xunit;

namespace Messentra.UnitTests.Features.Settings.Connections;

public sealed class ConnectionEffectsShould
{
    private readonly Fixture _fixture = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IDispatcher> _dispatcher = new();
    private readonly ConnectionEffects _sut;

    public ConnectionEffectsShould()
    {
        _sut = new ConnectionEffects(_mediator.Object);
    }
    
    [Fact]
    public async Task HandleFetchConnectionsActionWhenSuccess()
    {
        // Arrange
        var connections = _fixture.CreateMany<ConnectionDto>(2).ToList();
        
        _mediator
            .Setup(m => m.Send(It.IsAny<GetConnectionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(connections);
        
        // Act
        await _sut.HandleFetchConnectionsAction(_dispatcher.Object);
        
        // Assert
        _dispatcher.Verify(
            d => d.Dispatch(It.Is<FetchConnectionsSuccessAction>(a => a.Connections.SequenceEqual(connections))),
            Times.Once);
    }
    
    [Fact]
    public async Task HandleFetchConnectionsActionWhenFailure()
    {
        // Arrange
        _mediator
            .Setup(m => m.Send(It.IsAny<GetConnectionsQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());
        
        // Act
        await _sut.HandleFetchConnectionsAction(_dispatcher.Object);
        
        // Assert
        _dispatcher.Verify(
            d => d.Dispatch(It.IsAny<FetchConnectionsFailureAction>()),
            Times.Once);
    }
    
    [Fact]
    public async Task HandleCreateConnectionActionWhenSuccess()
    {
        // Arrange
        var connection = _fixture.Create<ConnectionDto>();
        var action = new CreateConnectionAction(connection);
        
        // Act
        await _sut.HandleCreateConnectionAction(action, _dispatcher.Object);
        
        // Assert
        _mediator.Verify(
            m => m.Send(It.Is<CreateConnectionCommand>(c =>
                c.Name == connection.Name &&
                c.ConnectionConfig.ConnectionType == connection.ConnectionConfig.ConnectionType &&
                c.ConnectionConfig.ConnectionString == connection.ConnectionConfig.ConnectionString &&
                c.ConnectionConfig.Namespace == connection.ConnectionConfig.Namespace &&
                c.ConnectionConfig.TenantId == connection.ConnectionConfig.TenantId &&
                c.ConnectionConfig.ClientId == connection.ConnectionConfig.ClientId), 
                It.IsAny<CancellationToken>()), 
            Times.Once);
        
        _dispatcher.Verify(d => d.Dispatch(It.IsAny<CreateConnectionSuccessAction>()), Times.Once);
        _dispatcher.Verify(d => d.Dispatch(It.IsAny<FetchConnectionsAction>()), Times.Once);
    }
    
    [Fact]
    public async Task HandleCreateConnectionActionWhenFailure()
    {
        // Arrange
        var connection = _fixture.Create<ConnectionDto>();
        var action = new CreateConnectionAction(connection);
        
        _mediator
            .Setup(m => m.Send(It.IsAny<CreateConnectionCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());
        
        // Act
        await _sut.HandleCreateConnectionAction(action, _dispatcher.Object);
        
        // Assert
        _dispatcher.Verify(d => d.Dispatch(It.IsAny<CreateConnectionFailureAction>()), Times.Once);
    }

    [Fact]
    public async Task HandleUpdateConnectionActionWhenSuccess()
    {
        // Arrange
        var connection = _fixture.Create<ConnectionDto>();
        var action = new UpdateConnectionAction(connection);

        // Act
        await _sut.HandleUpdateConnectionAction(action, _dispatcher.Object);

        // Assert
        _mediator.Verify(
            m => m.Send(It.Is<UpdateConnectionCommand>(c =>
                    c.Id == connection.Id &&
                    c.Name == connection.Name &&
                    c.ConnectionConfig.ConnectionType == connection.ConnectionConfig.ConnectionType &&
                    c.ConnectionConfig.ConnectionString == connection.ConnectionConfig.ConnectionString &&
                    c.ConnectionConfig.Namespace == connection.ConnectionConfig.Namespace &&
                    c.ConnectionConfig.TenantId == connection.ConnectionConfig.TenantId &&
                    c.ConnectionConfig.ClientId == connection.ConnectionConfig.ClientId),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        _dispatcher.Verify(d => d.Dispatch(It.IsAny<UpdateConnectionSuccessAction>()), Times.Once);
        _dispatcher.Verify(d => d.Dispatch(It.IsAny<FetchConnectionsAction>()), Times.Once);
    }
    
    [Fact]
    public async Task HandleUpdateConnectionActionWhenFailure()
    {
        // Arrange
        var connection = _fixture.Create<ConnectionDto>();
        var action = new UpdateConnectionAction(connection);
        
        _mediator
            .Setup(m => m.Send(It.IsAny<UpdateConnectionCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());
        
        // Act
        await _sut.HandleUpdateConnectionAction(action, _dispatcher.Object);
        
        // Assert
        _dispatcher.Verify(d => d.Dispatch(It.IsAny<UpdateConnectionFailureAction>()), Times.Once);
    }
    
    [Fact]
    public async Task HandleDeleteConnectionActionWhenSuccess()
    {
        // Arrange
        var connectionId = _fixture.Create<long>();
        var action = new DeleteConnectionAction(connectionId);

        // Act
        await _sut.HandleDeleteConnectionAction(action, _dispatcher.Object);

        // Assert
        _mediator.Verify(
            m => m.Send(It.Is<DeleteConnectionCommand>(c => c.Id == connectionId), It.IsAny<CancellationToken>()),
            Times.Once);
        
        _dispatcher.Verify(d => d.Dispatch(It.IsAny<DeleteConnectionSuccessAction>()), Times.Once);
        _dispatcher.Verify(d => d.Dispatch(It.IsAny<FetchConnectionsAction>()), Times.Once);
    }
    
    [Fact]
    public async Task HandleDeleteConnectionActionWhenFailure()
    {
        // Arrange
        var connectionId = _fixture.Create<long>();
        var action = new DeleteConnectionAction(connectionId);
        
        _mediator
            .Setup(m => m.Send(It.IsAny<DeleteConnectionCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());
        
        // Act
        await _sut.HandleDeleteConnectionAction(action, _dispatcher.Object);
        
        // Assert
        _dispatcher.Verify(d => d.Dispatch(It.IsAny<DeleteConnectionFailureAction>()), Times.Once);
    }
}