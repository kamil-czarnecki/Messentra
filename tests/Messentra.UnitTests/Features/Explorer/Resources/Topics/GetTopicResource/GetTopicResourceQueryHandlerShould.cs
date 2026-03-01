using Azure.Messaging.ServiceBus;
using Messentra.Domain;
using Messentra.Features.Explorer.Resources.Topics;
using Messentra.Features.Explorer.Resources.Topics.GetTopicResource;
using Messentra.Infrastructure.AzureServiceBus;
using Messentra.Infrastructure.AzureServiceBus.Topics;
using Moq;
using Shouldly;
using Xunit;
using ConnectionInfo = Messentra.Infrastructure.AzureServiceBus.ConnectionInfo;

namespace Messentra.UnitTests.Features.Explorer.Resources.Topics.GetTopicResource;

public sealed class GetTopicResourceQueryHandlerShould
{
    private readonly Mock<IAzureServiceBusTopicProvider> _providerMock = new();
    private readonly GetTopicResourceQueryHandler _sut;

    public GetTopicResourceQueryHandlerShould()
    {
        _sut = new GetTopicResourceQueryHandler(_providerMock.Object);
    }

    [Fact]
    public async Task ReturnTopicWhenFound_WithConnectionString()
    {
        // Arrange
        var connectionConfig = ConnectionConfig.CreateConnectionString("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key");
        var query = new GetTopicResourceQuery("test-topic", connectionConfig);
        var expectedTopic = CreateTopic("test-topic");

        _providerMock
            .Setup(x => x.GetByName(
                It.Is<ConnectionInfo.ConnectionString>(c => c.Value == connectionConfig.ConnectionStringConfig!.ConnectionString),
                "test-topic",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTopic);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsT0.ShouldBeTrue();
        result.AsT0.ShouldBe(expectedTopic);
    }

    [Fact]
    public async Task ReturnTopicWhenFound_WithEntraId()
    {
        // Arrange
        var connectionConfig = ConnectionConfig.CreateEntraId("test.servicebus.windows.net", "tenant-id", "client-id");
        var query = new GetTopicResourceQuery("test-topic", connectionConfig);
        var expectedTopic = CreateTopic("test-topic");

        _providerMock
            .Setup(x => x.GetByName(
                It.Is<ConnectionInfo.ManagedIdentity>(c =>
                    c.FullyQualifiedNamespace == connectionConfig.EntraIdConfig!.Namespace &&
                    c.TenantId == connectionConfig.EntraIdConfig.TenantId &&
                    c.ClientId == connectionConfig.EntraIdConfig.ClientId),
                "test-topic",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTopic);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsT0.ShouldBeTrue();
        result.AsT0.ShouldBe(expectedTopic);
    }

    [Fact]
    public async Task ReturnTopicNotFoundWhenEntityDoesNotExist()
    {
        // Arrange
        var connectionConfig = ConnectionConfig.CreateConnectionString("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key");
        var query = new GetTopicResourceQuery("missing-topic", connectionConfig);

        _providerMock
            .Setup(x => x.GetByName(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceBusException("Entity not found", ServiceBusFailureReason.MessagingEntityNotFound));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsT1.ShouldBeTrue();
        result.AsT1.ShouldBeOfType<TopicNotFound>();
    }

    [Fact]
    public async Task RethrowServiceBusExceptionForOtherReasons()
    {
        // Arrange
        var connectionConfig = ConnectionConfig.CreateConnectionString("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key");
        var query = new GetTopicResourceQuery("test-topic", connectionConfig);

        _providerMock
            .Setup(x => x.GetByName(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceBusException("Service communication problem", ServiceBusFailureReason.ServiceCommunicationProblem));

        // Act & Assert
        await Should.ThrowAsync<ServiceBusException>(() => _sut.Handle(query, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task ThrowInvalidOperationExceptionForUnsupportedConnectionType()
    {
        // Arrange
        var connectionConfig = new ConnectionConfig((ConnectionType)999, null, null);
        var query = new GetTopicResourceQuery("test-topic", connectionConfig);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => _sut.Handle(query, CancellationToken.None).AsTask());
    }

    private static Resource.Topic CreateTopic(string name) =>
        new(
            name,
            $"https://test.servicebus.windows.net/{name}",
            new ResourceOverview("Active", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                new MessageInfo(0, 0, 0, 0, 0, 0),
                new SizeInfo(0, 1024)),
            new TopicProperties(
                TimeSpan.FromDays(14),
                TimeSpan.MaxValue,
                false,
                false,
                TimeSpan.FromMinutes(1),
                256,
                string.Empty),
            []);
}

