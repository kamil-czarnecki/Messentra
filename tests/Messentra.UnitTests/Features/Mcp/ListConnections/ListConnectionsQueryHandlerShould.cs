using Messentra.Features.Mcp.ListConnections;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Mcp.ListConnections;

public sealed class ListConnectionsQueryHandlerShould : InMemoryDbTestBase
{
    private readonly ListConnectionsQueryHandler _sut;

    public ListConnectionsQueryHandlerShould()
    {
        _sut = new ListConnectionsQueryHandler(DbContextFactory);
    }

    [Fact]
    public async Task ReturnNameAndNamespace_ForConnectionStringConnection()
    {
        await SeedConnectionAsync("PROD", connectionString: "Endpoint=sb://my-namespace.servicebus.windows.net;SharedAccessKeyName=...");

        var result = await _sut.Handle(new ListConnectionsQuery(), CancellationToken.None);

        var summary = result.ShouldHaveSingleItem();
        summary.Name.ShouldBe("PROD");
        summary.Namespace.ShouldBe("my-namespace.servicebus.windows.net");
    }

    [Fact]
    public async Task ReturnNameAndNamespace_ForEntraIdConnection()
    {
        await SeedConnectionAsync("DEV", entraIdNamespace: "dev-namespace.servicebus.windows.net");

        var result = await _sut.Handle(new ListConnectionsQuery(), CancellationToken.None);

        var summary = result.ShouldHaveSingleItem();
        summary.Name.ShouldBe("DEV");
        summary.Namespace.ShouldBe("dev-namespace.servicebus.windows.net");
    }

    [Fact]
    public async Task ReturnCorruptedNamespace_ForCorruptedConnection()
    {
        await SeedCorruptedConnectionAsync("BROKEN");

        var result = await _sut.Handle(new ListConnectionsQuery(), CancellationToken.None);

        var summary = result.ShouldHaveSingleItem();
        summary.Name.ShouldBe("BROKEN");
        summary.Namespace.ShouldBe("(corrupted)");
    }
}
