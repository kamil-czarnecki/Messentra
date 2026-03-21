using System.Security.Cryptography;
using System.Text;
using Azure.Identity;
using Messentra.Infrastructure;
using Messentra.Infrastructure.AzureServiceBus;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Infrastructure.AzureServiceBus;

public sealed class AuthenticationRecordStoreShould
{
    private readonly Mock<IFileSystem> _fileSystem = new();
    private readonly AuthenticationRecordStore _sut;

    public AuthenticationRecordStoreShould()
    {
        _sut = new AuthenticationRecordStore(_fileSystem.Object);
    }

    [Fact]
    public void GetWhenFileDoesNotExistReturnsNull()
    {
        // Arrange
        var key = $"missing-{Guid.NewGuid():N}";
        var expectedPath = GetRecordPath(key);
        _fileSystem.Setup(x => x.FileExists(expectedPath)).Returns(false);

        // Act
        var record = _sut.Get(key);

        // Assert
        record.ShouldBeNull();
        _fileSystem.Verify(x => x.OpenRead(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void SaveWhenCalledSerializesAuthenticationRecord()
    {
        // Arrange
        var key = $"roundtrip-{Guid.NewGuid():N}";
        var expectedPath = GetRecordPath(key);
        var expectedRoot = GetRootPath();
        var record = IdentityModelFactory.AuthenticationRecord(
            username: "test-user",
            authority: "https://login.microsoftonline.com/common",
            homeAccountId: "home-account-id",
            tenantId: "tenant-id",
            clientId: "client-id");
        var writeStream = new NonDisposingMemoryStream();

        _fileSystem.Setup(x => x.CreateDirectory(expectedRoot));
        _fileSystem.Setup(x => x.OpenWrite(expectedPath)).Returns(writeStream);

        // Act
        _sut.Save(key, record);

        // Assert
        _fileSystem.Verify(x => x.CreateDirectory(expectedRoot), Times.Once);
        _fileSystem.Verify(x => x.OpenWrite(expectedPath), Times.Once);
        writeStream.Position = 0;
        var loaded = AuthenticationRecord.Deserialize(writeStream, CancellationToken.None);
        loaded.ShouldNotBeNull();
        loaded.Username.ShouldBe(record.Username);
        loaded.Authority.ShouldBe(record.Authority);
        loaded.HomeAccountId.ShouldBe(record.HomeAccountId);
        loaded.TenantId.ShouldBe(record.TenantId);
        loaded.ClientId.ShouldBe(record.ClientId);
    }

    [Fact]
    public void GetWhenFileContainsSerializedRecordReturnsRecord()
    {
        // Arrange
        var key = $"record-{Guid.NewGuid():N}";
        var expectedPath = GetRecordPath(key);
        var record = IdentityModelFactory.AuthenticationRecord(
            username: "test-user",
            authority: "https://login.microsoftonline.com/common",
            homeAccountId: "home-account-id",
            tenantId: "tenant-id",
            clientId: "client-id");
        var recordBytes = SerializeRecord(record);

        _fileSystem.Setup(x => x.FileExists(expectedPath)).Returns(true);
        _fileSystem.Setup(x => x.OpenRead(expectedPath)).Returns(() => new MemoryStream(recordBytes));

        // Act
        var loaded = _sut.Get(key);

        // Assert
        loaded.ShouldNotBeNull();
        loaded.Username.ShouldBe(record.Username);
        loaded.Authority.ShouldBe(record.Authority);
        loaded.HomeAccountId.ShouldBe(record.HomeAccountId);
        loaded.TenantId.ShouldBe(record.TenantId);
        loaded.ClientId.ShouldBe(record.ClientId);
    }

    [Fact]
    public void GetWhenFileIsCorruptedReturnsNullAndDeletesCorruptedFilePath()
    {
        // Arrange
        var key = $"corrupted-{Guid.NewGuid():N}";
        var expectedPath = GetRecordPath(key);

        _fileSystem.Setup(x => x.FileExists(expectedPath)).Returns(true);
        _fileSystem.Setup(x => x.OpenRead(expectedPath)).Returns(() => new MemoryStream([1, 2, 3, 4, 5]));
        _fileSystem.Setup(x => x.Delete(expectedPath));

        // Act
        var record = _sut.Get(key);

        // Assert
        record.ShouldBeNull();
        _fileSystem.Verify(x => x.Delete(expectedPath), Times.Once);
    }

    [Fact]
    public void ClearAllWhenRootExistsDeletesAllBinFiles()
    {
        // Arrange
        var expectedRoot = GetRootPath();
        var file1 = Path.Combine(expectedRoot, "a.bin");
        var file2 = Path.Combine(expectedRoot, "b.bin");

        _fileSystem.Setup(x => x.DirectoryExists(expectedRoot)).Returns(true);
        _fileSystem
            .Setup(x => x.EnumerateFiles(expectedRoot, "*.bin", SearchOption.TopDirectoryOnly))
            .Returns([file1, file2]);

        // Act
        _sut.ClearAll();

        // Assert
        _fileSystem.Verify(x => x.DirectoryExists(expectedRoot), Times.Once);
        _fileSystem.Verify(
            x => x.EnumerateFiles(expectedRoot, "*.bin", SearchOption.TopDirectoryOnly),
            Times.Once);
        _fileSystem.Verify(x => x.Delete(file1), Times.Once);
        _fileSystem.Verify(x => x.Delete(file2), Times.Once);
    }

    [Fact]
    public void ClearAllWhenRootDoesNotExistDoesNotEnumerateOrDeleteFiles()
    {
        // Arrange
        var expectedRoot = GetRootPath();
        _fileSystem.Setup(x => x.DirectoryExists(expectedRoot)).Returns(false);

        // Act
        _sut.ClearAll();

        // Assert
        _fileSystem.Verify(x => x.DirectoryExists(expectedRoot), Times.Once);
        _fileSystem.Verify(
            x => x.EnumerateFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>()),
            Times.Never);
        _fileSystem.Verify(x => x.Delete(It.IsAny<string>()), Times.Never);
    }

    private static byte[] SerializeRecord(AuthenticationRecord record)
    {
        using var stream = new MemoryStream();
        record.Serialize(stream);
        return stream.ToArray();
    }

    private sealed class NonDisposingMemoryStream : MemoryStream
    {
        protected override void Dispose(bool disposing)
        {
            // Keep buffer accessible for assertions after store disposes the stream.
        }
    }

    private static string GetRecordPath(string key)
    {
        var root = GetRootPath();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
        return Path.Combine(root, $"{hash}.bin");
    }

    private static string GetRootPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Messentra",
            "AuthRecords");
}

