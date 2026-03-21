using Mediator;
using Messentra.Features.Settings.Cache;
using Messentra.Infrastructure.AzureServiceBus;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Settings.Cache;

public sealed class ClearCacheCommandHandlerShould
{
    private readonly Mock<IAuthenticationRecordStore> _authenticationRecordStore = new();
    private readonly Mock<ICacheClearConfirmationService> _confirmationService = new();
    private readonly Mock<IApplicationLifecycleService> _applicationLifecycleService = new();
    private readonly ClearCacheCommandHandler _sut;

    public ClearCacheCommandHandlerShould()
    {
        _sut = new ClearCacheCommandHandler(
            _authenticationRecordStore.Object,
            _confirmationService.Object,
            _applicationLifecycleService.Object);
    }

    [Fact]
    public async Task HandleWhenUserConfirmsClearsCacheAndRestartsApplication()
    {
        // Arrange
        _confirmationService
            .Setup(x => x.ConfirmClearAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.Handle(new ClearCacheCommand(), CancellationToken.None);

        // Assert
        result.ShouldBe(Unit.Value);
        _authenticationRecordStore.Verify(x => x.ClearAll(), Times.Once);
        _applicationLifecycleService.Verify(x => x.Relaunch(), Times.Once);
        _applicationLifecycleService.Verify(x => x.Exit(), Times.Once);
    }

    [Fact]
    public async Task HandleWhenUserCancelsDoesNotClearCacheOrRestartApplication()
    {
        // Arrange
        _confirmationService
            .Setup(x => x.ConfirmClearAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.Handle(new ClearCacheCommand(), CancellationToken.None);

        // Assert
        result.ShouldBe(Unit.Value);
        _authenticationRecordStore.Verify(x => x.ClearAll(), Times.Never);
        _applicationLifecycleService.Verify(x => x.Relaunch(), Times.Never);
        _applicationLifecycleService.Verify(x => x.Exit(), Times.Never);
    }
}

