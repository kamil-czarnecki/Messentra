using AutoFixture;
using Messentra.Domain;
using Messentra.Features.Jobs.Stages;
using Messentra.Features.Jobs.Stages.FetchMessages;
using Messentra.Features.Jobs.Stages.PersistSelectedMessages;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Jobs.Stages.PersistSelectedMessages;

public sealed class PersistSelectedMessagesStageShould : InMemoryDbTestBase
{
    private readonly Fixture _fixture = new();

    [Fact]
    public async Task PersistAllMessagesAndMarkProgress100_WhenStartingFresh()
    {
        // Arrange
        var messages = CreateMessageDtos(5);
        var job = CreateJob(messages);
        var sut = new PersistSelectedMessagesStage<TestJob>(DbContext);

        // Act
        await sut.Run(job, TestContext.Current.CancellationToken);

        // Assert
        var batches = DbContext.Set<FetchedMessagesBatch>()
            .Where(x => x.JobId == job.Id)
            .ToList();
        batches.ShouldNotBeEmpty();
        batches.Sum(x => x.MessagesCount).ShouldBe(5);
        job.StageProgress.Progress.ShouldBe(100);
    }

    [Fact]
    public async Task SkipAlreadyPersistedMessagesOnResume_WhenSomeBatchesExist()
    {
        // Arrange
        var messages = CreateMessageDtos(5);
        var job = CreateJob(messages);

        await DbContext.Set<FetchedMessagesBatch>().AddAsync(new FetchedMessagesBatch
        {
            JobId = job.Id,
            Messages = messages.Take(2).ToList(),
            MessagesCount = 2,
            LastSequence = 1,
            CreatedOn = DateTime.UtcNow
        }, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new PersistSelectedMessagesStage<TestJob>(DbContext);

        // Act
        await sut.Run(job, TestContext.Current.CancellationToken);

        // Assert
        var totalPersisted = DbContext.Set<FetchedMessagesBatch>()
            .Where(x => x.JobId == job.Id)
            .Sum(x => x.MessagesCount);
        totalPersisted.ShouldBe(5);
        job.StageProgress.Progress.ShouldBe(100);
    }

    [Fact]
    public async Task CompleteImmediately_WhenAllMessagesAlreadyPersisted()
    {
        // Arrange
        var messages = CreateMessageDtos(3);
        var job = CreateJob(messages);

        await DbContext.Set<FetchedMessagesBatch>().AddAsync(new FetchedMessagesBatch
        {
            JobId = job.Id,
            Messages = messages,
            MessagesCount = 3,
            LastSequence = 2,
            CreatedOn = DateTime.UtcNow
        }, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new PersistSelectedMessagesStage<TestJob>(DbContext);

        // Act
        await sut.Run(job, TestContext.Current.CancellationToken);

        // Assert
        var batchCount = DbContext.Set<FetchedMessagesBatch>().Count(x => x.JobId == job.Id);
        batchCount.ShouldBe(1);
        job.StageProgress.Progress.ShouldBe(100);
    }

    [Fact]
    public async Task MarkProgressAs100WithoutPersistingBatch_WhenMessageListIsEmpty()
    {
        // Arrange
        var job = CreateJob([]);
        var sut = new PersistSelectedMessagesStage<TestJob>(DbContext);

        // Act
        await sut.Run(job, TestContext.Current.CancellationToken);

        // Assert
        var batchCount = DbContext.Set<FetchedMessagesBatch>().Count(x => x.JobId == job.Id);
        batchCount.ShouldBe(0);
        job.StageProgress.Progress.ShouldBe(100);
    }

    [Fact]
    public async Task PersistMultipleBatches_WhenMessageCountExceedsBatchSize()
    {
        // Arrange
        var messages = CreateMessageDtos(1500);
        var job = CreateJob(messages);
        var sut = new PersistSelectedMessagesStage<TestJob>(DbContext);

        // Act
        await sut.Run(job, TestContext.Current.CancellationToken);

        // Assert
        var batches = DbContext.Set<FetchedMessagesBatch>()
            .Where(x => x.JobId == job.Id)
            .OrderBy(x => x.Id)
            .ToList();
        batches.Count.ShouldBe(2);
        batches[0].MessagesCount.ShouldBe(1000);
        batches[1].MessagesCount.ShouldBe(500);
        job.StageProgress.Progress.ShouldBe(100);
    }

    private TestJob CreateJob(IReadOnlyList<ServiceBusMessageDto> messages) =>
        new(messages)
        {
            Id = _fixture.Create<long>(),
            Label = _fixture.Create<string>(),
            CreatedAt = DateTime.UtcNow
        };

    private static List<ServiceBusMessageDto> CreateMessageDtos(int count) =>
        Enumerable.Range(0, count)
            .Select(i => new ServiceBusMessageDto(
                $"body-{i}",
                new ServiceBusProperties(null, null, null, $"msg-{i}", null, null, null, null, null, null, null, null, null),
                new Dictionary<string, object>()))
            .ToList();

    private sealed class TestJob(IReadOnlyList<ServiceBusMessageDto> messages) : Job, IHasSelectedMessages
    {
        public override IReadOnlyList<Type> Stages { get; } = [];
        public IReadOnlyList<ServiceBusMessageDto> GetSelectedMessages() => messages;
    }
}
