using AutoFixture;
using FluentValidation;
using Messentra.Domain;
using Messentra.Features.Settings.Connections.CreateConnection;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Settings.Connections.CreateConnection;

public sealed class CreateConnectionCommandHandlerShould : InMemoryDbTestBase
{
    private readonly Fixture _fixture = new();
    private readonly CreateConnectionCommandHandler _sut;

    public CreateConnectionCommandHandlerShould()
    {
        _sut = new CreateConnectionCommandHandler(DbContext, new CreateConnectionCommandValidator());
    }

    [Fact]
    public async Task CreateConnectionForConnectionStringConfig()
    {
        // Arrange
        var command = _fixture
            .Build<CreateConnectionCommand>()
            .With(x => x.ConnectionConfig, new ConnectionConfigDto(
                ConnectionType.ConnectionString,
                "Endpoint=test",
                null,
                null,
                null))
            .Create();
        
        // Act
        await _sut.Handle(command, CancellationToken.None);
        
        // Assert
        var connection = await DbContext.Set<Connection>().SingleOrDefaultAsync(x => x.Id == 1, CancellationToken.None);
        connection.ShouldNotBeNull();
        connection.Name.ShouldBe(command.Name);
        connection.ConnectionConfig.ConnectionType.ShouldBe(ConnectionType.ConnectionString);
        connection.ConnectionConfig.ConnectionStringConfig!.ConnectionString.ShouldBe("Endpoint=test");
        connection.ConnectionConfig.EntraIdConfig.ShouldBeNull();
    }
    
    [Fact]
    public async Task CreateConnectionForEntraIdConfig()
    {
        // Arrange
        var command = _fixture
            .Build<CreateConnectionCommand>()
            .With(x => x.ConnectionConfig, new ConnectionConfigDto(
                ConnectionType.EntraId,
                null,
                "namespace",
                "tenant",
                "client"))
            .Create();
        
        // Act
        await _sut.Handle(command, CancellationToken.None);
        
        // Assert
        var connection = await DbContext.Set<Connection>().SingleOrDefaultAsync(x => x.Id == 1, CancellationToken.None);
        connection.ShouldNotBeNull();
        connection.Name.ShouldBe(command.Name);
        connection.ConnectionConfig.ConnectionType.ShouldBe(ConnectionType.EntraId);
        connection.ConnectionConfig.ConnectionStringConfig.ShouldBeNull();
        connection.ConnectionConfig.EntraIdConfig!.Namespace.ShouldBe("namespace");
        connection.ConnectionConfig.EntraIdConfig.TenantId.ShouldBe("tenant");
        connection.ConnectionConfig.EntraIdConfig.ClientId.ShouldBe("client");
    }
    
    [Fact]
    public async Task ThrowWhenValidationFails()
    {
        // Arrange
        var incorrectCommand = _fixture
            .Build<CreateConnectionCommand>()
            .With(x => x.Name, string.Empty)
            .Create();
        
        // Act
        var func = async () => await _sut.Handle(incorrectCommand, CancellationToken.None);
        
        await func.ShouldThrowAsync<ValidationException>();
    }
}