using Messentra.Domain;
using Messentra.Features.Explorer.Resources.Topics.GetAllTopicResources;
using Messentra.Infrastructure.AzureServiceBus;
using Messentra.Infrastructure.AzureServiceBus.Topics;
using Moq;
using Shouldly;
using Xunit;
using ConnectionInfo = Messentra.Infrastructure.AzureServiceBus.ConnectionInfo;

namespace Messentra.UnitTests.Features.Explorer.Resources.Topics.GetAllTopicResources;

public sealed class GetAllTopicResourcesQueryHandlerShould
{
    private readonly Mock<IAzureServiceBusTopicProvider> _providerMock = new();
    private readonly GetAllTopicResourcesQueryHandler _sut;

    public GetAllTopicResourcesQueryHandlerShould()
    {
        _sut = new GetAllTopicResourcesQueryHandler(_providerMock.Object);
    }

    [Fact]
    public async Task ReturnAllTopics_WithConnectionString()
    {
        // Arrange
        var connectionConfig = ConnectionConfig.CreateConnectionString("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key");
        var query = new GetAllTopicResourcesQuery(connectionConfig);
        var expectedTopics = new[] { CreateTopic("topic-1"), CreateTopic("topic-2") };

        _providerMock
            .Setup(x => x.GetAll(
                It.Is<ConnectionInfo.ConnectionString>(c => c.Value == connectionConfig.ConnectionStringConfig!.ConnectionString),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTopics);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldBe(expectedTopics);
    }

    [Fact]
    public async Task ReturnAllTopics_WithEntraId()
    {
        // Arrange
        var connectionConfig = ConnectionConfig.CreateEntraId("test.servicebus.windows.net", "tenant-id", "client-id");
        var query = new GetAllTopicResourcesQuery(connectionConfig);
        var expectedTopics = new[] { CreateTopic("topic-1") };

        _providerMock
            .Setup(x => x.GetAll(
                It.Is<ConnectionInfo.ManagedIdentity>(c =>
                    c.FullyQualifiedNamespace == connectionConfig.EntraIdConfig!.Namespace &&
                    c.TenantId == connectionConfig.EntraIdConfig.TenantId &&
                    c.ClientId == connectionConfig.EntraIdConfig.ClientId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTopics);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Count.ShouldBe(1);
        result.ShouldBe(expectedTopics);
    }

    [Fact]
    public async Task ReturnEmptyCollectionWhenNoTopicsExist()
    {
        // Arrange
        var connectionConfig = ConnectionConfig.CreateConnectionString("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key");
        var query = new GetAllTopicResourcesQuery(connectionConfig);

        _providerMock
            .Setup(x => x.GetAll(It.IsAny<ConnectionInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ThrowInvalidOperationExceptionForUnsupportedConnectionType()
    {
        // Arrange
        var connectionConfig = new ConnectionConfig((ConnectionType)999, null, null);
        var query = new GetAllTopicResourcesQuery(connectionConfig);

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
                265,
                string.Empty),
            []);
}

