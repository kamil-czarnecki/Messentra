using Mediator;
using Messentra.Domain;
using Messentra.Features.Mcp;
using Messentra.Features.Mcp.GetDlqSummary;
using Messentra.Features.Mcp.GetResource;
using Messentra.Features.Mcp.ListResources;
using Messentra.Features.Mcp.PeekMessages;
using Messentra.Features.Mcp.Tools;
using Moq;
using Shouldly;
using Xunit;
using SubQueue = Messentra.Features.Explorer.Messages.SubQueue;

namespace Messentra.UnitTests.Features.Mcp.Tools;

public sealed class ResourcesMcpToolShould
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IMcpHelpers> _helpers = new();
    private readonly ResourcesMcpTool _sut;

    public ResourcesMcpToolShould()
    {
        _sut = new ResourcesMcpTool(_mediator.Object, _helpers.Object);
    }

    // ── ListResources ────────────────────────────────────────────────────────

    [Fact]
    public async Task ListResources_ReturnsMcpError_WhenConnectionNotFound()
    {
        _helpers.Setup(h => h.ResolveConnection("unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Connection?)null);

        var result = await _sut.ListResources("unknown", ct: CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.AsError.Message.ShouldContain("unknown");
    }

    [Fact]
    public async Task ListResources_ReturnsMcpError_WhenFolderNotFound()
    {
        var connection = MakeConnection();
        _helpers.Setup(h => h.ResolveConnection("prod", It.IsAny<CancellationToken>())).ReturnsAsync(connection);
        _helpers.Setup(h => h.ResolveFolderResourceUrls(connection.Id, "missing-folder", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlySet<string>?)null);

        var result = await _sut.ListResources("prod", folderName: "missing-folder", ct: CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.AsError.Message.ShouldContain("missing-folder");
    }

    [Fact]
    public async Task ListResources_PassesNullUrlFilter_WhenNoFolderProvided()
    {
        var connection = MakeConnection();
        _helpers.Setup(h => h.ResolveConnection("prod", It.IsAny<CancellationToken>())).ReturnsAsync(connection);
        _mediator.Setup(m => m.Send(It.IsAny<ListResourcesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _sut.ListResources("prod", ct: CancellationToken.None);

        _mediator.Verify(m => m.Send(
            It.Is<ListResourcesQuery>(q => q.ResourceUrlFilter == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListResources_PassesUrlFilter_WhenFolderProvided()
    {
        var connection = MakeConnection();
        var urlFilter = new HashSet<string> { "sb://ns/queues/orders" };
        _helpers.Setup(h => h.ResolveConnection("prod", It.IsAny<CancellationToken>())).ReturnsAsync(connection);
        _helpers.Setup(h => h.ResolveFolderResourceUrls(connection.Id, "payments", It.IsAny<CancellationToken>()))
            .ReturnsAsync(urlFilter);
        _mediator.Setup(m => m.Send(It.IsAny<ListResourcesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _sut.ListResources("prod", folderName: "payments", ct: CancellationToken.None);

        _mediator.Verify(m => m.Send(
            It.Is<ListResourcesQuery>(q => q.ResourceUrlFilter == urlFilter),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListResources_ReturnsResources_FromMediator()
    {
        var connection = MakeConnection();
        var expected = new[] { MakeResourceSummary("orders") };
        _helpers.Setup(h => h.ResolveConnection("prod", It.IsAny<CancellationToken>())).ReturnsAsync(connection);
        _mediator.Setup(m => m.Send(It.IsAny<ListResourcesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.ListResources("prod", ct: CancellationToken.None);

        result.AsSuccess.ShouldBe(expected);
    }

    // ── GetResource ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetResource_ReturnsMcpError_WhenConnectionNotFound()
    {
        _helpers.Setup(h => h.ResolveConnection("unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Connection?)null);

        var result = await _sut.GetResource("unknown", "orders");

        result.IsError.ShouldBeTrue();
        result.AsError.Message.ShouldContain("unknown");
    }

    [Fact]
    public async Task GetResource_ReturnsResourceSummary_OnSuccess()
    {
        var connection = MakeConnection();
        var summary = MakeResourceSummary("orders");
        _helpers.Setup(h => h.ResolveConnection("prod", It.IsAny<CancellationToken>())).ReturnsAsync(connection);
        GetResourceResult handlerResult = summary;
        _mediator.Setup(m => m.Send(It.IsAny<GetResourceQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(handlerResult);

        var result = await _sut.GetResource("prod", "orders");

        result.IsError.ShouldBeFalse();
        result.AsSuccess.ShouldBe(summary);
    }

    [Fact]
    public async Task GetResource_ReturnsMcpError_WhenHandlerReturnsError()
    {
        var connection = MakeConnection();
        var error = new McpError("Resource 'missing' not found.");
        _helpers.Setup(h => h.ResolveConnection("prod", It.IsAny<CancellationToken>())).ReturnsAsync(connection);
        GetResourceResult handlerResult = error;
        _mediator.Setup(m => m.Send(It.IsAny<GetResourceQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(handlerResult);

        var result = await _sut.GetResource("prod", "missing");

        result.IsError.ShouldBeTrue();
        result.AsError.Message.ShouldContain("missing");
    }

    // ── PeekMessages ─────────────────────────────────────────────────────────

    [Fact]
    public async Task PeekMessages_ReturnsMcpError_WhenConnectionNotFound()
    {
        _helpers.Setup(h => h.ResolveConnection("unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Connection?)null);

        var result = await _sut.PeekMessages("unknown", "orders");

        result.IsError.ShouldBeTrue();
        result.AsError.Message.ShouldContain("unknown");
    }

    [Fact]
    public async Task PeekMessages_ParsesDlqSubqueue()
    {
        var connection = MakeConnection();
        _helpers.Setup(h => h.ResolveConnection("prod", It.IsAny<CancellationToken>())).ReturnsAsync(connection);
        PeekMessagesQueryResult handlerResult = new PeekMessagesResult([], null);
        _mediator.Setup(m => m.Send(It.IsAny<PeekMessagesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(handlerResult);

        await _sut.PeekMessages("prod", "orders", subQueue: "dlq");

        _mediator.Verify(m => m.Send(
            It.Is<PeekMessagesQuery>(q => q.Subqueue == SubQueue.DeadLetter),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PeekMessages_ParsesActiveSubqueue()
    {
        var connection = MakeConnection();
        _helpers.Setup(h => h.ResolveConnection("prod", It.IsAny<CancellationToken>())).ReturnsAsync(connection);
        PeekMessagesQueryResult handlerResult = new PeekMessagesResult([], null);
        _mediator.Setup(m => m.Send(It.IsAny<PeekMessagesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(handlerResult);

        await _sut.PeekMessages("prod", "orders", subQueue: "active");

        _mediator.Verify(m => m.Send(
            It.Is<PeekMessagesQuery>(q => q.Subqueue == SubQueue.Active),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PeekMessages_ClampsMaxMessages_ToMax100()
    {
        var connection = MakeConnection();
        _helpers.Setup(h => h.ResolveConnection("prod", It.IsAny<CancellationToken>())).ReturnsAsync(connection);
        PeekMessagesQueryResult handlerResult = new PeekMessagesResult([], null);
        _mediator.Setup(m => m.Send(It.IsAny<PeekMessagesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(handlerResult);

        await _sut.PeekMessages("prod", "orders", maxMessages: 999);

        _mediator.Verify(m => m.Send(
            It.Is<PeekMessagesQuery>(q => q.MaxMessages == 100),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PeekMessages_ReturnsMcpError_WhenHandlerReturnsError()
    {
        var connection = MakeConnection();
        _helpers.Setup(h => h.ResolveConnection("prod", It.IsAny<CancellationToken>())).ReturnsAsync(connection);
        PeekMessagesQueryResult handlerResult = new McpError("Resource 'orders' not found.");
        _mediator.Setup(m => m.Send(It.IsAny<PeekMessagesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(handlerResult);

        var result = await _sut.PeekMessages("prod", "orders");

        result.IsError.ShouldBeTrue();
    }

    // ── GetDlqSummary ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDlqSummary_ReturnsMcpError_WhenConnectionNotFound()
    {
        _helpers.Setup(h => h.ResolveConnection("unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Connection?)null);

        var result = await _sut.GetDlqSummary("unknown", "orders");

        result.IsError.ShouldBeTrue();
        result.AsError.Message.ShouldContain("unknown");
    }

    [Fact]
    public async Task GetDlqSummary_ClampsSampleSize_ToMax2000()
    {
        var connection = MakeConnection();
        _helpers.Setup(h => h.ResolveConnection("prod", It.IsAny<CancellationToken>())).ReturnsAsync(connection);
        GetDlqSummaryQueryResult handlerResult = new DlqSummaryResult(0, null, []);
        _mediator.Setup(m => m.Send(It.IsAny<GetDlqSummaryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(handlerResult);

        await _sut.GetDlqSummary("prod", "orders", sampleSize: 9999);

        _mediator.Verify(m => m.Send(
            It.Is<GetDlqSummaryQuery>(q => q.SampleSize == 2000),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDlqSummary_ReturnsDlqSummary_OnSuccess()
    {
        var connection = MakeConnection();
        var summary = new DlqSummaryResult(10, null, [new DlqReasonGroup("MaxDeliveryCountExceeded", null, 10, "{}")]);
        _helpers.Setup(h => h.ResolveConnection("prod", It.IsAny<CancellationToken>())).ReturnsAsync(connection);
        GetDlqSummaryQueryResult handlerResult = summary;
        _mediator.Setup(m => m.Send(It.IsAny<GetDlqSummaryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(handlerResult);

        var result = await _sut.GetDlqSummary("prod", "orders");

        result.IsError.ShouldBeFalse();
        result.AsSuccess.ShouldBe(summary);
    }

    [Fact]
    public async Task GetDlqSummary_ReturnsMcpError_WhenHandlerReturnsError()
    {
        var connection = MakeConnection();
        _helpers.Setup(h => h.ResolveConnection("prod", It.IsAny<CancellationToken>())).ReturnsAsync(connection);
        GetDlqSummaryQueryResult handlerResult = new McpError("Resource 'orders' not found.");
        _mediator.Setup(m => m.Send(It.IsAny<GetDlqSummaryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(handlerResult);

        var result = await _sut.GetDlqSummary("prod", "orders");

        result.IsError.ShouldBeTrue();
    }

    private static Connection MakeConnection(long id = 1) => new()
    {
        Name = "prod",
        ConnectionConfig = ConnectionConfig.CreateConnectionString(
            "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=Root;SharedAccessKey=test")
    };

    private static ResourceSummary MakeResourceSummary(string name) => new(
        name, "queue", null, "Active", 100, 5, 0, 0, 0, 10, false, null);
}
