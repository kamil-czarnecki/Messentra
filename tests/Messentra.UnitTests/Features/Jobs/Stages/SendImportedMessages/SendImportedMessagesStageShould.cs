using AutoFixture;
using Messentra.Domain;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Jobs;
using Messentra.Features.Jobs.Stages;
using Messentra.Features.Jobs.Stages.ImportMessages;
using Messentra.Features.Jobs.Stages.SendImportedMessages;
using Messentra.Infrastructure.AzureServiceBus;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Jobs.Stages.SendImportedMessages;

public sealed class SendImportedMessagesStageShould : InMemoryDbTestBase
{
    private readonly Fixture _fixture = new();
    private readonly Mock<IAzureServiceBusSender> _sender = new();

    [Fact]
    public async Task SendOnlyUnsentMessagesAndMarkThemSent_WhenRunCalled()
    {
        // Arrange
        var job = CreateJob();

        await DbContext.Set<ImportedMessage>().AddRangeAsync(
        [
            CreateImported(job.Id, 1, "already-sent", isSent: true),
            CreateImported(job.Id, 2, "to-send", isSent: false)
        ], TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _sender
            .Setup(x => x.SendBatchChunk(
                It.IsAny<ConnectionInfo>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<ServiceBusMessageDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = new SendImportedMessagesStage<TestJob>(DbContext, _sender.Object);

        // Act
        await sut.Run(job, TestContext.Current.CancellationToken);

        // Assert
        var rows = DbContext.Set<ImportedMessage>()
            .AsNoTracking()
            .Where(x => x.JobId == job.Id)
            .OrderBy(x => x.Position)
            .ToList();

        rows[0].IsSent.ShouldBeTrue();
        rows[1].IsSent.ShouldBeTrue();
        rows[1].SentOn.ShouldNotBeNull();

        job.StageProgress.Stage.ShouldBe("Sending messages");
        job.Result.ShouldNotBeNull();
        job.Result!.SentMessagesCount.ShouldBe(2);
        _sender.Verify(x => x.SendBatchChunk(
            It.IsAny<ConnectionInfo>(),
            "orders",
            It.Is<IReadOnlyList<ServiceBusMessageDto>>(list => list.Count == 1 && list[0].Properties.MessageId == "to-send"),
            It.IsAny<CancellationToken>()), Times.Once);
        _sender.Verify(x => x.Send(It.IsAny<ConnectionInfo>(), It.IsAny<string>(), It.IsAny<ServiceBusMessageDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GenerateNewMessageId_WhenOptionEnabled()
    {
        // Arrange
        var job = CreateJob(generateNewMessageId: true);

        await DbContext.Set<ImportedMessage>().AddAsync(
            CreateImported(job.Id, 1, "original-message-id", isSent: false),
            TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        IReadOnlyList<ServiceBusMessageDto>? capturedMessages = null;
        _sender
            .Setup(x => x.SendBatchChunk(
                It.IsAny<ConnectionInfo>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<ServiceBusMessageDto>>(),
                It.IsAny<CancellationToken>()))
            .Callback<ConnectionInfo, string, IReadOnlyList<ServiceBusMessageDto>, CancellationToken>((_, _, messages, _) => capturedMessages = messages)
            .ReturnsAsync(1);

        var sut = new SendImportedMessagesStage<TestJob>(DbContext, _sender.Object);

        // Act
        await sut.Run(job, TestContext.Current.CancellationToken);

        // Assert
        capturedMessages.ShouldNotBeNull();
        capturedMessages.Count.ShouldBe(1);
        capturedMessages[0].Properties.MessageId.ShouldNotBeNullOrWhiteSpace();
        capturedMessages[0].Properties.MessageId.ShouldNotBe("original-message-id");
    }

    [Fact]
    public async Task FallbackToSingleSendAndMarkSent_WhenBatchChunkCannotFitMessage()
    {
        // Arrange
        var job = CreateJob();

        await DbContext.Set<ImportedMessage>().AddAsync(
            CreateImported(job.Id, 1, "oversized", isSent: false),
            TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _sender
            .Setup(x => x.SendBatchChunk(
                It.IsAny<ConnectionInfo>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<ServiceBusMessageDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _sender
            .Setup(x => x.Send(
                It.IsAny<ConnectionInfo>(),
                It.IsAny<string>(),
                It.IsAny<ServiceBusMessageDto>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new SendImportedMessagesStage<TestJob>(DbContext, _sender.Object);

        // Act
        await sut.Run(job, TestContext.Current.CancellationToken);

        // Assert
        var row = DbContext.Set<ImportedMessage>().AsNoTracking().Single(x => x.JobId == job.Id);
        row.IsSent.ShouldBeTrue();
        row.SentOn.ShouldNotBeNull();
        _sender.Verify(x => x.SendBatchChunk(It.IsAny<ConnectionInfo>(), "orders", It.IsAny<IReadOnlyList<ServiceBusMessageDto>>(), It.IsAny<CancellationToken>()), Times.Once);
        _sender.Verify(x => x.Send(It.IsAny<ConnectionInfo>(), "orders", It.IsAny<ServiceBusMessageDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private TestJob CreateJob(bool generateNewMessageId = false)
    {
        var config = ConnectionConfig.CreateConnectionString("Endpoint=sb://tests/");

        return new TestJob(new MessageImportSendConfiguration(config, new ResourceTarget.Queue("orders", SubQueue.Active), generateNewMessageId))
        {
            Id = _fixture.Create<long>(),
            Label = "import-job",
            CreatedAt = DateTime.UtcNow
        };
    }

    private static ImportedMessage CreateImported(long jobId, long position, string id, bool isSent) =>
        new()
        {
            JobId = jobId,
            Position = position,
            Message = new ServiceBusMessageDto(
                "body",
                new ServiceBusProperties(
                    ContentType: "text/plain",
                    CorrelationId: null,
                    Subject: null,
                    MessageId: id,
                    To: null,
                    ReplyTo: null,
                    TimeToLive: null,
                    ReplyToSessionId: null,
                    SessionId: null,
                    PartitionKey: null,
                    ScheduledEnqueueTime: null,
                    TransactionPartitionKey: null,
                    EnqueuedTimeUtc: null),
                new Dictionary<string, object>()),
            IsSent = isSent,
            CreatedOn = DateTime.UtcNow,
            SentOn = isSent ? DateTime.UtcNow : null
        };

    private sealed class TestJob(MessageImportSendConfiguration configuration) : Job, IHasMessageImportSendConfiguration, IStageCompletionHandler<SendImportedMessagesStageResult>
    {
        public override IReadOnlyList<Type> Stages { get; } = [];
        public SendImportedMessagesStageResult? Result { get; private set; }

        public MessageImportSendConfiguration GetMessageImportSendConfiguration() => configuration;

        public void StageCompleted(SendImportedMessagesStageResult result)
        {
            Result = result;
        }
    }
}

