using Messentra.Domain;
using Messentra.Features.Explorer.Folders.CreateFolder;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Explorer.Folders.CreateFolder;

public sealed class CreateFolderCommandHandlerShould : InMemoryDbTestBase
{
    private readonly CreateFolderCommandHandler _sut;

    public CreateFolderCommandHandlerShould()
    {
        _sut = new CreateFolderCommandHandler(DbContextFactory);
    }

    [Fact]
    public async Task CreateFolderAndReturnId()
    {
        // Arrange
        var connection = await SeedConnectionAsync();

        // Act
        var id = await _sut.Handle(new CreateFolderCommand(ConnectionId: connection.Id, Name: "My Team"), CancellationToken.None);

        // Assert
        id.ShouldBeGreaterThan(0);
        var folder = await DbContext.Set<Folder>().FirstOrDefaultAsync(f => f.Id == id, cancellationToken: TestContext.Current.CancellationToken);
        folder.ShouldNotBeNull();
        folder.Name.ShouldBe("My Team");
        folder.ConnectionId.ShouldBe(connection.Id);
    }

    [Fact]
    public async Task RejectDuplicateFolderNameInSameConnection()
    {
        // Arrange
        var connection = await SeedConnectionAsync();
        await _sut.Handle(new CreateFolderCommand(connection.Id, "My Team"), CancellationToken.None);

        // Act
        var act = async () => await _sut.Handle(new CreateFolderCommand(connection.Id, " my team "), CancellationToken.None);

        // Assert
        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task AllowSameFolderNameInDifferentConnections()
    {
        // Arrange
        var connection1 = await SeedConnectionAsync();
        var connection2 = await SeedConnectionAsync();

        // Act
        var id1 = await _sut.Handle(new CreateFolderCommand(connection1.Id, "My Team"), CancellationToken.None);
        var id2 = await _sut.Handle(new CreateFolderCommand(connection2.Id, "My Team"), CancellationToken.None);

        // Assert
        id1.ShouldNotBe(id2);
        var count = await DbContext.Set<Folder>()
            .CountAsync(f => f.Name == "My Team", cancellationToken: TestContext.Current.CancellationToken);
        count.ShouldBe(2);
    }
}
