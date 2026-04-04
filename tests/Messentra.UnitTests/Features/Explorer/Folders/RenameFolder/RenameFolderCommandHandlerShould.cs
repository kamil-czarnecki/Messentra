using Messentra.Domain;
using Messentra.Features.Explorer.Folders.RenameFolder;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Explorer.Folders.RenameFolder;

public sealed class RenameFolderCommandHandlerShould : InMemoryDbTestBase
{
    private readonly RenameFolderCommandHandler _sut;

    public RenameFolderCommandHandlerShould()
    {
        _sut = new RenameFolderCommandHandler(DbContext);
    }

    [Fact]
    public async Task UpdateFolderName()
    {
        // Arrange
        var connection = await SeedConnectionAsync();
        var folder = new Folder { ConnectionId = connection.Id, Name = "Old Name" };
        await DbContext.Set<Folder>().AddAsync(folder);
        await DbContext.SaveChangesAsync();

        // Act
        await _sut.Handle(new RenameFolderCommand(folder.Id, "New Name"), CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Clear();
        var updated = await DbContext.Set<Folder>().FindAsync(folder.Id);
        updated!.Name.ShouldBe("New Name");
    }
}
