using Mediator;
using Messentra.Domain;
using Messentra.Features.Mcp;
using Messentra.Features.Mcp.ListFolders;
using Messentra.Features.Mcp.Tools;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Mcp.Tools;

public sealed class FoldersMcpToolShould
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IMcpHelpers> _helpers = new();
    private readonly FoldersMcpTool _sut;

    public FoldersMcpToolShould()
    {
        _sut = new FoldersMcpTool(_mediator.Object, _helpers.Object);
    }

    [Fact]
    public async Task ReturnMcpError_WhenConnectionNotFound()
    {
        _helpers.Setup(h => h.ResolveConnection("unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Connection?)null);

        var result = await _sut.ListFolders("unknown", CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.AsError.Message.ShouldContain("unknown");
    }

    [Fact]
    public async Task ReturnFolders_WhenConnectionFound()
    {
        var connection = MakeConnection();
        var expected = new[] { new FolderSummary("payments"), new FolderSummary("events") };
        _helpers.Setup(h => h.ResolveConnection("prod", It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);
        _mediator.Setup(m => m.Send(It.Is<ListFoldersQuery>(q => q.ConnectionId == connection.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.ListFolders("prod", CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.AsSuccess.ShouldBe(expected);
    }

    [Fact]
    public async Task PassConnectionId_ToMediator()
    {
        var connection = MakeConnection(id: 42);
        _helpers.Setup(h => h.ResolveConnection("prod", It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection);
        _mediator.Setup(m => m.Send(It.IsAny<ListFoldersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _sut.ListFolders("prod", CancellationToken.None);

        _mediator.Verify(m => m.Send(It.Is<ListFoldersQuery>(q => q.ConnectionId == 42), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Connection MakeConnection(long id = 1) => new()
    {
        Id = id,
        Name = "prod",
        ConnectionConfig = ConnectionConfig.CreateConnectionString(
            "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=Root;SharedAccessKey=test")
    };
}
