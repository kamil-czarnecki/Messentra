using Messentra.Features.Jobs;
using Messentra.Infrastructure.Jobs;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Infrastructure.Jobs;

public sealed class JobWorkerShould
{
    [Fact]
    public async Task DequeueJobAndRunIt_WhenWorkerIsRunning()
    {
        // Arrange
        var queue = new Mock<IBackgroundJobQueue>(MockBehavior.Strict);
        var runner = new Mock<IJobRunner>(MockBehavior.Strict);
        var logger = new Mock<ILogger<JobWorker>>();

        var runCalled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dequeueCalls = 0;
        var dequeueToken = CancellationToken.None;
        var runToken = CancellationToken.None;

        queue
            .Setup(x => x.Dequeue(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(ct =>
            {
                dequeueCalls++;
                if (dequeueCalls == 1)
                {
                    dequeueToken = ct;
                    return new ValueTask<long>(123L);
                }

                return new ValueTask<long>(WaitForCancellationAsync(ct));
            });

        runner
            .Setup(x => x.Run(123L, It.IsAny<CancellationToken>()))
            .Callback<long, CancellationToken>((_, ct) =>
            {
                runToken = ct;
                runCalled.TrySetResult();
            })
            .Returns(Task.CompletedTask);

        var sut = new JobWorker(runner.Object, queue.Object, logger.Object);

        // Act
        await sut.StartAsync(CancellationToken.None);
        await runCalled.Task.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        await sut.StopAsync(CancellationToken.None);

        // Assert
        runner.Verify(x => x.Run(123L, It.IsAny<CancellationToken>()), Times.Once);
        runToken.ShouldBe(dequeueToken);
    }

    [Fact]
    public async Task LogErrorAndContinueWithNextJob_WhenRunnerThrows()
    {
        // Arrange
        var queue = new Mock<IBackgroundJobQueue>(MockBehavior.Strict);
        var runner = new Mock<IJobRunner>(MockBehavior.Strict);
        var logger = new Mock<ILogger<JobWorker>>();

        var secondRunCalled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dequeueCalls = 0;

        queue
            .Setup(x => x.Dequeue(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(ct =>
            {
                dequeueCalls++;
                return dequeueCalls switch
                {
                    1 => new ValueTask<long>(10L),
                    2 => new ValueTask<long>(20L),
                    _ => new ValueTask<long>(WaitForCancellationAsync(ct))
                };
            });

        runner
            .Setup(x => x.Run(10L, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        runner
            .Setup(x => x.Run(20L, It.IsAny<CancellationToken>()))
            .Callback(() => secondRunCalled.TrySetResult())
            .Returns(Task.CompletedTask);

        var sut = new JobWorker(runner.Object, queue.Object, logger.Object);

        // Act
        await sut.StartAsync(CancellationToken.None);
        await secondRunCalled.Task.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        await sut.StopAsync(CancellationToken.None);

        // Assert
        runner.Verify(x => x.Run(10L, It.IsAny<CancellationToken>()), Times.Once);
        runner.Verify(x => x.Run(20L, It.IsAny<CancellationToken>()), Times.Once);
        logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Unexpected job worker failure for job 10")),
                It.Is<Exception>(ex => ex.Message == "boom"),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopWithoutRunningJobs_WhenCancelledWhileWaitingForQueue()
    {
        // Arrange
        var queue = new Mock<IBackgroundJobQueue>(MockBehavior.Strict);
        var runner = new Mock<IJobRunner>(MockBehavior.Strict);
        var logger = new Mock<ILogger<JobWorker>>();

        var dequeueCalled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        queue
            .Setup(x => x.Dequeue(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(ct =>
            {
                dequeueCalled.TrySetResult();
                return new ValueTask<long>(WaitForCancellationAsync(ct));
            });

        var sut = new JobWorker(runner.Object, queue.Object, logger.Object);

        // Act
        await sut.StartAsync(CancellationToken.None);
        await dequeueCalled.Task.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        await sut.StopAsync(CancellationToken.None);

        // Assert
        runner.Verify(x => x.Run(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static async Task<long> WaitForCancellationAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        return 0;
    }
}


