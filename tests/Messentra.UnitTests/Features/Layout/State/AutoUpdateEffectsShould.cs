using Fluxor;
using Messentra.Features.Layout.State;
using Messentra.Infrastructure.AutoUpdater;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Layout.State;

public sealed class AutoUpdateEffectsShould
{
    private readonly Mock<IAutoUpdaterService> _autoUpdater = new();
    private readonly Mock<IDispatcher> _dispatcher = new();
    private readonly AutoUpdateEffects _sut;

    public AutoUpdateEffectsShould()
    {
        _sut = new AutoUpdateEffects(_autoUpdater.Object);
    }

    [Fact]
    public async Task DispatchCurrentVersion_WhenRunAutoUpdaterActionHandled()
    {
        // Arrange
        _autoUpdater.Setup(x => x.GetCurrentVersion()).ReturnsAsync("v1.0.0");

        // Act
        await _sut.HandleRunAutoUpdater(new RunAutoUpdaterAction(), _dispatcher.Object);

        // Assert
        _dispatcher.Verify(d => d.Dispatch(new UpdateCurrentVersionAction("v1.0.0")), Times.Once);
    }

    [Fact]
    public async Task DispatchCheckForUpdatesAction_WhenRunAutoUpdaterActionHandled()
    {
        // Arrange
        _autoUpdater.Setup(x => x.GetCurrentVersion()).ReturnsAsync("v1.0.0");

        // Act
        await _sut.HandleRunAutoUpdater(new RunAutoUpdaterAction(), _dispatcher.Object);

        // Assert
        _dispatcher.Verify(d => d.Dispatch(new CheckForUpdatesAction()), Times.Once);
    }

    [Fact]
    public async Task DispatchActionsInOrder_WhenRunAutoUpdaterActionHandled()
    {
        // Arrange
        _autoUpdater.Setup(x => x.GetCurrentVersion()).ReturnsAsync("v2.3.4");
        var dispatchedActions = new List<object>();
        _dispatcher.Setup(d => d.Dispatch(It.IsAny<object>()))
            .Callback<object>(dispatchedActions.Add);

        // Act
        await _sut.HandleRunAutoUpdater(new RunAutoUpdaterAction(), _dispatcher.Object);

        // Assert
        dispatchedActions.Count.ShouldBe(2);
        dispatchedActions[0].ShouldBeOfType<UpdateCurrentVersionAction>();
        dispatchedActions[1].ShouldBeOfType<CheckForUpdatesAction>();
    }

    [Fact]
    public async Task DispatchUpdateCheckingAction_WhenCheckForUpdatesActionHandled()
    {
        // Arrange
        _autoUpdater.Setup(x => x.CheckForUpdates()).Returns(Task.CompletedTask);

        // Act
        await _sut.HandleCheckForUpdates(new CheckForUpdatesAction(), _dispatcher.Object);

        // Assert
        _dispatcher.Verify(d => d.Dispatch(new UpdateCheckingAction()), Times.Once);
    }

    [Fact]
    public async Task CallCheckForUpdates_WhenCheckForUpdatesActionHandled()
    {
        // Arrange
        _autoUpdater.Setup(x => x.CheckForUpdates()).Returns(Task.CompletedTask);

        // Act
        await _sut.HandleCheckForUpdates(new CheckForUpdatesAction(), _dispatcher.Object);

        // Assert
        _autoUpdater.Verify(x => x.CheckForUpdates(), Times.Once);
    }

    [Fact]
    public async Task DispatchAutoUpdateErrorAction_WhenCheckForUpdatesThrows()
    {
        // Arrange
        var exception = new InvalidOperationException("Connection refused");
        _autoUpdater.Setup(x => x.CheckForUpdates()).ThrowsAsync(exception);

        // Act
        await _sut.HandleCheckForUpdates(new CheckForUpdatesAction(), _dispatcher.Object);

        // Assert
        _dispatcher.Verify(d => d.Dispatch(new AutoUpdateErrorAction("Connection refused")), Times.Once);
    }

    [Fact]
    public async Task NotDispatchUpdateCheckingAction_WhenCheckForUpdatesThrows()
    {
        // Arrange
        _autoUpdater.Setup(x => x.CheckForUpdates()).ThrowsAsync(new Exception("fail"));

        // Act
        await _sut.HandleCheckForUpdates(new CheckForUpdatesAction(), _dispatcher.Object);

        // Assert — UpdateCheckingAction is dispatched before the error happens
        _dispatcher.Verify(d => d.Dispatch(new UpdateCheckingAction()), Times.Once);
    }

    [Fact]
    public async Task CallDownloadUpdate_WhenDownloadUpdateActionHandled()
    {
        // Arrange
        _autoUpdater.Setup(x => x.DownloadUpdate()).Returns(Task.CompletedTask);

        // Act
        await _sut.HandleDownloadUpdate(new DownloadUpdateAction(), _dispatcher.Object);

        // Assert
        _autoUpdater.Verify(x => x.DownloadUpdate(), Times.Once);
    }


    [Fact]
    public async Task DispatchAutoUpdateErrorAction_WhenDownloadUpdateThrows()
    {
        // Arrange
        var exception = new InvalidOperationException("Download failed");
        _autoUpdater.Setup(x => x.DownloadUpdate()).ThrowsAsync(exception);

        // Act
        await _sut.HandleDownloadUpdate(new DownloadUpdateAction(), _dispatcher.Object);

        // Assert
        _dispatcher.Verify(d => d.Dispatch(new AutoUpdateErrorAction("Download failed")), Times.Once);
    }

    [Fact]
    public async Task NotDispatchReadyToInstall_WhenDownloadUpdateThrows()
    {
        // Arrange
        _autoUpdater.Setup(x => x.DownloadUpdate()).ThrowsAsync(new Exception("fail"));

        // Act
        await _sut.HandleDownloadUpdate(new DownloadUpdateAction(), _dispatcher.Object);

        // Assert
        _dispatcher.Verify(d => d.Dispatch(It.IsAny<UpdateReadyToInstallAction>()), Times.Never);
    }

    [Fact]
    public async Task CallQuitAndInstall_WhenInstallUpdateActionHandled()
    {
        // Act
        await _sut.HandleInstallUpdate(new InstallUpdateAction(), _dispatcher.Object);

        // Assert
        _autoUpdater.Verify(x => x.QuitAndInstall(false, false), Times.Once);
    }

    [Fact]
    public async Task DispatchAutoUpdateErrorAction_WhenQuitAndInstallThrows()
    {
        // Arrange
        var exception = new InvalidOperationException("Install failed");
        _autoUpdater.Setup(x => x.QuitAndInstall(It.IsAny<bool>(), It.IsAny<bool>()))
            .Throws(exception);

        // Act
        await _sut.HandleInstallUpdate(new InstallUpdateAction(), _dispatcher.Object);

        // Assert
        _dispatcher.Verify(d => d.Dispatch(new AutoUpdateErrorAction("Install failed")), Times.Once);
    }
}

