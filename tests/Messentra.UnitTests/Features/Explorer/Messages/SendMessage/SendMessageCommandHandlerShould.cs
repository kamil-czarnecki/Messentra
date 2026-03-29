using Messentra.Domain;
using Messentra.Features.Explorer.Messages.SendMessage;
using Messentra.Features.Explorer.Resources;
using Messentra.Infrastructure.AzureServiceBus;
using Moq;
using Shouldly;
using Xunit;
using ConnectionInfo = Messentra.Infrastructure.AzureServiceBus.ConnectionInfo;

namespace Messentra.UnitTests.Features.Explorer.Messages.SendMessage;

public sealed class SendMessageCommandHandlerShould
{
    private readonly Mock<IAzureServiceBusSender> _senderMock = new();
    private readonly SendMessageCommandHandler _sut;

    private static readonly ConnectionConfig ConnectionStringConfig =
        ConnectionConfig.CreateConnectionString(
            "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key");

    private static readonly ConnectionConfig EntraIdConfig =
        ConnectionConfig.CreateEntraId("test.servicebus.windows.net", "tenant-id", "client-id");

    public SendMessageCommandHandlerShould()
    {
        _sut = new SendMessageCommandHandler(_senderMock.Object);
    }


    [Fact]
    public async Task ReturnSuccess_WhenSenderCompletes()
    {
        // Arrange
        var command = BuildCommand(new QueueTreeNode("conn", CreateQueue("q"), ConnectionStringConfig));

        _senderMock
            .Setup(x => x.Send(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), command, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsT0.ShouldBeTrue();
    }

    [Fact]
    public async Task ReturnSendMessageError_WhenSenderThrows()
    {
        // Arrange
        var command = BuildCommand(new QueueTreeNode("conn", CreateQueue("q"), ConnectionStringConfig));

        _senderMock
            .Setup(x => x.Send(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("send failed"));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsT1.ShouldBeTrue();
        result.AsT1.Message.ShouldBe("send failed");
    }


    [Fact]
    public async Task UseQueueName_AsEntityPath_ForQueueNode()
    {
        // Arrange
        var command = BuildCommand(new QueueTreeNode("conn", CreateQueue("my-queue"), ConnectionStringConfig));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _senderMock.Verify(
            x => x.Send(It.IsAny<ConnectionInfo>(), "my-queue", command, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UseTopicName_AsEntityPath_ForTopicNode()
    {
        // Arrange
        var command = BuildCommand(new TopicTreeNode("conn", CreateTopic("my-topic"), ConnectionStringConfig));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _senderMock.Verify(
            x => x.Send(It.IsAny<ConnectionInfo>(), "my-topic", command, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UseTopicName_AsEntityPath_ForSubscriptionNode()
    {
        // Arrange
        var command = BuildCommand(new SubscriptionTreeNode("conn", CreateSubscription("my-sub", "my-topic"), ConnectionStringConfig));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _senderMock.Verify(
            x => x.Send(It.IsAny<ConnectionInfo>(), "my-topic", command, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReturnSendMessageError_ForUnsupportedNodeType()
    {
        // Arrange
        var command = BuildCommand(new NamespaceTreeNode("conn", ConnectionStringConfig));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsT1.ShouldBeTrue();
        result.AsT1.Message.ShouldContain("NamespaceTreeNode");
    }


    [Fact]
    public async Task UseConnectionStringInfo_ForConnectionStringConfig()
    {
        // Arrange
        var command = BuildCommand(new QueueTreeNode("conn", CreateQueue("q"), ConnectionStringConfig));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _senderMock.Verify(
            x => x.Send(
                It.Is<ConnectionInfo.ConnectionString>(c =>
                    c.Value == ConnectionStringConfig.ConnectionStringConfig!.ConnectionString),
                It.IsAny<string>(), command, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UseManagedIdentityInfo_ForEntraIdConfig()
    {
        // Arrange
        var command = BuildCommand(new QueueTreeNode("conn", CreateQueue("q"), EntraIdConfig));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _senderMock.Verify(
            x => x.Send(
                It.Is<ConnectionInfo.ManagedIdentity>(c =>
                    c.FullyQualifiedNamespace == EntraIdConfig.EntraIdConfig!.Namespace &&
                    c.TenantId == EntraIdConfig.EntraIdConfig.TenantId &&
                    c.ClientId == EntraIdConfig.EntraIdConfig.ClientId),
                It.IsAny<string>(), command, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReturnSendMessageError_ForUnsupportedConnectionType()
    {
        // Arrange
        var unsupportedConfig = new ConnectionConfig((ConnectionType)999, null, null);
        var command = BuildCommand(new QueueTreeNode("conn", CreateQueue("q"), unsupportedConfig));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsT1.ShouldBeTrue();
        result.AsT1.Message.ShouldContain("999");
    }


    private static SendMessageCommand BuildCommand(ResourceTreeNode node, string body = "hello") =>
        new(
            ResourceTreeNode: node,
            Body: body,
            MessageId: null,
            Label: null,
            CorrelationId: null,
            SessionId: null,
            ReplyToSessionId: null,
            PartitionKey: null,
            ScheduledEnqueueTimeUtc: null,
            TimeToLive: null,
            To: null,
            ReplyTo: null,
            ContentType: null,
            ApplicationProperties: new Dictionary<string, object>());

    private static Resource.Queue CreateQueue(string name) =>
        new(
            name,
            $"https://test.servicebus.windows.net/{name}",
            new ResourceOverview("Active", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                new MessageInfo(0, 0, 0, 0, 0, 0),
                new SizeInfo(0, 1024)),
            new QueueProperties(
                TimeSpan.FromDays(14), TimeSpan.FromSeconds(60), TimeSpan.MaxValue,
                10, false, null, null, false, false, TimeSpan.FromMinutes(1), false, 256, string.Empty));

    private static Resource.Topic CreateTopic(string name) =>
        new(
            name,
            $"https://test.servicebus.windows.net/{name}",
            new ResourceOverview("Active", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                new MessageInfo(0, 0, 0, 0, 0, 0),
                new SizeInfo(0, 1024)),
            new TopicProperties(
                TimeSpan.FromDays(14), TimeSpan.MaxValue, false, false, TimeSpan.FromMinutes(1), 256, string.Empty),
            []);

    private static Resource.Subscription CreateSubscription(string name, string topicName) =>
        new(
            name,
            topicName,
            $"https://test.servicebus.windows.net/{topicName}/subscriptions/{name}",
            new ResourceOverview("Active", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                new MessageInfo(0, 0, 0, 0, 0, 0),
                new SizeInfo(0, 1024)),
            new SubscriptionProperties(
                TimeSpan.FromDays(14), TimeSpan.FromSeconds(60), TimeSpan.MaxValue,
                10, false, null, null, false, string.Empty));
}

