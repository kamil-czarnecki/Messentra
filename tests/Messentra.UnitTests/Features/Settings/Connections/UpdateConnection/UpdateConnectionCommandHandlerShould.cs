using AutoFixture;
using FluentValidation;
using Messentra.Domain;
using Messentra.Features.Settings.Connections.UpdateConnection;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;
using ConnectionConfigDto = Messentra.Features.Settings.Connections.UpdateConnection.ConnectionConfigDto;

namespace Messentra.UnitTests.Features.Settings.Connections.UpdateConnection;

public sealed class UpdateConnectionCommandHandlerShould : InMemoryDbTestBase
{
    private readonly Fixture _fixture = new();
    private readonly UpdateConnectionCommandHandler _sut;

    public UpdateConnectionCommandHandlerShould()
    {
        _sut = new UpdateConnectionCommandHandler(DbContext, new UpdateConnectionCommandValidator());
    }

    [Fact]
    public async Task UpdateConnection()
    {
        // Arrange
        var connection = await GivenConnection();
        var command = new UpdateConnectionCommand(
            connection.Id,
            _fixture.Create<string>(),
            new ConnectionConfigDto(
                connection.ConnectionConfig.ConnectionType,
                connection.ConnectionConfig.ConnectionStringConfig?.ConnectionString,
                connection.ConnectionConfig.EntraIdConfig?.Namespace,
                connection.ConnectionConfig.EntraIdConfig?.TenantId,
                connection.ConnectionConfig.EntraIdConfig?.ClientId));
        
        // Act
        await _sut.Handle(command, CancellationToken.None);
        
        // Assert
        var updatedConnection = await DbContext.Set<Connection>().SingleOrDefaultAsync(x => x.Id == connection.Id, CancellationToken.None);
        updatedConnection.ShouldNotBeNull();
        updatedConnection.Name.ShouldBe(command.Name);
        updatedConnection.ConnectionConfig.ConnectionType.ShouldBe(connection.ConnectionConfig.ConnectionType);
        updatedConnection.ConnectionConfig.ConnectionStringConfig?.ConnectionString.ShouldBe(connection.ConnectionConfig.ConnectionStringConfig?.ConnectionString);
        updatedConnection.ConnectionConfig.EntraIdConfig?.Namespace.ShouldBe(connection.ConnectionConfig.EntraIdConfig?.Namespace);
        updatedConnection.ConnectionConfig.EntraIdConfig?.TenantId.ShouldBe(connection.ConnectionConfig.EntraIdConfig?.TenantId);
        updatedConnection.ConnectionConfig.EntraIdConfig?.ClientId.ShouldBe(connection.ConnectionConfig.EntraIdConfig?.ClientId);
    }
    
    [Fact]
    public async Task DoNothingWhenConnectionNotFound()
    {
        // Arrange
        var connection = await GivenConnection();
        var command = _fixture
            .Build<UpdateConnectionCommand>()
            .With(x => x.Id, connection.Id + 1)
            .With(x => x.Name, "new name")
            .Create();
        
        // Act
        await _sut.Handle(command, CancellationToken.None);
        
        // Assert
        var allConnections = await DbContext
            .Set<Connection>()
            .ToListAsync(CancellationToken.None);
        allConnections.Count.ShouldBe(1);
        allConnections[0].ShouldBeEquivalentTo(connection);
    }
    
    [Fact]
    public async Task AllowUpdatingConnectionToKeepItsOwnName()
    {
        // Arrange
        var connection = await GivenConnection();
        var command = new UpdateConnectionCommand(
            connection.Id,
            connection.Name,
            new ConnectionConfigDto(
                connection.ConnectionConfig.ConnectionType,
                connection.ConnectionConfig.ConnectionStringConfig?.ConnectionString,
                connection.ConnectionConfig.EntraIdConfig?.Namespace,
                connection.ConnectionConfig.EntraIdConfig?.TenantId,
                connection.ConnectionConfig.EntraIdConfig?.ClientId));

        // Act & Assert — should not throw when name is unchanged
        await _sut.Handle(command, CancellationToken.None);
    }

    [Fact]
    public async Task ThrowWhenConnectionNameAlreadyTakenByAnotherConnection()
    {
        // Arrange
        var existing = await GivenConnection();
        var target = await GivenConnection();

        var command = new UpdateConnectionCommand(
            target.Id,
            existing.Name,
            new ConnectionConfigDto(
                target.ConnectionConfig.ConnectionType,
                target.ConnectionConfig.ConnectionStringConfig?.ConnectionString,
                target.ConnectionConfig.EntraIdConfig?.Namespace,
                target.ConnectionConfig.EntraIdConfig?.TenantId,
                target.ConnectionConfig.EntraIdConfig?.ClientId));

        // Act
        var func = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        var ex = await func.ShouldThrowAsync<ValidationException>();
        ex.Message.ShouldContain(existing.Name);
    }

    [Fact]
    public async Task ThrowWhenConnectionNameExistsWithDifferentCasing()
    {
        // Arrange
        var existing = await GivenConnection();
        var target = await GivenConnection();

        var command = new UpdateConnectionCommand(
            target.Id,
            existing.Name.ToUpper(),
            new ConnectionConfigDto(
                target.ConnectionConfig.ConnectionType,
                target.ConnectionConfig.ConnectionStringConfig?.ConnectionString,
                target.ConnectionConfig.EntraIdConfig?.Namespace,
                target.ConnectionConfig.EntraIdConfig?.TenantId,
                target.ConnectionConfig.EntraIdConfig?.ClientId));

        // Act
        var func = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        var ex = await func.ShouldThrowAsync<ValidationException>();
        ex.Message.ShouldContain(existing.Name.ToUpper());
    }

    [Fact]
    public async Task ThrowWhenValidationFails()
    {
        // Arrange
        var incorrectCommand = _fixture
            .Build<UpdateConnectionCommand>()
            .With(x => x.Name, string.Empty)
            .Create();
        
        // Act
        var func = async () => await _sut.Handle(incorrectCommand, CancellationToken.None);
        
        await func.ShouldThrowAsync<ValidationException>();
    }

    private async Task<Connection> GivenConnection()
    {
        var connection = _fixture.Create<Connection>();
        
        await DbContext.Set<Connection>().AddAsync(connection, CancellationToken.None);
        await DbContext.SaveChangesAsync();
        
        return connection;
    }
}