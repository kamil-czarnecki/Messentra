using Messentra.Domain;
using Messentra.Features.Explorer.Folders.RemoveResourceFromFolder;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Explorer.Folders.RemoveResourceFromFolder;

public sealed class RemoveResourceFromFolderCommandHandlerShould : InMemoryDbTestBase
{
    private readonly RemoveResourceFromFolderCommandHandler _sut;

    public RemoveResourceFromFolderCommandHandlerShould()
    {
        _sut = new RemoveResourceFromFolderCommandHandler(DbContextFactory);
    }

    [Fact]
    public async Task RemoveResourceFromFolder()
    {
        // Arrange
        var connection = await SeedConnectionAsync();
        var folder = new Folder { ConnectionId = connection.Id, Name = "My Team", Resources = [new FolderResource { ResourceUrl = "queue:orders" }] };
        await DbContext.Set<Folder>().AddAsync(folder, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await _sut.Handle(new RemoveResourceFromFolderCommand(folder.Id, "queue:orders"), CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Clear();
        (await DbContext.Set<FolderResource>().AnyAsync(r => r.FolderId == folder.Id, cancellationToken: TestContext.Current.CancellationToken)).ShouldBeFalse();
    }
}
