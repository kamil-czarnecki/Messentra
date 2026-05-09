using Messentra.Domain;
using Messentra.Features.Mcp;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Mcp;

public sealed class McpHelpersShould : InMemoryDbTestBase
{
    private readonly McpHelpers _sut;

    public McpHelpersShould()
    {
        _sut = new McpHelpers(DbContextFactory);
    }

    [Fact]
    public async Task ResolveConnection_ReturnsConnection_WhenNameMatchesExactly()
    {
        var connection = await SeedConnectionAsync("PROD");

        var result = await _sut.ResolveConnection("PROD", CancellationToken.None);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(connection.Id);
    }

    [Fact]
    public async Task ResolveConnection_IsCaseInsensitive()
    {
        var connection = await SeedConnectionAsync("PROD");

        var result = await _sut.ResolveConnection("prod", CancellationToken.None);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(connection.Id);
    }

    [Fact]
    public async Task ResolveConnection_ReturnsNull_WhenNotFound()
    {
        var result = await _sut.ResolveConnection("NONEXISTENT", CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveFolderResourceUrls_ReturnsUrls_WhenFolderFound()
    {
        var connection = await SeedConnectionAsync("PROD");
        await DbContext.Set<Folder>().AddAsync(new Folder
        {
            ConnectionId = connection.Id,
            Name = "Squad",
            Resources = [new FolderResource { ResourceUrl = "queue:orders" }]
        }, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _sut.ResolveFolderResourceUrls(connection.Id, "squad", CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldContain("queue:orders");
    }

    [Fact]
    public async Task ResolveFolderResourceUrls_IsCaseInsensitive()
    {
        var connection = await SeedConnectionAsync("PROD");
        await DbContext.Set<Folder>().AddAsync(new Folder
        {
            ConnectionId = connection.Id,
            Name = "SQUAD",
            Resources = []
        }, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _sut.ResolveFolderResourceUrls(connection.Id, "squad", CancellationToken.None);

        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task ResolveFolderResourceUrls_ReturnsNull_WhenFolderNotFound()
    {
        var connection = await SeedConnectionAsync("PROD");

        var result = await _sut.ResolveFolderResourceUrls(connection.Id, "nonexistent", CancellationToken.None);

        result.ShouldBeNull();
    }
}
