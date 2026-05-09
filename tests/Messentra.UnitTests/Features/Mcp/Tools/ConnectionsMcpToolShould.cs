using Mediator;
using Messentra.Features.Mcp;
using Messentra.Features.Mcp.ListConnections;
using Messentra.Features.Mcp.Tools;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Mcp.Tools;

public sealed class ConnectionsMcpToolShould
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IMcpHelpers> _helpers = new();
    private readonly ConnectionsMcpTool _sut;

    public ConnectionsMcpToolShould()
    {
        _sut = new ConnectionsMcpTool(_mediator.Object, _helpers.Object);
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

    // AddConnection tests

    [Fact]
    public async Task AddConnection_ReturnsMcpError_WhenConnectionTypeIsInvalid()
    {
        var result = await _sut.AddConnection(
            "prod", "INVALID_TYPE",
            connectionString: null, @namespace: null, tenantId: null, clientId: null,
            CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.AsError.Message.ShouldContain("INVALID_TYPE");
    }

    [Fact]
    public async Task AddConnection_SendsCreateConnectionCommand_WithConnectionString()
    {
        _mediator
            .Setup(m => m.Send(It.IsAny<Messentra.Features.Settings.Connections.CreateConnection.CreateConnectionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mediator.Unit.Value);

        var result = await _sut.AddConnection(
            "prod", "ConnectionString",
            connectionString: "Endpoint=sb://prod.servicebus.windows.net/;SharedAccessKeyName=Root;SharedAccessKey=key",
            @namespace: null, tenantId: null, clientId: null,
            CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.AsSuccess.ShouldContain("prod");
        _mediator.Verify(m => m.Send(
            It.Is<Messentra.Features.Settings.Connections.CreateConnection.CreateConnectionCommand>(
                c => c.Name == "prod" &&
                     c.ConnectionConfig.ConnectionType == Messentra.Domain.ConnectionType.ConnectionString &&
                     c.ConnectionConfig.ConnectionString == "Endpoint=sb://prod.servicebus.windows.net/;SharedAccessKeyName=Root;SharedAccessKey=key"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddConnection_SendsCreateConnectionCommand_WithEntraId()
    {
        _mediator
            .Setup(m => m.Send(It.IsAny<Messentra.Features.Settings.Connections.CreateConnection.CreateConnectionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mediator.Unit.Value);

        var result = await _sut.AddConnection(
            "prod", "EntraId",
            connectionString: null,
            @namespace: "prod.servicebus.windows.net",
            tenantId: "tenant-123",
            clientId: "client-456",
            CancellationToken.None);

        result.IsError.ShouldBeFalse();
        _mediator.Verify(m => m.Send(
            It.Is<Messentra.Features.Settings.Connections.CreateConnection.CreateConnectionCommand>(
                c => c.Name == "prod" &&
                     c.ConnectionConfig.ConnectionType == Messentra.Domain.ConnectionType.EntraId &&
                     c.ConnectionConfig.Namespace == "prod.servicebus.windows.net" &&
                     c.ConnectionConfig.TenantId == "tenant-123" &&
                     c.ConnectionConfig.ClientId == "client-456"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddConnection_ReturnsMcpError_WhenValidationFails()
    {
        _mediator
            .Setup(m => m.Send(It.IsAny<Messentra.Features.Settings.Connections.CreateConnection.CreateConnectionCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FluentValidation.ValidationException("Name is required."));

        var result = await _sut.AddConnection(
            "", "ConnectionString",
            connectionString: "Endpoint=sb://prod.servicebus.windows.net/",
            @namespace: null, tenantId: null, clientId: null,
            CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.AsError.Message.ShouldContain("Name is required.");
    }

    [Fact]
    public async Task AddConnection_ReturnsMcpError_WhenUnexpectedExceptionOccurs()
    {
        _mediator
            .Setup(m => m.Send(It.IsAny<Messentra.Features.Settings.Connections.CreateConnection.CreateConnectionCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB exploded"));

        var result = await _sut.AddConnection(
            "prod", "ConnectionString",
            connectionString: "Endpoint=sb://prod.servicebus.windows.net/",
            @namespace: null, tenantId: null, clientId: null,
            CancellationToken.None);

        result.IsError.ShouldBeTrue();
    }

    // UpdateConnection tests

    [Fact]
    public async Task UpdateConnection_ReturnsMcpError_WhenConnectionNotFound()
    {
        _helpers.Setup(h => h.ResolveConnection("unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Messentra.Domain.Connection?)null);

        var result = await _sut.UpdateConnection(
            "unknown", "new-name", "ConnectionString",
            connectionString: "Endpoint=sb://x.servicebus.windows.net/",
            @namespace: null, tenantId: null, clientId: null,
            CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.AsError.Message.ShouldContain("unknown");
    }

    [Fact]
    public async Task UpdateConnection_ReturnsMcpError_WhenConnectionTypeIsInvalid()
    {
        _helpers.Setup(h => h.ResolveConnection("prod", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeConnection(id: 1, name: "prod"));

        var result = await _sut.UpdateConnection(
            "prod", "prod", "INVALID_TYPE",
            connectionString: null, @namespace: null, tenantId: null, clientId: null,
            CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.AsError.Message.ShouldContain("INVALID_TYPE");
    }

    [Fact]
    public async Task UpdateConnection_SendsUpdateConnectionCommand_WithResolvedId()
    {
        _helpers.Setup(h => h.ResolveConnection("prod", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeConnection(id: 42, name: "prod"));
        _mediator
            .Setup(m => m.Send(It.IsAny<Messentra.Features.Settings.Connections.UpdateConnection.UpdateConnectionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mediator.Unit.Value);

        var result = await _sut.UpdateConnection(
            "prod", "prod-renamed", "ConnectionString",
            connectionString: "Endpoint=sb://prod.servicebus.windows.net/;SharedAccessKeyName=Root;SharedAccessKey=key",
            @namespace: null, tenantId: null, clientId: null,
            CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.AsSuccess.ShouldContain("prod");
        _mediator.Verify(m => m.Send(
            It.Is<Messentra.Features.Settings.Connections.UpdateConnection.UpdateConnectionCommand>(
                c => c.Id == 42 &&
                     c.Name == "prod-renamed" &&
                     c.ConnectionConfig.ConnectionType == Messentra.Domain.ConnectionType.ConnectionString &&
                     c.ConnectionConfig.ConnectionString == "Endpoint=sb://prod.servicebus.windows.net/;SharedAccessKeyName=Root;SharedAccessKey=key"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateConnection_SendsUpdateConnectionCommand_WithEntraId()
    {
        _helpers.Setup(h => h.ResolveConnection("prod", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeConnection(id: 7, name: "prod"));
        _mediator
            .Setup(m => m.Send(It.IsAny<Messentra.Features.Settings.Connections.UpdateConnection.UpdateConnectionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mediator.Unit.Value);

        var result = await _sut.UpdateConnection(
            "prod", "prod", "EntraId",
            connectionString: null,
            @namespace: "prod.servicebus.windows.net",
            tenantId: "tenant-123",
            clientId: "client-456",
            CancellationToken.None);

        result.IsError.ShouldBeFalse();
        _mediator.Verify(m => m.Send(
            It.Is<Messentra.Features.Settings.Connections.UpdateConnection.UpdateConnectionCommand>(
                c => c.Id == 7 &&
                     c.ConnectionConfig.ConnectionType == Messentra.Domain.ConnectionType.EntraId &&
                     c.ConnectionConfig.Namespace == "prod.servicebus.windows.net" &&
                     c.ConnectionConfig.TenantId == "tenant-123" &&
                     c.ConnectionConfig.ClientId == "client-456"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateConnection_ReturnsMcpError_WhenValidationFails()
    {
        _helpers.Setup(h => h.ResolveConnection("prod", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeConnection(id: 1, name: "prod"));
        _mediator
            .Setup(m => m.Send(It.IsAny<Messentra.Features.Settings.Connections.UpdateConnection.UpdateConnectionCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FluentValidation.ValidationException("Namespace is required when ConnectionType is EntraId."));

        var result = await _sut.UpdateConnection(
            "prod", "prod", "EntraId",
            connectionString: null, @namespace: null, tenantId: null, clientId: null,
            CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.AsError.Message.ShouldContain("Namespace is required");
    }

    [Fact]
    public async Task UpdateConnection_ReturnsMcpError_WhenUnexpectedExceptionOccurs()
    {
        _helpers.Setup(h => h.ResolveConnection("prod", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeConnection(id: 1, name: "prod"));
        _mediator
            .Setup(m => m.Send(It.IsAny<Messentra.Features.Settings.Connections.UpdateConnection.UpdateConnectionCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB exploded"));

        var result = await _sut.UpdateConnection(
            "prod", "prod", "ConnectionString",
            connectionString: "Endpoint=sb://prod.servicebus.windows.net/",
            @namespace: null, tenantId: null, clientId: null,
            CancellationToken.None);

        result.IsError.ShouldBeTrue();
    }

    // DeleteConnection tests

    [Fact]
    public async Task DeleteConnection_ReturnsMcpError_WhenConnectionNotFound()
    {
        _helpers.Setup(h => h.ResolveConnection("unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Messentra.Domain.Connection?)null);

        var result = await _sut.DeleteConnection("unknown", CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.AsError.Message.ShouldContain("unknown");
    }

    [Fact]
    public async Task DeleteConnection_SendsDeleteConnectionCommand_WithResolvedId()
    {
        _helpers.Setup(h => h.ResolveConnection("prod", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeConnection(id: 99, name: "prod"));
        _mediator
            .Setup(m => m.Send(It.IsAny<Messentra.Features.Settings.Connections.DeleteConnection.DeleteConnectionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mediator.Unit.Value);

        var result = await _sut.DeleteConnection("prod", CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.AsSuccess.ShouldContain("prod");
        _mediator.Verify(m => m.Send(
            It.Is<Messentra.Features.Settings.Connections.DeleteConnection.DeleteConnectionCommand>(
                c => c.Id == 99),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteConnection_ReturnsMcpError_WhenUnexpectedExceptionOccurs()
    {
        _helpers.Setup(h => h.ResolveConnection("prod", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeConnection(id: 1, name: "prod"));
        _mediator
            .Setup(m => m.Send(It.IsAny<Messentra.Features.Settings.Connections.DeleteConnection.DeleteConnectionCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB exploded"));

        var result = await _sut.DeleteConnection("prod", CancellationToken.None);

        result.IsError.ShouldBeTrue();
    }

    private static Messentra.Domain.Connection MakeConnection(long id, string name) => new()
    {
        Id = id,
        Name = name,
        ConnectionConfig = Messentra.Domain.ConnectionConfig.CreateConnectionString(
            "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=Root;SharedAccessKey=test")
    };
}
