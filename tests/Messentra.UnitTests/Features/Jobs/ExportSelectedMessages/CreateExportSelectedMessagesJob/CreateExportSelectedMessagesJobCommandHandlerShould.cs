using Messentra.Domain;
using Messentra.Features.Jobs;
using Messentra.Features.Jobs.ExportSelectedMessages;
using Messentra.Features.Jobs.ExportSelectedMessages.CreateExportSelectedMessagesJob;
using Messentra.Features.Jobs.Stages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Jobs.ExportSelectedMessages.CreateExportSelectedMessagesJob;

public sealed class CreateExportSelectedMessagesJobCommandHandlerShould : InMemoryDbTestBase
{
    private static ServiceBusMessageDto CreateMessageDto(string messageId) =>
        new(
            messageId,
            new ServiceBusProperties(null, null, null, messageId, null, null, null, null, null, null, null, null, null),
            new Dictionary<string, object>());

    [Fact]
    public async Task PersistExportJobAndReturnJobListItem_WhenHandleCalled()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<CreateExportSelectedMessagesJobCommandHandler>>();
        var sut = new CreateExportSelectedMessagesJobCommandHandler(new TestDbContextFactory(DbContext), loggerMock.Object);

        var messages = new List<ServiceBusMessageDto>
        {
            CreateMessageDto("msg-1"),
            CreateMessageDto("msg-2")
        };
        var request = new ExportSelectedMessagesJobRequest(messages, "orders-Active");
        var command = new CreateExportSelectedMessagesJobCommand(request);
        var before = DateTime.UtcNow;

        // Act
        var result = await sut.Handle(command, TestContext.Current.CancellationToken);
        var after = DateTime.UtcNow;

        // Assert
        var savedJob = await DbContext.Set<Job>()
            .OfType<ExportSelectedMessagesJob>()
            .SingleAsync(cancellationToken: TestContext.Current.CancellationToken);

        savedJob.Input!.ResourceLabel.ShouldBe(request.ResourceLabel);
        savedJob.Input!.Messages.Count.ShouldBe(request.Messages.Count);
        savedJob.MaxRetries.ShouldBe(3);
        savedJob.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        savedJob.CreatedAt.ShouldBeLessThanOrEqualTo(after);
        savedJob.Label.ShouldBe("ExportSelectedMessages-orders-Active-2msgs");

        result.Id.ShouldBe(savedJob.Id);
        result.Label.ShouldBe(savedJob.Label);
        result.Status.ShouldBe(JobStatus.Queued);
    }

    [Fact]
    public async Task ThrowInvalidOperationException_WhenMessageListIsEmpty()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<CreateExportSelectedMessagesJobCommandHandler>>();
        var sut = new CreateExportSelectedMessagesJobCommandHandler(new TestDbContextFactory(DbContext), loggerMock.Object);

        var command = new CreateExportSelectedMessagesJobCommand(
            new ExportSelectedMessagesJobRequest([], "orders-Active"));

        // Act
        var act = () => sut.Handle(command, TestContext.Current.CancellationToken).AsTask();

        // Assert
        await act.ShouldThrowAsync<InvalidOperationException>();
        (await DbContext.Set<Job>().CountAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
    }
}
