using AutoFixture;
using Messentra.Domain;
using Messentra.Features.Settings.Connections.DeleteConnection;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Settings.Connections.DeleteConnection;

public sealed class DeleteConnectionCommandHandlerShould : InMemoryDbTestBase
{
    private readonly Fixture _fixture = new();
    private readonly DeleteConnectionCommandHandler _sut;

    public DeleteConnectionCommandHandlerShould()
    {
        _sut = new DeleteConnectionCommandHandler(DbContext);
    }

    [Fact]
    public async Task DeleteConnection()
    {
        // Arrange
        var connection = GivenConnection();
        
        // Act
        await _sut.Handle(new DeleteConnectionCommand(connection.Id), CancellationToken.None);
        
        // Assert
        var deletedConnection = await DbContext
            .Set<Connection>()
            .FirstOrDefaultAsync(x => x.Id == connection.Id, CancellationToken.None);
        deletedConnection.ShouldBeNull();
    }

    [Fact]
    public async Task DoNothingWhenConnectionDoesNotExist()
    {
        // Act
        var func = async () => await _sut.Handle(new DeleteConnectionCommand(999), CancellationToken.None);

        await func.ShouldNotThrowAsync();
    }
    

    private async Task<Connection> GivenConnection()
    {
        var connection = _fixture.Create<Connection>();
        
        await DbContext.AddAsync(connection, CancellationToken.None);
        await DbContext.SaveChangesAsync(CancellationToken.None);
        
        return connection;
    }
}