using Messentra.Infrastructure.AzureServiceBus;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Infrastructure.AzureServiceBus;

public sealed class AzureServiceBusMessagePeekContextShould
{
    private static AzureServiceBusMessagePeekContext CreateSut() => new();

    [Fact]
    public async Task Complete_ThrowsNotSupported()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var action = () => sut.Complete(CancellationToken.None);

        // Assert
        await action.ShouldThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task Abandon_ThrowsNotSupported()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var action = () => sut.Abandon(CancellationToken.None);

        // Assert
        await action.ShouldThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task DeadLetter_ThrowsNotSupported()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var action = () => sut.DeadLetter(CancellationToken.None);

        // Assert
        await action.ShouldThrowAsync<NotSupportedException>();
    }
}
