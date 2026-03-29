using Messentra.Infrastructure.Jobs;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Infrastructure.Jobs;

public sealed class BackgroundJobQueueShould
{
    [Fact]
    public async Task DequeueInFifoOrder_WhenMultipleItemsAreEnqueued()
    {
        // Arrange
        var sut = new BackgroundJobQueue();

        // Act
        await sut.Enqueue(1, CancellationToken.None);
        await sut.Enqueue(2, CancellationToken.None);
        var first = await sut.Dequeue(CancellationToken.None);
        var second = await sut.Dequeue(CancellationToken.None);

        // Assert
        first.ShouldBe(1);
        second.ShouldBe(2);
    }

    [Fact]
    public async Task WaitForItem_WhenDequeueCalledBeforeEnqueue()
    {
        // Arrange
        var sut = new BackgroundJobQueue();
        var dequeueTask = sut.Dequeue(CancellationToken.None).AsTask();

        // Act
        await Task.Delay(50, TestContext.Current.CancellationToken);
        dequeueTask.IsCompleted.ShouldBeFalse();
        await sut.Enqueue(42, CancellationToken.None);
        var dequeued = await dequeueTask;

        // Assert
        dequeued.ShouldBe(42);
    }

    [Fact]
    public async Task ThrowOperationCanceledException_WhenQueueIsEmptyAndCancellationRequested()
    {
        // Arrange
        var sut = new BackgroundJobQueue();
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        // Act
        var action = async () => await sut.Dequeue(cts.Token).AsTask();

        // Assert
        await action.ShouldThrowAsync<OperationCanceledException>();
    }
}

