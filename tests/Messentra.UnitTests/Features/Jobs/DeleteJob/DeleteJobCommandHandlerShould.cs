using Messentra.Domain;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Jobs;
using Messentra.Features.Jobs.DeleteJob;
using Messentra.Features.Jobs.ImportMessages;
using Messentra.Features.Jobs.Stages.ImportMessages;
using Messentra.Features.Jobs.Stages;
using Messentra.Features.Jobs.Stages.FetchMessages;
using Messentra.Infrastructure;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Jobs.DeleteJob;

public sealed class DeleteJobCommandHandlerShould : InMemoryDbTestBase
{
    private readonly Mock<IFileSystem> _fileSystem = new();

    [Fact]
    public async Task DeleteJobBatchesAndFolder_WhenJobIsNotRunning()
    {
        // Arrange
        var job = CreateJob(JobStatus.Completed);
        await DbContext.Set<Job>().AddAsync(job, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await DbContext.Set<FetchedMessagesBatch>().AddRangeAsync(
        [
            new FetchedMessagesBatch
            {
                JobId = job.Id,
                Messages = [CreateServiceBusMessage("a")],
                MessagesCount = 1,
                LastSequence = 1,
                CreatedOn = DateTime.UtcNow
            },
            new FetchedMessagesBatch
            {
                JobId = job.Id,
                Messages = [CreateServiceBusMessage("b")],
                MessagesCount = 1,
                LastSequence = 2,
                CreatedOn = DateTime.UtcNow
            }
        ], TestContext.Current.CancellationToken);

        await DbContext.Set<ImportedMessage>().AddAsync(
            new ImportedMessage
            {
                JobId = job.Id,
                Position = 1,
                Message = CreateServiceBusMessage("import-a"),
                IsSent = false,
                CreatedOn = DateTime.UtcNow
            },
            TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var root = "/tmp/messentra-tests";
        var expectedPath = Path.Combine(root, "Jobs", job.Id.ToString());
        _fileSystem.Setup(x => x.GetRootPath()).Returns(root);
        _fileSystem.Setup(x => x.DirectoryExists(expectedPath)).Returns(true);

        var sut = new DeleteJobCommandHandler(new TestDbContextFactory(DbContext), _fileSystem.Object);

        // Act
        var result = await sut.Handle(new DeleteJobCommand(job.Id), TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeTrue();
        DbContext.ChangeTracker.Clear();
        (await DbContext.Set<Job>().FindAsync([job.Id], TestContext.Current.CancellationToken)).ShouldBeNull();
        DbContext.Set<FetchedMessagesBatch>().Count(x => x.JobId == job.Id).ShouldBe(0);
        DbContext.Set<ImportedMessage>().Count(x => x.JobId == job.Id).ShouldBe(0);
        _fileSystem.Verify(x => x.DeleteDirectory(expectedPath, true), Times.Once);
        _fileSystem.Verify(x => x.Delete(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteImportSourceFile_WhenSourceFileIsUnderAppRoot()
    {
        // Arrange
        var root = "/tmp/messentra-tests";
        var sourceFilePath = Path.Combine(root, "Jobs", "Imports", "import.json");
        var job = CreateJob(JobStatus.Completed, sourceFilePath);

        await DbContext.Set<Job>().AddAsync(job, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var expectedJobFolderPath = Path.Combine(root, "Jobs", job.Id.ToString());

        _fileSystem.Setup(x => x.GetRootPath()).Returns(root);
        _fileSystem.Setup(x => x.FileExists(sourceFilePath)).Returns(true);
        _fileSystem.Setup(x => x.DirectoryExists(expectedJobFolderPath)).Returns(false);

        var sut = new DeleteJobCommandHandler(new TestDbContextFactory(DbContext), _fileSystem.Object);

        // Act
        var result = await sut.Handle(new DeleteJobCommand(job.Id), TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeTrue();
        _fileSystem.Verify(x => x.Delete(sourceFilePath), Times.Once);
    }

    [Fact]
    public async Task DoNotDeleteImportSourceFile_WhenSourceFileIsOutsideAppRoot()
    {
        // Arrange
        var root = "/tmp/messentra-tests";
        var sourceFilePath = "/tmp/external/import.json";
        var job = CreateJob(JobStatus.Completed, sourceFilePath);

        await DbContext.Set<Job>().AddAsync(job, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var expectedJobFolderPath = Path.Combine(root, "Jobs", job.Id.ToString());

        _fileSystem.Setup(x => x.GetRootPath()).Returns(root);
        _fileSystem.Setup(x => x.FileExists(sourceFilePath)).Returns(true);
        _fileSystem.Setup(x => x.DirectoryExists(expectedJobFolderPath)).Returns(false);

        var sut = new DeleteJobCommandHandler(new TestDbContextFactory(DbContext), _fileSystem.Object);

        // Act
        var result = await sut.Handle(new DeleteJobCommand(job.Id), TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeTrue();
        _fileSystem.Verify(x => x.Delete(sourceFilePath), Times.Never);
    }

    [Fact]
    public async Task ReturnFalseAndKeepData_WhenJobIsRunning()
    {
        // Arrange
        var job = CreateJob(JobStatus.Running);
        await DbContext.Set<Job>().AddAsync(job, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new DeleteJobCommandHandler(new TestDbContextFactory(DbContext), _fileSystem.Object);

        // Act
        var result = await sut.Handle(new DeleteJobCommand(job.Id), TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeFalse();
        (await DbContext.Set<Job>().FindAsync([job.Id], TestContext.Current.CancellationToken)).ShouldNotBeNull();
        _fileSystem.Verify(x => x.DeleteDirectory(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    private static ImportMessagesJob CreateJob(JobStatus status, string sourceFilePath = "/tmp/import.json")
    {
        var job = new ImportMessagesJob
        {
            Label = "job",
            CreatedAt = DateTime.UtcNow,
            Input = new ImportMessagesJobRequest(
                ConnectionConfig.CreateConnectionString("Endpoint=sb://tests/"),
                new ResourceTarget.Queue("orders", SubQueue.Active),
                sourceFilePath,
                "hash")
        };

        job.UpdateStatus(status);

        return job;
    }

    private static ServiceBusMessageDto CreateServiceBusMessage(string id) =>
        new(
            Message: "body",
            Properties: new ServiceBusProperties(
                ContentType: "text/plain",
                CorrelationId: null,
                Subject: null,
                MessageId: id,
                To: null,
                ReplyTo: null,
                TimeToLive: TimeSpan.FromMinutes(1),
                ReplyToSessionId: null,
                SessionId: null,
                PartitionKey: null,
                ScheduledEnqueueTime: null,
                TransactionPartitionKey: null,
                EnqueuedTimeUtc: null),
            ApplicationProperties: new Dictionary<string, object>());
}


