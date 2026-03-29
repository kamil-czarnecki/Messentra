using Messentra.Domain;
using Messentra.Features.Jobs;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Jobs;

public sealed class JobProgressNotifierShould
{
    [Fact]
    public void NotifySubscriber_WhenUpdateIsPublished()
    {
        // Arrange
        var sut = CreateSut();
        JobProgressUpdate? received = null;
        sut.Subscribe(update => received = update);
        var expected = new JobProgressUpdate(1, JobStatus.Running, new StageProgress("Fetch", 25));

        // Act
        sut.Publish(expected);

        // Assert
        received.ShouldNotBeNull();
        received.ShouldBe(expected);
    }

    [Fact]
    public void NotifyAllSubscribers_WhenUpdateIsPublished()
    {
        // Arrange
        var sut = CreateSut();
        var firstReceived = new List<JobProgressUpdate>();
        var secondReceived = new List<JobProgressUpdate>();

        sut.Subscribe(update => firstReceived.Add(update));
        sut.Subscribe(update => secondReceived.Add(update));

        var updateToPublish = new JobProgressUpdate(2, JobStatus.Running, new StageProgress("Serialize", 50), 1);

        // Act
        sut.Publish(updateToPublish);

        // Assert
        firstReceived.Count.ShouldBe(1);
        secondReceived.Count.ShouldBe(1);
        firstReceived[0].ShouldBe(updateToPublish);
        secondReceived[0].ShouldBe(updateToPublish);
    }

    [Fact]
    public void StopNotifyingDisposedSubscription_WhenUpdateIsPublishedAfterDispose()
    {
        // Arrange
        var sut = CreateSut();
        var received = new List<JobProgressUpdate>();
        var subscription = sut.Subscribe(update => received.Add(update));
        var first = new JobProgressUpdate(3, JobStatus.Running, new StageProgress("Fetch", 10));
        var second = new JobProgressUpdate(3, JobStatus.Running, new StageProgress("Fetch", 20));

        // Act
        sut.Publish(first);
        subscription.Dispose();
        sut.Publish(second);

        // Assert
        received.Count.ShouldBe(1);
        received[0].ShouldBe(first);
    }

    [Fact]
    public void AllowDisposeTwiceWithoutThrowing_WhenSubscriptionAlreadyDisposed()
    {
        // Arrange
        var sut = CreateSut();
        var subscription = sut.Subscribe(_ => { });

        // Act
        subscription.Dispose();
        var action = () => subscription.Dispose();

        // Assert
        action.ShouldNotThrow();
    }

    [Fact]
    public void KeepOtherSubscribersActive_WhenOneSubscriptionIsDisposed()
    {
        // Arrange
        var sut = CreateSut();
        var firstReceived = new List<JobProgressUpdate>();
        var secondReceived = new List<JobProgressUpdate>();

        var firstSubscription = sut.Subscribe(update => firstReceived.Add(update));
        sut.Subscribe(update => secondReceived.Add(update));

        var updateToPublish = new JobProgressUpdate(5, JobStatus.Completed, new StageProgress("Done", 100));

        // Act
        firstSubscription.Dispose();
        sut.Publish(updateToPublish);

        // Assert
        firstReceived.Count.ShouldBe(0);
        secondReceived.Count.ShouldBe(1);
        secondReceived[0].ShouldBe(updateToPublish);
    }

    [Fact]
    public void ContinueNotifyingOtherSubscribers_WhenOneSubscriberThrows()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<JobProgressNotifier>>();
        var sut = new JobProgressNotifier(loggerMock.Object);
        var received = new List<JobProgressUpdate>();
        var update = new JobProgressUpdate(8, JobStatus.Running, new StageProgress("Fetch", 75));

        sut.Subscribe(_ => throw new InvalidOperationException("boom"));
        sut.Subscribe(x => received.Add(x));

        // Act
        sut.Publish(update);

        // Assert
        received.Count.ShouldBe(1);
        received[0].ShouldBe(update);
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Subscriber callback failed")),
                It.Is<InvalidOperationException>(ex => ex.Message == "boom"),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static JobProgressNotifier CreateSut()
    {
        var logger = Mock.Of<ILogger<JobProgressNotifier>>();
        return new JobProgressNotifier(logger);
    }
}


