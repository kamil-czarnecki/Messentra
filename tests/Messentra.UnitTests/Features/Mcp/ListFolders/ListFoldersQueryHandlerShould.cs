using Messentra.Domain;
using Messentra.Features.Mcp.ListFolders;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Mcp.ListFolders;

public sealed class ListFoldersQueryHandlerShould : InMemoryDbTestBase
{
    private readonly ListFoldersQueryHandler _sut;

    public ListFoldersQueryHandlerShould()
    {
        _sut = new ListFoldersQueryHandler(DbContextFactory);
    }

    [Fact]
    public async Task ReturnFolderNames_ForConnection()
    {
        var connection = await SeedConnectionAsync("PROD");
        await DbContext.Set<Folder>().AddAsync(new Folder { ConnectionId = connection.Id, Name = "Squad" });
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _sut.Handle(new ListFoldersQuery(connection.Id), TestContext.Current.CancellationToken);

        result.ShouldHaveSingleItem().Name.ShouldBe("Squad");
    }

    [Fact]
    public async Task ReturnMultipleFolders()
    {
        var connection = await SeedConnectionAsync("PROD");
        await DbContext.Set<Folder>().AddRangeAsync(
            new Folder { ConnectionId = connection.Id, Name = "Alpha" },
            new Folder { ConnectionId = connection.Id, Name = "Beta" });
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _sut.Handle(new ListFoldersQuery(connection.Id), TestContext.Current.CancellationToken);

        result.Count().ShouldBe(2);
    }

    [Fact]
    public async Task ReturnEmpty_WhenNoFolders()
    {
        var connection = await SeedConnectionAsync("PROD");

        var result = await _sut.Handle(new ListFoldersQuery(connection.Id), TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }
}
