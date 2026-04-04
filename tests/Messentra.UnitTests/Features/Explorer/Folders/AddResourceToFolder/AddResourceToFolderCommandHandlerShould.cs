using Messentra.Domain;
using Messentra.Features.Explorer.Folders.AddResourceToFolder;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Explorer.Folders.AddResourceToFolder;

public sealed class AddResourceToFolderCommandHandlerShould : InMemoryDbTestBase
{
    private readonly AddResourceToFolderCommandHandler _sut;

    public AddResourceToFolderCommandHandlerShould()
    {
        _sut = new AddResourceToFolderCommandHandler(DbContextFactory);
    }

    [Fact]
    public async Task AddResourceUrlToFolder()
    {
        // Arrange
        var connection = await SeedConnectionAsync();
        var folder = new Folder { ConnectionId = connection.Id, Name = "My Team" };
        await DbContext.Set<Folder>().AddAsync(folder, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await _sut.Handle(new AddResourceToFolderCommand(folder.Id, "queue:orders"), CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Clear();
        var resource = await DbContext.Set<FolderResource>()
            .FirstOrDefaultAsync(r => r.FolderId == folder.Id && r.ResourceUrl == "queue:orders", cancellationToken: TestContext.Current.CancellationToken);
        resource.ShouldNotBeNull();
    }

    [Fact]
    public async Task BeIdempotentWhenResourceAlreadyAdded()
    {
        // Arrange
        var connection = await SeedConnectionAsync();
        var folder = new Folder { ConnectionId = connection.Id, Name = "My Team", Resources = [new FolderResource { ResourceUrl = "queue:orders" }] };
        await DbContext.Set<Folder>().AddAsync(folder, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var act = async () => await _sut.Handle(new AddResourceToFolderCommand(folder.Id, "queue:orders"), CancellationToken.None);
        await act.ShouldNotThrowAsync();

        // Assert
        DbContext.ChangeTracker.Clear();
        var count = await DbContext.Set<FolderResource>().CountAsync(r => r.FolderId == folder.Id && r.ResourceUrl == "queue:orders", cancellationToken: TestContext.Current.CancellationToken);
        count.ShouldBe(1);
    }
}
