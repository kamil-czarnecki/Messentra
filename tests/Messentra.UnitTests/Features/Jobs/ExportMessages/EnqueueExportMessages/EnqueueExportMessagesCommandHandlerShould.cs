using Mediator;
using Messentra.Domain;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Jobs;
using Messentra.Features.Jobs.ExportMessages;
using Messentra.Features.Jobs.ExportMessages.EnqueueExportMessages;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Jobs.ExportMessages.EnqueueExportMessages;

public sealed class EnqueueExportMessagesCommandHandlerShould : InMemoryDbTestBase
{
    [Fact]
    public async Task PersistExportJobEnqueueItAndReturnUnit_WhenHandleCalled()
    {
        // Arrange
        var queueMock = new Mock<IBackgroundJobQueue>();
        var sut = new EnqueueExportMessagesCommandHandler(DbContext, queueMock.Object);

        var request = new ExportMessagesJobRequest(
            ConnectionConfig.CreateConnectionString("Endpoint=sb://tests/"),
            new ResourceTarget.Queue("orders", SubQueue.Active),
            250);

        var command = new EnqueueExportMessagesCommand(request);
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        var before = DateTime.UtcNow;

        // Act
        var result = await sut.Handle(command, cancellationToken);
        var after = DateTime.UtcNow;

        // Assert
        result.ShouldBe(Unit.Value);

        var savedJob = await DbContext.Set<Job>()
            .OfType<ExportMessagesJob>()
            .SingleAsync(cancellationToken: TestContext.Current.CancellationToken);

        savedJob.Input.ShouldBe(request);
        savedJob.MaxRetries.ShouldBe(3);
        savedJob.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        savedJob.CreatedAt.ShouldBeLessThanOrEqualTo(after);
        savedJob.Label.ShouldStartWith("ExportMessagesJob-");

        queueMock.Verify(x => x.Enqueue(savedJob.Id, cancellationToken), Times.Once);
    }
}

