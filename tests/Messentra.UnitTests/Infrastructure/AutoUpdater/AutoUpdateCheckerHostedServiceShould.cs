using Messentra.Infrastructure.AutoUpdater;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Infrastructure.AutoUpdater;

public sealed class AutoUpdateCheckerHostedServiceShould
{
    [Fact]
    public async Task CallCheckForUpdates_WhenIntervalElapsed()
    {
        // Arrange
        var autoUpdater = new Mock<IAutoUpdaterService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<AutoUpdateCheckerHostedService>>();
        var timeProvider = new FakeTimeProvider();
        var serviceScopeFactory = CreateServiceScopeFactory(autoUpdater.Object);

        var called = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        autoUpdater
            .Setup(x => x.CheckForUpdates())
            .Callback(() => called.TrySetResult())
            .Returns(Task.CompletedTask);

        var options = Options.Create(new AutoUpdatePollingOptions
        {
            CheckIntervalMinutes = 1
        });

        var sut = new AutoUpdateCheckerHostedService(options, logger.Object, serviceScopeFactory.Object, timeProvider);

        // Act
        await sut.StartAsync(CancellationToken.None);
        await AdvanceUntilAsync(called, timeProvider, TimeSpan.FromMinutes(1), 120);
        await sut.StopAsync(CancellationToken.None);

        // Assert
        autoUpdater.Verify(x => x.CheckForUpdates(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ContinuePollingAndLogWarning_WhenCheckForUpdatesThrows()
    {
        // Arrange
        var autoUpdater = new Mock<IAutoUpdaterService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<AutoUpdateCheckerHostedService>>();
        var timeProvider = new FakeTimeProvider();
        var serviceScopeFactory = CreateServiceScopeFactory(autoUpdater.Object);

        var callCount = 0;

        autoUpdater
            .Setup(x => x.CheckForUpdates())
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                    throw new InvalidOperationException("boom");
                return Task.CompletedTask;
            });

        var options = Options.Create(new AutoUpdatePollingOptions
        {
            CheckIntervalMinutes = 1
        });

        var sut = new AutoUpdateCheckerHostedService(options, logger.Object, serviceScopeFactory.Object, timeProvider);

        // Act
        await sut.StartAsync(CancellationToken.None);
        for (var i = 0; i < 6; i++)
        {
            timeProvider.Advance(TimeSpan.FromMinutes(1));
            await Task.Yield();
        }
        await sut.StopAsync(CancellationToken.None);

        // Assert
        callCount.ShouldBeGreaterThanOrEqualTo(2);
        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Periodic update check failed")),
                It.Is<Exception>(ex => ex.Message == "boom"),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopWithoutChecking_WhenCancelledBeforeFirstTick()
    {
        // Arrange
        var autoUpdater = new Mock<IAutoUpdaterService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<AutoUpdateCheckerHostedService>>();
        var timeProvider = new FakeTimeProvider();
        var serviceScopeFactory = CreateServiceScopeFactory(autoUpdater.Object);

        var options = Options.Create(new AutoUpdatePollingOptions
        {
            CheckIntervalMinutes = 10
        });

        var sut = new AutoUpdateCheckerHostedService(options, logger.Object, serviceScopeFactory.Object, timeProvider);

        // Act
        await sut.StartAsync(CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);

        // Assert
        autoUpdater.Verify(x => x.CheckForUpdates(), Times.Never);
    }

    private static async Task AdvanceUntilAsync(
        TaskCompletionSource completionSource,
        FakeTimeProvider timeProvider,
        TimeSpan step,
        int maxSteps)
    {
        for (var i = 0; i < maxSteps && !completionSource.Task.IsCompleted; i++)
        {
            timeProvider.Advance(step);
            await Task.Yield();
            await Task.Delay(1, TestContext.Current.CancellationToken);
        }

        await completionSource.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
    }

    private static Mock<IServiceScopeFactory> CreateServiceScopeFactory(IAutoUpdaterService autoUpdaterService)
    {
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider
            .Setup(x => x.GetService(typeof(IAutoUpdaterService)))
            .Returns(autoUpdaterService);

        var serviceScope = new Mock<IServiceScope>();
        serviceScope.SetupGet(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var serviceScopeFactory = new Mock<IServiceScopeFactory>(MockBehavior.Strict);
        serviceScopeFactory
            .Setup(x => x.CreateScope())
            .Returns(serviceScope.Object);

        return serviceScopeFactory;
    }
}



