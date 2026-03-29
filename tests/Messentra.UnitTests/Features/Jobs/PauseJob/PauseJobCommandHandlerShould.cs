using Messentra.Domain;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Jobs;
using Messentra.Features.Jobs.PauseJob;
using Messentra.Features.Jobs.ImportMessages;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Jobs.PauseJob;

public sealed class PauseJobCommandHandlerShould : InMemoryDbTestBase
{
    [Fact]
    public async Task ReturnTrueAndRequestPause_WhenJobIsRunning()
    {
        // Arrange
        var registry = new Mock<IJobCancellationRegistry>();
        var sut = new PauseJobCommandHandler(new TestDbContextFactory(DbContext), registry.Object);
        var job = CreateJob("running-job", JobStatus.Running);

        DbContext.Set<Job>().Add(job);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await sut.Handle(new PauseJobCommand(job.Id), TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeTrue();
        registry.Verify(x => x.RequestPause(job.Id), Times.Once);
    }

    [Fact]
    public async Task ReturnFalseAndDoNothing_WhenJobIsNotRunning()
    {
        // Arrange
        var registry = new Mock<IJobCancellationRegistry>();
        var sut = new PauseJobCommandHandler(new TestDbContextFactory(DbContext), registry.Object);
        var job = CreateJob("paused-job", JobStatus.Paused);

        DbContext.Set<Job>().Add(job);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await sut.Handle(new PauseJobCommand(job.Id), TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeFalse();
        registry.Verify(x => x.RequestPause(It.IsAny<long>()), Times.Never);
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


