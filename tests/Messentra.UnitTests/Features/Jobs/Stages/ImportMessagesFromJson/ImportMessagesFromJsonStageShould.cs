using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AutoFixture;
using Messentra.Domain;
using Messentra.Features.Jobs.Stages;
using Messentra.Features.Jobs.Stages.ImportMessages;
using Messentra.Features.Jobs.Stages.ImportMessagesFromJson;
using Messentra.Infrastructure;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Jobs.Stages.ImportMessagesFromJson;

public sealed class ImportMessagesFromJsonStageShould : InMemoryDbTestBase
{
    private readonly Fixture _fixture = new();
    private readonly Mock<IFileSystem> _fileSystem = new();

    [Fact]
    public async Task PersistImportedMessagesAndComplete_WhenJsonIsValid()
    {
        // Arrange
        var progressValues = new ConcurrentQueue<int>();
        var path = "/tmp/import.json";
        var json = """
                   [
                     {
                       "message": {"hello":"world"},
                       "properties": {
                         "contentType":"application/json",
                         "correlationId":null,
                         "subject":"subject-1",
                         "messageId":"msg-1",
                         "to":null,
                         "replyTo":null,
                         "timeToLive":"00:00:30",
                         "replyToSessionId":null,
                         "sessionId":null,
                         "partitionKey":null,
                         "scheduledEnqueueTime":null,
                         "transactionPartitionKey":null,
                         "enqueuedTimeUtc":null
                       },
                       "applicationProperties": {
                         "tenant":"alpha"
                       }
                     },
                     {
                       "message": "plain-text",
                       "properties": {
                         "contentType":"text/plain",
                         "correlationId":null,
                         "subject":"subject-2",
                         "messageId":"msg-2",
                         "to":null,
                         "replyTo":null,
                         "timeToLive":"00:01:00",
                         "replyToSessionId":null,
                         "sessionId":null,
                         "partitionKey":null,
                         "scheduledEnqueueTime":null,
                         "transactionPartitionKey":null,
                         "enqueuedTimeUtc":null
                       },
                       "applicationProperties": {}
                     }
                   ]
                   """;
        var job = CreateJob(_fixture.Create<long>(), path, json, progressValues);

        _fileSystem.Setup(x => x.FileExists(path)).Returns(true);
        _fileSystem.Setup(x => x.OpenRead(path)).Returns(() => new MemoryStream(Encoding.UTF8.GetBytes(json)));

        var sut = new PrepareMessagesFromJsonStage<TestJob>(DbContext, _fileSystem.Object);

        // Act
        await sut.Run(job, TestContext.Current.CancellationToken);

        // Assert
        var persisted = DbContext.Set<ImportedMessage>()
            .Where(x => x.JobId == job.Id)
            .OrderBy(x => x.Position)
            .ToList();

        persisted.Count.ShouldBe(2);
        persisted[0].IsSent.ShouldBeFalse();
        persisted[0].Message.Properties.MessageId.ShouldBe("msg-1");
        persisted[1].Message.Properties.MessageId.ShouldBe("msg-2");

        job.StageProgress.Stage.ShouldBe("Preparing messages");
        job.StageProgress.Progress.ShouldBe(100);
        progressValues.ToArray().ShouldContain(100);
    }

    [Fact]
    public async Task ResumeFromLastPersistedPosition_WhenMessagesAlreadyStored()
    {
        // Arrange
        var progressValues = new ConcurrentQueue<int>();
        var path = "/tmp/import-resume.json";
        var json = """
                   [
                     { "message": "body-1", "properties": { "messageId": "1" }, "applicationProperties": {} },
                     { "message": "body-2", "properties": { "messageId": "2" }, "applicationProperties": {} },
                     { "message": "body-3", "properties": { "messageId": "3" }, "applicationProperties": {} }
                   ]
                   """;
        var job = CreateJob(_fixture.Create<long>(), path, json, progressValues);

        await DbContext.Set<ImportedMessage>().AddAsync(new ImportedMessage
        {
            JobId = job.Id,
            Position = 1,
            Message = CreateMessage("1", "body-1"),
            IsSent = true,
            CreatedOn = DateTime.UtcNow,
            SentOn = DateTime.UtcNow
        }, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _fileSystem.Setup(x => x.FileExists(path)).Returns(true);
        _fileSystem.Setup(x => x.OpenRead(path)).Returns(() => new MemoryStream(Encoding.UTF8.GetBytes(json)));

        var sut = new PrepareMessagesFromJsonStage<TestJob>(DbContext, _fileSystem.Object);

        // Act
        await sut.Run(job, TestContext.Current.CancellationToken);

        // Assert
        var persisted = DbContext.Set<ImportedMessage>()
            .Where(x => x.JobId == job.Id)
            .OrderBy(x => x.Position)
            .ToList();

        persisted.Count.ShouldBe(3);
        persisted[0].IsSent.ShouldBeTrue();
        persisted[1].IsSent.ShouldBeFalse();
        persisted[2].IsSent.ShouldBeFalse();
    }

    [Fact]
    public async Task ThrowAndStop_WhenFirstMessageIsInvalid()
    {
        // Arrange
        var path = "/tmp/import-invalid.json";
        var invalidJson = """
                          [
                            {
                              "message": "ok",
                              "properties": "not-an-object",
                              "applicationProperties": {}
                            }
                          ]
                          """;
        var job = CreateJob(_fixture.Create<long>(), path, invalidJson, new ConcurrentQueue<int>());

        _fileSystem.Setup(x => x.FileExists(path)).Returns(true);
        _fileSystem.Setup(x => x.OpenRead(path)).Returns(() => new MemoryStream(Encoding.UTF8.GetBytes(invalidJson)));

        var sut = new PrepareMessagesFromJsonStage<TestJob>(DbContext, _fileSystem.Object);

        // Act
        var action = () => sut.Run(job, TestContext.Current.CancellationToken);

        // Assert
        await action.ShouldThrowAsync<JsonException>();
        DbContext.Set<ImportedMessage>().Count(x => x.JobId == job.Id).ShouldBe(0);
    }

    [Fact]
    public async Task ThrowInvalidOperationException_WhenFileHashDoesNotMatchQueuedHash()
    {
        // Arrange
        var path = "/tmp/import-hash.json";
        var json = """
                   [
                     { "message": "body-1", "properties": { "messageId": "1" }, "applicationProperties": {} }
                   ]
                   """;
        var job = new TestJob
        {
            Id = _fixture.Create<long>(),
            Label = "import-job",
            CreatedAt = DateTime.UtcNow,
            Source = new ImportMessagesFile(path, "WRONG_HASH")
        };

        _fileSystem.Setup(x => x.FileExists(path)).Returns(true);
        _fileSystem.Setup(x => x.OpenRead(path)).Returns(() => new MemoryStream(Encoding.UTF8.GetBytes(json)));

        var sut = new PrepareMessagesFromJsonStage<TestJob>(DbContext, _fileSystem.Object);

        // Act
        var action = () => sut.Run(job, TestContext.Current.CancellationToken);

        // Assert
        await action.ShouldThrowAsync<InvalidOperationException>();
    }

    private TestJob CreateJob(long id, string sourcePath, string sourceContent, ConcurrentQueue<int> progressValues)
    {
        var sourceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sourceContent)));
        var job = new TestJob
        {
            Id = id,
            Label = "import-job",
            CreatedAt = DateTime.UtcNow,
            Source = new ImportMessagesFile(sourcePath, sourceHash)
        };

        job.Subscribe(update =>
        {
            if (update.StageProgress is not null)
                progressValues.Enqueue(update.StageProgress.Progress);
        });

        return job;
    }

    private static ServiceBusMessageDto CreateMessage(string messageId, string body) =>
        new(
            body,
            new ServiceBusProperties(
                ContentType: "text/plain",
                CorrelationId: null,
                Subject: null,
                MessageId: messageId,
                To: null,
                ReplyTo: null,
                TimeToLive: null,
                ReplyToSessionId: null,
                SessionId: null,
                PartitionKey: null,
                ScheduledEnqueueTime: null,
                TransactionPartitionKey: null,
                EnqueuedTimeUtc: null),
            new Dictionary<string, object>());

    private sealed class TestJob : Job, IHasImportMessagesFile
    {
        public override IReadOnlyList<Type> Stages { get; } = [];
        public required ImportMessagesFile Source { get; init; }

        public ImportMessagesFile GetImportMessagesFilePath() => Source;
    }
}




