using Messentra.Domain;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Jobs;
using Messentra.Features.Jobs.ImportMessages;
using Messentra.Features.Jobs.ResumeJob;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Jobs.ResumeJob;

public sealed class ResumeJobCommandHandlerShould : InMemoryDbTestBase
{
    [Fact]
    public async Task QueueJobAndReturnTrue_WhenJobIsPaused()
    {
        // Arrange
        var queue = new Mock<IBackgroundJobQueue>();
        var sut = new ResumeJobCommandHandler(DbContext, queue.Object);
        var job = CreateJob("paused-job", JobStatus.Paused);

        DbContext.Set<Job>().Add(job);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await sut.Handle(new ResumeJobCommand(job.Id), TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeTrue();
        DbContext.ChangeTracker.Clear();
        var saved = await DbContext.Set<Job>().FindAsync([job.Id], TestContext.Current.CancellationToken);
        saved.ShouldNotBeNull();
        saved!.Status.ShouldBe(JobStatus.Queued);
        queue.Verify(x => x.Enqueue(job.Id, TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task ReturnFalseAndSkipQueue_WhenJobIsNotPaused()
    {
        // Arrange
        var queue = new Mock<IBackgroundJobQueue>();
        var sut = new ResumeJobCommandHandler(DbContext, queue.Object);
        var job = CreateJob("running-job", JobStatus.Running);

        DbContext.Set<Job>().Add(job);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await sut.Handle(new ResumeJobCommand(job.Id), TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeFalse();
        queue.Verify(x => x.Enqueue(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static ImportMessagesJob CreateJob(string label, JobStatus status)
    {
        var job = new ImportMessagesJob
        {
            Label = label,
            CreatedAt = DateTime.UtcNow,
            Input = new ImportMessagesJobRequest(
                ConnectionConfig.CreateConnectionString("Endpoint=sb://tests/"),
                new ResourceTarget.Queue("orders", SubQueue.Active))
        };

        job.UpdateStatus(status);

        return job;
    }
}


