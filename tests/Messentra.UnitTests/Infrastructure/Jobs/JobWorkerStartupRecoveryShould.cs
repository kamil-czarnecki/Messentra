using Messentra.Domain;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Jobs;
using Messentra.Features.Jobs.ImportMessages;
using Messentra.Infrastructure.Database;
using Messentra.Infrastructure.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Infrastructure.Jobs;

public sealed class JobWorkerStartupRecoveryShould : InMemoryDbTestBase
{
    [Fact]
    public async Task PauseRunningJobs_WhenWorkerStarts()
    {
        // Arrange
        var runningJob = CreateJob(JobStatus.Running, "running");
        var queuedJob = CreateJob(JobStatus.Queued, "queued");

        await DbContext.Set<Job>().AddRangeAsync([runningJob, queuedJob], TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dequeueCalled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var queue = new Mock<IBackgroundJobQueue>(MockBehavior.Strict);
        queue
            .Setup(x => x.Dequeue(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(ct =>
            {
                dequeueCalled.TrySetResult();
                return new ValueTask<long>(WaitForCancellationAsync(ct));
            });

        var runner = new Mock<IJobRunner>(MockBehavior.Strict);
        var logger = new Mock<ILogger<JobWorker>>();
        var scopeFactory = CreateScopeFactory(DbContext);

        var sut = new JobWorker(runner.Object, queue.Object, logger.Object, scopeFactory.Object);

        // Act
        await sut.StartAsync(CancellationToken.None);
        await dequeueCalled.Task.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        await sut.StopAsync(CancellationToken.None);

        DbContext.ChangeTracker.Clear();
        var savedRunning = await DbContext.Set<Job>()
            .SingleAsync(x => x.Id == runningJob.Id, TestContext.Current.CancellationToken);
        var savedQueued = await DbContext.Set<Job>()
            .SingleAsync(x => x.Id == queuedJob.Id, TestContext.Current.CancellationToken);

        // Assert
        savedRunning.Status.ShouldBe(JobStatus.Paused);
        savedQueued.Status.ShouldBe(JobStatus.Queued);
    }

    private static Mock<IServiceScopeFactory> CreateScopeFactory(MessentraDbContext dbContext)
    {
        var provider = new Mock<IServiceProvider>();
        provider
            .Setup(x => x.GetService(typeof(MessentraDbContext)))
            .Returns(dbContext);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(provider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        return scopeFactory;
    }

    private static ImportMessagesJob CreateJob(JobStatus status, string label)
    {
        var job = new ImportMessagesJob
        {
            Label = label,
            CreatedAt = DateTime.UtcNow,
            Input = new ImportMessagesJobRequest(
                ConnectionConfig.CreateConnectionString("Endpoint=sb://tests/"),
                new ResourceTarget.Queue("orders", SubQueue.Active))
        };

        if (status != JobStatus.Queued)
        {
            job.UpdateStatus(status);
        }

        return job;
    }

    private static async Task<long> WaitForCancellationAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        return 0;
    }
}

