using AutoFixture;
using Messentra.Domain;
using Messentra.Features.Settings.Connections.GetConnections;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Settings.Connections.GetConnections;

public sealed class GetConnectionsQueryHandlerShould : InMemoryDbTestBase
{
    private readonly Fixture _fixture = new();
    private readonly GetConnectionsQueryHandler _sut;

    public GetConnectionsQueryHandlerShould()
    {
        _sut = new GetConnectionsQueryHandler(DbContext);
    }
    
    [Fact]
    public async Task ReturnConnections()
    {
        // Arrange
        var connections = await GivenConnections();

        // Act
        var result = await _sut.Handle(new GetConnectionsQuery(), CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        
        var connection1 = result.FirstOrDefault(x => x.Name == connections[0].Name);
        connection1.ShouldBeEquivalentTo(ConnectionDto.From(connections[0]));
        
        var connection2 = result.FirstOrDefault(x => x.Name == connections[1].Name);
        connection2.ShouldBeEquivalentTo(ConnectionDto.From(connections[1]));
    }
    
    [Fact]
    public async Task ReturnEmptyListWhenNoConnections()
    {
        // Act
        var result = await _sut.Handle(new GetConnectionsQuery(), CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }

    private async Task<Connection[]> GivenConnections()
    {
        var connections = _fixture.CreateMany<Connection>(2).ToArray();
        
        await DbContext.Set<Connection>().AddRangeAsync(connections);
        await DbContext.SaveChangesAsync();
        
        return connections;
    }
}