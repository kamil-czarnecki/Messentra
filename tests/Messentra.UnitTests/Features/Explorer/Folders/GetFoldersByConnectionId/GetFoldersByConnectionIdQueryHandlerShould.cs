using Messentra.Domain;
using Messentra.Features.Explorer.Folders.GetFoldersByConnectionId;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Explorer.Folders.GetFoldersByConnectionId;

public sealed class GetFoldersByConnectionIdQueryHandlerShould : InMemoryDbTestBase
{
    private readonly GetFoldersByConnectionIdQueryHandler _sut;

    public GetFoldersByConnectionIdQueryHandlerShould()
    {
        _sut = new GetFoldersByConnectionIdQueryHandler(DbContextFactory);
    }

    [Fact]
    public async Task ReturnFoldersForConnection()
    {
        // Arrange
        var connection1 = await SeedConnectionAsync();
        var connection2 = await SeedConnectionAsync();

        await DbContext.Set<Folder>().AddRangeAsync(
            new Folder { ConnectionId = connection1.Id, Name = "My Team", Resources = [new FolderResource { ResourceUrl = "queue:orders" }] },
            new Folder { ConnectionId = connection1.Id, Name = "Monitoring", Resources = [] },
            new Folder { ConnectionId = connection2.Id, Name = "Other Connection", Resources = [] });
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.Handle(new GetFoldersByConnectionIdQuery(connection1.Id), CancellationToken.None);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(f => f.Name == "My Team" && f.ResourceUrls.Contains("queue:orders"));
        result.ShouldContain(f => f.Name == "Monitoring" && f.ResourceUrls.Count == 0);
    }

    [Fact]
    public async Task ReturnEmptyWhenNoFoldersExist()
    {
        // Act
        var result = await _sut.Handle(new GetFoldersByConnectionIdQuery(99), CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }
}
