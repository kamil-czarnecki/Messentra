using Messentra.Domain;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Jobs;
using Messentra.Features.Jobs.ExportMessages;
using Messentra.Features.Jobs.ExportMessages.CreateExportMessagesJob;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Jobs.ExportMessages.CreateExportMessagesJob;

public sealed class CreateExportMessagesJobCommandHandlerShould : InMemoryDbTestBase
{
    [Fact]
    public async Task PersistExportJobAndReturnJobListItem_WhenHandleCalled()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<CreateExportMessagesJobCommandHandler>>();
        var sut = new CreateExportMessagesJobCommandHandler(new TestDbContextFactory(DbContext), loggerMock.Object);

        var request = new ExportMessagesJobRequest(
            ConnectionConfig.CreateConnectionString("Endpoint=sb://tests/"),
            new ResourceTarget.Queue("orders", SubQueue.Active),
            250);

        var command = new CreateExportMessagesJobCommand(request);
        var before = DateTime.UtcNow;

        // Act
        var result = await sut.Handle(command, TestContext.Current.CancellationToken);
        var after = DateTime.UtcNow;

        // Assert
        var savedJob = await DbContext.Set<Job>()
            .OfType<ExportMessagesJob>()
            .SingleAsync(cancellationToken: TestContext.Current.CancellationToken);

        savedJob.Input.ShouldBe(request);
        savedJob.MaxRetries.ShouldBe(3);
        savedJob.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        savedJob.CreatedAt.ShouldBeLessThanOrEqualTo(after);
        savedJob.Label.ShouldStartWith("ExportMessagesJob-");

        result.Id.ShouldBe(savedJob.Id);
        result.Label.ShouldBe(savedJob.Label);
        result.Status.ShouldBe(JobStatus.Queued);
    }

    [Fact]
    public async Task ThrowInvalidOperationException_WhenRequestedMessageCountIsNotPositive()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<CreateExportMessagesJobCommandHandler>>();
        var sut = new CreateExportMessagesJobCommandHandler(new TestDbContextFactory(DbContext), loggerMock.Object);

        var command = new CreateExportMessagesJobCommand(new ExportMessagesJobRequest(
            ConnectionConfig.CreateConnectionString("Endpoint=sb://tests/"),
            new ResourceTarget.Queue("orders", SubQueue.Active),
            0));

        // Act
        var act = () => sut.Handle(command, TestContext.Current.CancellationToken).AsTask();

        // Assert
        await act.ShouldThrowAsync<InvalidOperationException>();
        (await DbContext.Set<Job>().CountAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
    }
}

