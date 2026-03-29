using Mediator;
using System.Security.Cryptography;
using System.Text;
using Messentra.Domain;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Jobs;
using Messentra.Features.Jobs.ImportMessages;
using Messentra.Features.Jobs.ImportMessages.EnqueueImportMessages;
using Messentra.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Jobs.ImportMessages.EnqueueImportMessages;

public sealed class EnqueueImportMessagesCommandHandlerShould : InMemoryDbTestBase
{
    [Fact]
    public async Task PersistImportJobEnqueueItAndReturnUnit_WhenHandleCalled()
    {
        // Arrange
        var queueMock = new Mock<IBackgroundJobQueue>();
        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock.Setup(x => x.FileExists("/tmp/import.json")).Returns(true);
        var payload = "[{\"message\":\"body\",\"properties\":{},\"applicationProperties\":{}}]"u8.ToArray();
        fileSystemMock.Setup(x => x.OpenRead("/tmp/import.json")).Returns(() => new MemoryStream(payload));
        var loggerMock = new Mock<ILogger<EnqueueImportMessagesCommandHandler>>();
        var sut = new EnqueueImportMessagesCommandHandler(new TestDbContextFactory(DbContext), queueMock.Object, fileSystemMock.Object, loggerMock.Object);
        var expectedHash = Convert.ToHexString(SHA256.HashData(payload));

        var request = new ImportMessagesJobRequest(
            ConnectionConfig.CreateConnectionString("Endpoint=sb://tests/"),
            new ResourceTarget.Queue("orders", SubQueue.Active),
            "/tmp/import.json",
            string.Empty);

        var command = new EnqueueImportMessagesCommand(request);
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        // Act
        var result = await sut.Handle(command, cancellationToken);

        // Assert
        result.ShouldBe(Unit.Value);

        var savedJob = await DbContext.Set<Job>()
            .OfType<ImportMessagesJob>()
            .SingleAsync(cancellationToken: TestContext.Current.CancellationToken);

        savedJob.Input.ShouldNotBeNull();
        savedJob.Input.ConnectionConfig.ShouldBe(request.ConnectionConfig);
        savedJob.Input.Target.ShouldBe(request.Target);
        savedJob.Input.SourceFilePath.ShouldBe(request.SourceFilePath);
        savedJob.Input.SourceFileHash.ShouldBe(expectedHash);
        savedJob.MaxRetries.ShouldBe(3);
        savedJob.Label.ShouldStartWith("ImportMessagesJob-");

        queueMock.Verify(x => x.Enqueue(savedJob.Id, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task ReturnWithoutPersistingOrEnqueuing_WhenSourceFileDoesNotExist()
    {
        // Arrange
        var queueMock = new Mock<IBackgroundJobQueue>();
        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock.Setup(x => x.FileExists("/tmp/missing.json")).Returns(false);
        var loggerMock = new Mock<ILogger<EnqueueImportMessagesCommandHandler>>();
        var sut = new EnqueueImportMessagesCommandHandler(new TestDbContextFactory(DbContext), queueMock.Object, fileSystemMock.Object, loggerMock.Object);

        var command = new EnqueueImportMessagesCommand(new ImportMessagesJobRequest(
            ConnectionConfig.CreateConnectionString("Endpoint=sb://tests/"),
            new ResourceTarget.Queue("orders", SubQueue.Active),
            "/tmp/missing.json",
            string.Empty));

        // Act
        var result = await sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(Unit.Value);
        (await DbContext.Set<Job>().CountAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
        queueMock.Verify(x => x.Enqueue(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

