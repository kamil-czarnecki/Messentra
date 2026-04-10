using Mediator;
using Messentra.Features.Jobs;
using Messentra.Features.Jobs.EnqueueJob;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Jobs.EnqueueJob;

public sealed class EnqueueJobCommandHandlerShould
{
    [Fact]
    public async Task EnqueueJobIdAndReturnUnit_WhenHandleCalled()
    {
        // Arrange
        var queue = new Mock<IBackgroundJobQueue>();
        const long jobId = 42;
        var ct = TestContext.Current.CancellationToken;
        var sut = new EnqueueJobCommandHandler(queue.Object);

        // Act
        var result = await sut.Handle(new EnqueueJobCommand(jobId), ct);

        // Assert
        queue.Verify(x => x.Enqueue(jobId, ct), Times.Once);
        result.ShouldBe(Unit.Value);
    }
}
