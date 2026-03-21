using System.Security.Cryptography;
using System.Text;
using Azure.Identity;
using Messentra.Infrastructure.AzureServiceBus;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Infrastructure.AzureServiceBus;

public sealed class AuthenticationRecordStoreShould
{
    private readonly AuthenticationRecordStore _sut = new();

    [Fact]
    public void GetWhenFileDoesNotExistReturnsNull()
    {
        // Arrange
        var key = $"missing-{Guid.NewGuid():N}";
        var path = GetRecordPath(key);
        TryDelete(path);

        // Act
        var record = _sut.Get(key);

        // Assert
        record.ShouldBeNull();
    }

    [Fact]
    public void SaveThenGetReturnsAuthenticationRecord()
    {
        // Arrange
        var key = $"roundtrip-{Guid.NewGuid():N}";
        var path = GetRecordPath(key);
        TryDelete(path);
        var record = IdentityModelFactory.AuthenticationRecord(
            username: "test-user",
            authority: "https://login.microsoftonline.com/common",
            homeAccountId: "home-account-id",
            tenantId: "tenant-id",
            clientId: "client-id");

        // Act
        _sut.Save(key, record);
        var loaded = _sut.Get(key);

        // Assert
        loaded.ShouldNotBeNull();
        loaded.Username.ShouldBe(record.Username);
        loaded.Authority.ShouldBe(record.Authority);
        loaded.HomeAccountId.ShouldBe(record.HomeAccountId);
        loaded.TenantId.ShouldBe(record.TenantId);
        loaded.ClientId.ShouldBe(record.ClientId);

        TryDelete(path);
    }

    [Fact]
    public void GetWhenFileIsCorruptedReturnsNullAndDeletesFile()
    {
        // Arrange
        var key = $"corrupted-{Guid.NewGuid():N}";
        var path = GetRecordPath(key);
        TryDelete(path);
        File.WriteAllBytes(path, [1, 2, 3, 4, 5]);

        // Act
        var record = _sut.Get(key);

        // Assert
        record.ShouldBeNull();
        File.Exists(path).ShouldBeFalse();
    }

    private static string GetRecordPath(string key)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Messentra",
            "AuthRecords");

        Directory.CreateDirectory(root);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
        return Path.Combine(root, $"{hash}.bin");
    }

    private static void TryDelete(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}

