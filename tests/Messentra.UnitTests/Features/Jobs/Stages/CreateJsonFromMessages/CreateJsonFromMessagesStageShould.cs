using System.Text.Json;
using System.Collections.Concurrent;
using AutoFixture;
using Messentra.Domain;
using Messentra.Features.Jobs.Stages;
using Messentra.Features.Jobs.Stages.CreateJsonFromMessages;
using Messentra.Features.Jobs.Stages.FetchMessages;
using Messentra.Infrastructure;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Jobs.Stages.CreateJsonFromMessages;

public sealed class CreateJsonFromMessagesStageShould : InMemoryDbTestBase
{
    private readonly Fixture _fixture = new();
    private readonly Mock<IFileSystem> _fileSystem = new();

    [Fact]
    public async Task WriteEmptyJsonArrayAndComplete_WhenNoFetchedMessagesExist()
    {
        // Arrange
        var progressValues = new ConcurrentQueue<int>();
        var job = CreateJob(id: _fixture.Create<long>(), label: "export-empty", progressValues);
        var rootPath = "/tmp/messentra-tests";
        var expectedPath = Path.Combine(rootPath, "Jobs", job.Id.ToString(), $"{job.Label}.json");
        var outputStream = new NonDisposingMemoryStream();

        _fileSystem.Setup(x => x.GetRootPath()).Returns(rootPath);
        _fileSystem
            .Setup(x => x.OpenWrite(expectedPath, 65536, true))
            .Returns(outputStream);

        var sut = new CreateJsonFromMessagesStage<TestJob>(DbContext, _fileSystem.Object);

        // Act
        await sut.Run(job, TestContext.Current.CancellationToken);

        // Assert
        job.CompletedResult.ShouldNotBeNull();
        job.CompletedResult!.FilePath.ShouldBe(expectedPath);
        job.StageProgress.Stage.ShouldBe("Creating JSON");
        job.StageProgress.Progress.ShouldBe(100);
        await WaitForProgressCountAsync(progressValues, 2, TestContext.Current.CancellationToken);
        progressValues.ToArray().ShouldBe([0, 100]);

        outputStream.Position = 0;
        using var document = await JsonDocument.ParseAsync(outputStream, cancellationToken: TestContext.Current.CancellationToken);
        document.RootElement.ValueKind.ShouldBe(JsonValueKind.Array);
        document.RootElement.GetArrayLength().ShouldBe(0);

        _fileSystem.Verify(x => x.GetRootPath(), Times.Once);
        _fileSystem.Verify(x => x.OpenWrite(expectedPath, 65536, true), Times.Once);
    }

    [Fact]
    public async Task WriteMessagesInBatchOrderAndUpdateProgress_WhenFetchedMessagesExist()
    {
        // Arrange
        var progressValues = new ConcurrentQueue<int>();
        var job = CreateJob(id: _fixture.Create<long>(), label: "export-messages", progressValues);
        var rootPath = "/tmp/messentra-tests";
        var expectedPath = Path.Combine(rootPath, "Jobs", job.Id.ToString(), $"{job.Label}.json");
        var outputStream = new NonDisposingMemoryStream();

        await DbContext.Set<FetchedMessagesBatch>().AddRangeAsync(
        [
            new FetchedMessagesBatch
            {
                JobId = job.Id,
                Messages =
                [
                    CreateServiceBusMessage("msg-1", "body-1"),
                    CreateServiceBusMessage("msg-2", "body-2")
                ],
                MessagesCount = 2,
                LastSequence = 2,
                CreatedOn = DateTime.UtcNow
            },
            new FetchedMessagesBatch
            {
                JobId = job.Id,
                Messages =
                [
                    CreateServiceBusMessage("msg-3", "body-3")
                ],
                MessagesCount = 1,
                LastSequence = 3,
                CreatedOn = DateTime.UtcNow.AddSeconds(1)
            }
        ], TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _fileSystem.Setup(x => x.GetRootPath()).Returns(rootPath);
        _fileSystem
            .Setup(x => x.OpenWrite(expectedPath, 65536, true))
            .Returns(outputStream);

        var sut = new CreateJsonFromMessagesStage<TestJob>(DbContext, _fileSystem.Object);

        // Act
        await sut.Run(job, TestContext.Current.CancellationToken);

        // Assert
        job.CompletedResult.ShouldNotBeNull();
        job.CompletedResult!.FilePath.ShouldBe(expectedPath);
        job.StageProgress.Stage.ShouldBe("Creating JSON");
        job.StageProgress.Progress.ShouldBe(100);
        await WaitForProgressCountAsync(progressValues, 3, TestContext.Current.CancellationToken);
        progressValues.ToArray().ShouldBe([0, 66, 100]);

        outputStream.Position = 0;
        using var document = await JsonDocument.ParseAsync(outputStream, cancellationToken: TestContext.Current.CancellationToken);
        var items = document.RootElement.EnumerateArray().ToArray();
        items.Length.ShouldBe(3);
        items[0].GetProperty("message").GetString().ShouldBe("body-1");
        items[0].GetProperty("properties").GetProperty("messageId").GetString().ShouldBe("msg-1");
        items[1].GetProperty("message").GetString().ShouldBe("body-2");
        items[1].GetProperty("properties").GetProperty("messageId").GetString().ShouldBe("msg-2");
        items[2].GetProperty("message").GetString().ShouldBe("body-3");
        items[2].GetProperty("properties").GetProperty("messageId").GetString().ShouldBe("msg-3");

        _fileSystem.Verify(x => x.GetRootPath(), Times.Once);
        _fileSystem.Verify(x => x.OpenWrite(expectedPath, 65536, true), Times.Once);
    }

    private TestJob CreateJob(long id, string label, ConcurrentQueue<int> progressValues)
    {
        var job = new TestJob
        {
            Id = id,
            Label = label,
            CreatedAt = DateTime.UtcNow
        };

        job.Subscribe(update =>
        {
            if (update.StageProgress is not null)
            {
                progressValues.Enqueue(update.StageProgress.Progress);
            }
        });

        return job;
    }

    private static async Task WaitForProgressCountAsync(ConcurrentQueue<int> progressValues, int expectedCount, CancellationToken cancellationToken)
    {
        var start = DateTime.UtcNow;

        while (progressValues.Count < expectedCount && DateTime.UtcNow - start < TimeSpan.FromSeconds(1))
        {
            await Task.Delay(10, cancellationToken);
        }
    }

    private static ServiceBusMessageDto CreateServiceBusMessage(string messageId, string body) =>
        new(
            Message: body,
            Properties: new ServiceBusProperties(
                ContentType: "text/plain",
                CorrelationId: null,
                Subject: null,
                MessageId: messageId,
                To: null,
                ReplyTo: null,
                TimeToLive: TimeSpan.FromMinutes(5),
                ReplyToSessionId: null,
                SessionId: null,
                PartitionKey: null,
                ScheduledEnqueueTime: null,
                TransactionPartitionKey: null),
            ApplicationProperties: new Dictionary<string, object>());

    private sealed class TestJob : Job, IStageCompletionHandler<CreateJsonStageResult>
    {
        public override IReadOnlyList<Type> Stages { get; } = [];

        public CreateJsonStageResult? CompletedResult { get; private set; }

        public void StageCompleted(CreateJsonStageResult result)
        {
            CompletedResult = result;
        }
    }

    private sealed class NonDisposingMemoryStream : MemoryStream
    {
        protected override void Dispose(bool disposing)
        {
            // Keep stream available for assertions after stage disposal.
        }
    }
}
