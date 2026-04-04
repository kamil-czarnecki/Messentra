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
        _sut = new CreateFolderCommandHandler(DbContext);
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
        var folder = await DbContext.Set<Folder>().FirstOrDefaultAsync(f => f.Id == id);
        folder.ShouldNotBeNull();
        folder.Name.ShouldBe("My Team");
        folder.ConnectionId.ShouldBe(connection.Id);
    }
}
