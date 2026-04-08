using System.Text.Json;
using Messentra.Domain;
using Messentra.Features.Explorer.Folders;
using Messentra.Features.Explorer.Folders.ImportFolders;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Explorer.Folders.ImportFolders;

public sealed class ImportFoldersCommandHandlerShould : InMemoryDbTestBase
{
    private const string NamespacePrefix = "https://test.servicebus.windows.net";

    private readonly ImportFoldersCommandHandler _sut;

    public ImportFoldersCommandHandlerShould()
    {
        _sut = new ImportFoldersCommandHandler(DbContextFactory);
    }

    private static string BuildJson(params (string Name, string[] Resources)[] folders)
    {
        var items = folders.Select(f => new FolderExportItem(f.Name, f.Resources)).ToList();
        return JsonSerializer.Serialize(items);
    }

    [Fact]
    public async Task CreateNewFolders_WhenNoneExist()
    {
        // Arrange
        var connection = await SeedConnectionAsync();
        var json = BuildJson(
            ("Orders", ["orders-queue"]),
            ("Topics", ["topics/my-topic/subscriptions/sub1"]));

        var command = new ImportFoldersCommand(connection.Id, connection.ConnectionConfig, json);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var folders = await DbContext.Set<Folder>()
            .Include(f => f.Resources)
            .Where(f => f.ConnectionId == connection.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        folders.Count.ShouldBe(2);

        var orders = folders.Single(f => f.Name == "Orders");
        orders.Resources.Count.ShouldBe(1);
        orders.Resources[0].ResourceUrl.ShouldBe($"{NamespacePrefix}/orders-queue");

        var topics = folders.Single(f => f.Name == "Topics");
        topics.Resources[0].ResourceUrl.ShouldBe($"{NamespacePrefix}/topics/my-topic/subscriptions/sub1");
    }

    [Fact]
    public async Task ReplaceExistingFolder_WhenSameNameExists()
    {
        // Arrange
        var connection = await SeedConnectionAsync();

        var existingFolder = new Folder { ConnectionId = connection.Id, Name = "Critical" };
        await DbContext.Set<Folder>().AddAsync(existingFolder, TestContext.Current.CancellationToken);
        await DbContext.Set<FolderResource>().AddAsync(new FolderResource { FolderId = existingFolder.Id, ResourceUrl = $"{NamespacePrefix}/old-queue" }, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var json = BuildJson(("Critical", ["new-queue"]));
        var command = new ImportFoldersCommand(connection.Id, connection.ConnectionConfig, json);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var folders = await DbContext.Set<Folder>()
            .Include(f => f.Resources)
            .Where(f => f.ConnectionId == connection.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        folders.Count.ShouldBe(1);
        folders[0].Name.ShouldBe("Critical");
        folders[0].Resources.Count.ShouldBe(1);
        folders[0].Resources[0].ResourceUrl.ShouldBe($"{NamespacePrefix}/new-queue");
    }

    [Fact]
    public async Task ReplaceExistingFolder_CaseInsensitiveNameMatch()
    {
        // Arrange
        var connection = await SeedConnectionAsync();

        var existingFolder = new Folder { ConnectionId = connection.Id, Name = "my folder" };
        await DbContext.Set<Folder>().AddAsync(existingFolder, TestContext.Current.CancellationToken);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var json = BuildJson(("MY FOLDER", ["queue-1"]));
        var command = new ImportFoldersCommand(connection.Id, connection.ConnectionConfig, json);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var folders = await DbContext.Set<Folder>()
            .Where(f => f.ConnectionId == connection.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        folders.Count.ShouldBe(1);
        folders[0].Name.ShouldBe("MY FOLDER");
    }
}
