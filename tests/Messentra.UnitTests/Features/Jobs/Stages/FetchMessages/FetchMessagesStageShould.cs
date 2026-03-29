using AutoFixture;
using Messentra.Domain;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Jobs;
using Messentra.Features.Jobs.Stages.FetchMessages;
using Messentra.Infrastructure.AzureServiceBus;
using Messentra.Infrastructure.AzureServiceBus.Queues;
using Messentra.Infrastructure.AzureServiceBus.Subscriptions;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Jobs.Stages.FetchMessages;

public sealed class FetchMessagesStageShould : InMemoryDbTestBase
{
    private readonly Fixture _fixture = new();
    private readonly Mock<IAzureServiceBusQueueMessagesProvider> _queueProvider = new();
    private readonly Mock<IAzureServiceBusSubscriptionMessagesProvider> _subscriptionProvider = new();

    [Fact]
    public async Task ResumeFromLastPersistedSequence_WhenPreviousBatchExists()
    {
        // Arrange
        var job = CreateJob(new ResourceTarget.Queue("queue-a", SubQueue.Active), totalToFetch: 5000);
        await DbContext.Set<FetchedMessagesBatch>().AddAsync(new FetchedMessagesBatch
        {
            JobId = job.Id,
            Messages = [],
            MessagesCount = 100,
            LastSequence = 25,
            CreatedOn = DateTime.UtcNow
        }, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _queueProvider
            .Setup(x => x.Get(
                It.IsAny<ConnectionInfo>(),
                It.IsAny<string>(),
                It.IsAny<FetchMessagesOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var sut = new FetchMessagesStage<TestJob>(DbContext, _queueProvider.Object, _subscriptionProvider.Object);

        // Act
        await sut.Run(job, TestContext.Current.CancellationToken);

        // Assert
        _queueProvider.Verify(x => x.Get(
            It.IsAny<ConnectionInfo>(),
            "queue-a",
            It.Is<FetchMessagesOptions>(o => o.StartSequence == 26),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PersistOneFetchedMessagesBatchPerProviderBatch_WhenMessagesArriveInChunks()
    {
        // Arrange
        var job = CreateJob(new ResourceTarget.Queue("queue-a", SubQueue.Active), totalToFetch: 1200);
        var requestedBatchSizes = new List<int>();
        var callCount = 0;

        _queueProvider
            .Setup(x => x.Get(
                It.IsAny<ConnectionInfo>(),
                It.IsAny<string>(),
                It.IsAny<FetchMessagesOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<ConnectionInfo, string, FetchMessagesOptions, CancellationToken>((_, _, options, _) => requestedBatchSizes.Add(options.MessageCount))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1
                    ? CreateMessages(1, 1000)
                    : CreateMessages(1001, 200);
            });

        var sut = new FetchMessagesStage<TestJob>(DbContext, _queueProvider.Object, _subscriptionProvider.Object);

        // Act
        await sut.Run(job, TestContext.Current.CancellationToken);

        // Assert
        var batches = DbContext.Set<FetchedMessagesBatch>()
            .Where(x => x.JobId == job.Id)
            .OrderBy(x => x.Id)
            .ToList();
        batches.Count.ShouldBe(2);
        batches[0].MessagesCount.ShouldBe(1000);
        batches[0].LastSequence.ShouldBe(1000);
        batches[1].MessagesCount.ShouldBe(200);
        batches[1].LastSequence.ShouldBe(1200);
        requestedBatchSizes.ShouldBe([1000, 200]);
        job.StageProgress.Progress.ShouldBe(100);
    }

    [Fact]
    public async Task RequestOnlyRemainingMessagesAndStop_WhenRequestedTotalIsBelowBatchSize()
    {
        // Arrange
        var job = CreateJob(new ResourceTarget.Queue("queue-a", SubQueue.Active), totalToFetch: 250);
        var requestedBatchSizes = new List<int>();

        _queueProvider
            .Setup(x => x.Get(
                It.IsAny<ConnectionInfo>(),
                It.IsAny<string>(),
                It.IsAny<FetchMessagesOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<ConnectionInfo, string, FetchMessagesOptions, CancellationToken>((_, _, options, _) => requestedBatchSizes.Add(options.MessageCount))
            .ReturnsAsync(CreateMessages(1, 250));

        var sut = new FetchMessagesStage<TestJob>(DbContext, _queueProvider.Object, _subscriptionProvider.Object);

        // Act
        await sut.Run(job, TestContext.Current.CancellationToken);

        // Assert
        var batches = DbContext.Set<FetchedMessagesBatch>()
            .Where(x => x.JobId == job.Id)
            .ToList();
        batches.Count.ShouldBe(1);
        batches[0].MessagesCount.ShouldBe(250);
        requestedBatchSizes.ShouldBe([250]);
        _queueProvider.Verify(x => x.Get(
            It.IsAny<ConnectionInfo>(),
            It.IsAny<string>(),
            It.IsAny<FetchMessagesOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
        job.StageProgress.Progress.ShouldBe(100);
    }

    [Fact]
    public async Task UseSubscriptionProviderAndNotQueueProvider_WhenTargetIsTopicSubscription()
    {
        // Arrange
        var job = CreateJob(new ResourceTarget.TopicSubscription("topic-a", "sub-a", SubQueue.Active), totalToFetch: 100);

        _subscriptionProvider
            .Setup(x => x.Get(
                It.IsAny<ConnectionInfo>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<FetchMessagesOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var sut = new FetchMessagesStage<TestJob>(DbContext, _queueProvider.Object, _subscriptionProvider.Object);

        // Act
        await sut.Run(job, TestContext.Current.CancellationToken);

        // Assert
        _subscriptionProvider.Verify(x => x.Get(
            It.IsAny<ConnectionInfo>(),
            "topic-a",
            "sub-a",
            It.IsAny<FetchMessagesOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _queueProvider.Verify(x => x.Get(
            It.IsAny<ConnectionInfo>(),
            It.IsAny<string>(),
            It.IsAny<FetchMessagesOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarkProgressAs100AndNotPersistBatch_WhenNoMessagesReturned()
    {
        // Arrange
        var job = CreateJob(new ResourceTarget.Queue("queue-a", SubQueue.Active), totalToFetch: 500);

        _queueProvider
            .Setup(x => x.Get(
                It.IsAny<ConnectionInfo>(),
                It.IsAny<string>(),
                It.IsAny<FetchMessagesOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var sut = new FetchMessagesStage<TestJob>(DbContext, _queueProvider.Object, _subscriptionProvider.Object);

        // Act
        await sut.Run(job, TestContext.Current.CancellationToken);

        // Assert
        var persistedCount = DbContext.Set<FetchedMessagesBatch>().Count(x => x.JobId == job.Id);
        persistedCount.ShouldBe(0);
        job.StageProgress.Progress.ShouldBe(100);
    }

    private TestJob CreateJob(ResourceTarget target, long totalToFetch)
    {
        var connectionConfig = new ConnectionConfig(
            ConnectionType.ConnectionString,
            new ConnectionStringConfig(_fixture.Create<string>()),
            null);

        return new TestJob(new MessageFetchConfiguration(connectionConfig, target, totalToFetch))
        {
            Id = _fixture.Create<long>(),
            Label = _fixture.Create<string>(),
            CreatedAt = DateTime.UtcNow
        };
    }

    private static IReadOnlyCollection<ServiceBusMessage> CreateMessages(long startingSequence, int count)
    {
        return Enumerable.Range(0, count)
            .Select(i =>
            {
                var sequenceNumber = startingSequence + i;
                var message = new MessageDto(
                    Body: "{}",
                    BrokerProperties: new BrokerProperties(
                        MessageId: $"msg-{sequenceNumber}",
                        SequenceNumber: sequenceNumber,
                        CorrelationId: null,
                        SessionId: null,
                        ReplyToSessionId: null,
                        EnqueuedTimeUtc: DateTime.UtcNow,
                        ScheduledEnqueueTimeUtc: DateTime.UtcNow,
                        TimeToLive: TimeSpan.FromMinutes(5),
                        LockedUntilUtc: DateTime.UtcNow,
                        ExpiresAtUtc: DateTime.UtcNow.AddHours(1),
                        DeliveryCount: 1,
                        Label: null,
                        To: null,
                        ReplyTo: null,
                        PartitionKey: null,
                        ContentType: "application/json",
                        DeadLetterReason: null,
                        DeadLetterErrorDescription: null),
                    ApplicationProperties: new Dictionary<string, object>());

                return new ServiceBusMessage(message, new TestServiceBusMessageContext());
            })
            .ToList();
    }

    private sealed class TestJob(MessageFetchConfiguration configuration) : Job, IHasMessageFetchConfiguration
    {
        public override IReadOnlyList<Type> Stages { get; } = [];

        public MessageFetchConfiguration GetMessageFetchConfiguration() => configuration;
    }

    private sealed class TestServiceBusMessageContext : IServiceBusMessageContext
    {
        public Task Complete(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task Abandon(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task DeadLetter(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task Resend(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
