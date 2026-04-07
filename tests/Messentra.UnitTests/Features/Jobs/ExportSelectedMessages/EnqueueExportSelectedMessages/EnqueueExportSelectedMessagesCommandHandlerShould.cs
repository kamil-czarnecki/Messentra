using Mediator;
using Messentra.Domain;
using Messentra.Features.Jobs;
using Messentra.Features.Jobs.ExportSelectedMessages;
using Messentra.Features.Jobs.ExportSelectedMessages.EnqueueExportSelectedMessages;
using Messentra.Features.Jobs.Stages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Jobs.ExportSelectedMessages.EnqueueExportSelectedMessages;

public sealed class EnqueueExportSelectedMessagesCommandHandlerShould : InMemoryDbTestBase
{
    private static ServiceBusMessageDto CreateMessageDto(string messageId) =>
        new(
            messageId,
            new ServiceBusProperties(null, null, null, messageId, null, null, null, null, null, null, null, null, null),
            new Dictionary<string, object>());

    [Fact]
    public async Task PersistExportJobEnqueueItAndReturnUnit_WhenHandleCalled()
    {
        // Arrange
        var queueMock = new Mock<IBackgroundJobQueue>();
        var loggerMock = new Mock<ILogger<EnqueueExportSelectedMessagesCommandHandler>>();
        var sut = new EnqueueExportSelectedMessagesCommandHandler(
            new TestDbContextFactory(DbContext), queueMock.Object, loggerMock.Object);

        var messages = new List<ServiceBusMessageDto>
        {
            CreateMessageDto("msg-1"),
            CreateMessageDto("msg-2")
        };
        var request = new ExportSelectedMessagesJobRequest(messages, "orders-Active");
        var command = new EnqueueExportSelectedMessagesCommand(request);
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        var before = DateTime.UtcNow;

        // Act
        var result = await sut.Handle(command, cancellationToken);
        var after = DateTime.UtcNow;

        // Assert
        result.ShouldBe(Unit.Value);

        var savedJob = await DbContext.Set<Job>()
            .OfType<ExportSelectedMessagesJob>()
            .SingleAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Note: ServiceBusMessageDto.Message is object — after JSON round-trip becomes JsonElement.
        // Check structural equality instead of record equality.
        savedJob.Input!.ResourceLabel.ShouldBe(request.ResourceLabel);
        savedJob.Input!.Messages.Count.ShouldBe(request.Messages.Count);
        savedJob.MaxRetries.ShouldBe(3);
        savedJob.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        savedJob.CreatedAt.ShouldBeLessThanOrEqualTo(after);
        savedJob.Label.ShouldBe("ExportSelectedMessages-orders-Active-2msgs");

        queueMock.Verify(x => x.Enqueue(savedJob.Id, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task ReturnWithoutPersistingOrEnqueuing_WhenMessageListIsEmpty()
    {
        // Arrange
        var queueMock = new Mock<IBackgroundJobQueue>();
        var loggerMock = new Mock<ILogger<EnqueueExportSelectedMessagesCommandHandler>>();
        var sut = new EnqueueExportSelectedMessagesCommandHandler(
            new TestDbContextFactory(DbContext), queueMock.Object, loggerMock.Object);

        var command = new EnqueueExportSelectedMessagesCommand(
            new ExportSelectedMessagesJobRequest([], "orders-Active"));

        // Act
        var result = await sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(Unit.Value);
        (await DbContext.Set<Job>().CountAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
        queueMock.Verify(x => x.Enqueue(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Skipping export selected messages")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
