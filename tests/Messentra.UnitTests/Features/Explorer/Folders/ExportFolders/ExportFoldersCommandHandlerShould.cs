using System.Text.Json;
using Messentra.Domain;
using Messentra.Features.Explorer.Folders;
using Messentra.Features.Explorer.Folders.ExportFolders;
using Messentra.Infrastructure;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Explorer.Folders.ExportFolders;

public sealed class ExportFoldersCommandHandlerShould : InMemoryDbTestBase
{
    private readonly Mock<IFileSystem> _fileSystem = new();
    private readonly ExportFoldersCommandHandler _sut;

    public ExportFoldersCommandHandlerShould()
    {
        _sut = new ExportFoldersCommandHandler(DbContextFactory, _fileSystem.Object);
    }

    [Fact]
    public async Task WriteJsonWithRelativeResourcePaths()
    {
        // Arrange
        var connection = await SeedConnectionAsync();
        var namespacePrefix = "https://test.servicebus.windows.net";

        var folder = new Folder { ConnectionId = connection.Id, Name = "Critical" };
        await DbContext.Set<Folder>().AddAsync(folder, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        await DbContext.Set<FolderResource>().AddAsync(new FolderResource { FolderId = folder.Id, ResourceUrl = $"{namespacePrefix}/orders-queue" }, TestContext.Current.CancellationToken);
        await DbContext.Set<FolderResource>().AddAsync(new FolderResource { FolderId = folder.Id, ResourceUrl = $"{namespacePrefix}/topics/my-topic/subscriptions/sub1" }, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var outputStream = new NonDisposingMemoryStream();
        _fileSystem.Setup(x => x.OpenWrite("/tmp/out.json", It.IsAny<int>(), It.IsAny<bool>()))
                   .Returns(outputStream);

        var command = new ExportFoldersCommand(connection.Id, connection.ConnectionConfig, "/tmp/out.json");

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        outputStream.Position = 0;
        var items = JsonSerializer.Deserialize<List<FolderExportItem>>(outputStream)!;
        items.Count.ShouldBe(1);
        items[0].Name.ShouldBe("Critical");
        items[0].Resources.ShouldContain("orders-queue");
        items[0].Resources.ShouldContain("topics/my-topic/subscriptions/sub1");
    }

    [Fact]
    public async Task WriteEmptyJsonArray_WhenNoFoldersExist()
    {
        // Arrange
        var connection = await SeedConnectionAsync();
        var outputStream = new NonDisposingMemoryStream();
        _fileSystem.Setup(x => x.OpenWrite("/tmp/out.json", It.IsAny<int>(), It.IsAny<bool>()))
                   .Returns(outputStream);

        var command = new ExportFoldersCommand(connection.Id, connection.ConnectionConfig, "/tmp/out.json");

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        outputStream.Position = 0;
        var items = JsonSerializer.Deserialize<List<FolderExportItem>>(outputStream)!;
        items.ShouldBeEmpty();
    }

    private sealed class NonDisposingMemoryStream : MemoryStream
    {
        protected override void Dispose(bool disposing) { }
    }
}
