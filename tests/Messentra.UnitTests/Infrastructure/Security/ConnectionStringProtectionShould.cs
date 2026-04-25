using System.Security.Cryptography;
using Messentra.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Infrastructure.Security;

public sealed class ConnectionStringProtectionShould
{
    public ConnectionStringProtectionShould()
    {
        ConnectionStringProtection.Initialize(new EphemeralDataProtectionProvider());
    }

    [Fact]
    public void ProtectAndUnprotect_RoundTrip_ReturnOriginalValue()
    {
        // Arrange
        const string plainText = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=abc123";

        // Act
        var cipherText = ConnectionStringProtection.Protect(plainText);
        var result = ConnectionStringProtection.Unprotect(cipherText);

        // Assert
        result.ShouldBe(plainText);
    }

    [Fact]
    public void Protect_ReturnsDifferentValueThanInput()
    {
        // Arrange
        const string plainText = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=abc123";

        // Act
        var cipherText = ConnectionStringProtection.Protect(plainText);

        // Assert
        cipherText.ShouldNotBe(plainText);
    }

    [Fact]
    public void Protect_WhenCalledTwiceWithSameInput_ReturnsDifferentCipherTexts()
    {
        // Arrange
        const string plainText = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=abc123";

        // Act
        var first = ConnectionStringProtection.Protect(plainText);
        var second = ConnectionStringProtection.Protect(plainText);

        // Assert
        first.ShouldNotBe(second);
    }

    [Fact]
    public void Unprotect_WhenGivenInvalidCipherText_ThrowsCryptographicException()
    {
        // Arrange
        const string invalidCipher = "not-valid-cipher-text";

        // Act
        var act = () => ConnectionStringProtection.Unprotect(invalidCipher);

        // Assert
        act.ShouldThrow<CryptographicException>();
    }

    [Fact]
    public void Initialize_WhenCalledMultipleTimes_IsIdempotent()
    {
        // Arrange
        const string plainText = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=Key;SharedAccessKey=xyz";
        var cipherText = ConnectionStringProtection.Protect(plainText);

        // Act — re-initialize with a different provider; should be a no-op
        ConnectionStringProtection.Initialize(new EphemeralDataProtectionProvider());
        var result = ConnectionStringProtection.Unprotect(cipherText);

        // Assert — original key still in use
        result.ShouldBe(plainText);
    }
}
