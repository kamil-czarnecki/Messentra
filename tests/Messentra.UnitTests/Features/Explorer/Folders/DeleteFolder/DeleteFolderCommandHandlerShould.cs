using Messentra.Domain;
using Messentra.Features.Explorer.Folders.DeleteFolder;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Explorer.Folders.DeleteFolder;

public sealed class DeleteFolderCommandHandlerShould : InMemoryDbTestBase
{
    private readonly DeleteFolderCommandHandler _sut;

    public DeleteFolderCommandHandlerShould()
    {
        _sut = new DeleteFolderCommandHandler(DbContextFactory);
    }

    [Fact]
    public async Task DeleteFolderAndItsResources()
    {
        // Arrange
        var connection = await SeedConnectionAsync();
        var folder = new Folder { ConnectionId = connection.Id, Name = "My Team", Resources = [new FolderResource { ResourceUrl = "queue:orders" }] };
        await DbContext.Set<Folder>().AddAsync(folder, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await _sut.Handle(new DeleteFolderCommand(folder.Id), CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Clear();
        (await DbContext.Set<Folder>().FindAsync([folder.Id], TestContext.Current.CancellationToken)).ShouldBeNull();
        (await DbContext.Set<FolderResource>().AnyAsync(r => r.FolderId == folder.Id, cancellationToken: TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task DoNothingWhenFolderDoesNotExist()
    {
        // Act
        var act = async () => await _sut.Handle(new DeleteFolderCommand(999), CancellationToken.None);

        // Assert
        await act.ShouldNotThrowAsync();
    }
}
