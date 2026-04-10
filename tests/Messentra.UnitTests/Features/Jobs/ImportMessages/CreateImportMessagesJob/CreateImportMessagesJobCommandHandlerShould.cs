using System.Security.Cryptography;
using Messentra.Domain;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Jobs;
using Messentra.Features.Jobs.ImportMessages;
using Messentra.Features.Jobs.ImportMessages.CreateImportMessagesJob;
using Messentra.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Jobs.ImportMessages.CreateImportMessagesJob;

public sealed class CreateImportMessagesJobCommandHandlerShould : InMemoryDbTestBase
{
    [Fact]
    public async Task PersistImportJobAndReturnJobListItem_WhenHandleCalled()
    {
        // Arrange
        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock.Setup(x => x.FileExists("/tmp/import.json")).Returns(true);
        var payload = "[{\"message\":\"body\",\"properties\":{},\"applicationProperties\":{}}]"u8.ToArray();
        fileSystemMock.Setup(x => x.OpenRead("/tmp/import.json")).Returns(() => new MemoryStream(payload));
        var loggerMock = new Mock<ILogger<CreateImportMessagesJobCommandHandler>>();
        var sut = new CreateImportMessagesJobCommandHandler(new TestDbContextFactory(DbContext), fileSystemMock.Object, loggerMock.Object);
        var expectedHash = Convert.ToHexString(SHA256.HashData(payload));

        var request = new ImportMessagesJobRequest(
            ConnectionConfig.CreateConnectionString("Endpoint=sb://tests/"),
            new ResourceTarget.Queue("orders", SubQueue.Active),
            "/tmp/import.json",
            string.Empty);

        var command = new CreateImportMessagesJobCommand(request);

        // Act
        var result = await sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
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

        result.Id.ShouldBe(savedJob.Id);
        result.Label.ShouldBe(savedJob.Label);
        result.Status.ShouldBe(JobStatus.Queued);
    }

    [Fact]
    public async Task ThrowInvalidOperationException_WhenSourceFileDoesNotExist()
    {
        // Arrange
        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock.Setup(x => x.FileExists("/tmp/missing.json")).Returns(false);
        var loggerMock = new Mock<ILogger<CreateImportMessagesJobCommandHandler>>();
        var sut = new CreateImportMessagesJobCommandHandler(new TestDbContextFactory(DbContext), fileSystemMock.Object, loggerMock.Object);

        var command = new CreateImportMessagesJobCommand(new ImportMessagesJobRequest(
            ConnectionConfig.CreateConnectionString("Endpoint=sb://tests/"),
            new ResourceTarget.Queue("orders", SubQueue.Active),
            "/tmp/missing.json",
            string.Empty));

        // Act
        var act = () => sut.Handle(command, TestContext.Current.CancellationToken).AsTask();

        // Assert
        await act.ShouldThrowAsync<InvalidOperationException>();
        (await DbContext.Set<Job>().CountAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
    }
}

