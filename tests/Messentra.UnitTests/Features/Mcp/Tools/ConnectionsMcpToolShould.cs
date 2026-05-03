using Mediator;
using Messentra.Features.Mcp.ListConnections;
using Messentra.Features.Mcp.Tools;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Mcp.Tools;

public sealed class ConnectionsMcpToolShould
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly ConnectionsMcpTool _sut;

    public ConnectionsMcpToolShould()
    {
        _sut = new ConnectionsMcpTool(_mediator.Object);
    }

    [Fact]
    public async Task ReturnConnections_FromMediator()
    {
        var expected = new[] { new ConnectionSummary("prod", "sb://prod.servicebus.windows.net") };
        _mediator.Setup(m => m.Send(It.IsAny<ListConnectionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.ListConnections(CancellationToken.None);

        result.ShouldBe(expected);
    }

    [Fact]
    public async Task ReturnEmpty_WhenNoConnectionsExist()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ListConnectionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.ListConnections(CancellationToken.None);

        result.ShouldBeEmpty();
    }
}
