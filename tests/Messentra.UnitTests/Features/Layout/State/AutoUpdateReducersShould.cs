using Messentra.Features.Layout.State;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Layout.State;

public sealed class AutoUpdateReducersShould
{
    private static AutoUpdateState DefaultState => new(
        CurrentVersion: null,
        IsChecking: false,
        IsUpdateAvailable: false,
        IsDownloading: false,
        IsReadyToInstall: false,
        DownloadProgress: 0,
        AvailableVersion: null,
        ErrorMessage: null);

    [Fact]
    public void SetCurrentVersion_WhenUpdateCurrentVersionActionDispatched()
    {
        // Arrange
        var state = DefaultState;
        var action = new UpdateCurrentVersionAction("v1.2.3");

        // Act
        var newState = AutoUpdateReducers.Reduce(state, action);

        // Assert
        newState.CurrentVersion.ShouldBe("v1.2.3");
    }

    [Fact]
    public void SetIsCheckingToTrue_WhenUpdateCheckingActionDispatched()
    {
        // Arrange
        var state = DefaultState;

        // Act
        var newState = AutoUpdateReducers.Reduce(state, new UpdateCheckingAction());

        // Assert
        newState.IsChecking.ShouldBeTrue();
    }

    [Fact]
    public void ClearErrorMessage_WhenUpdateCheckingActionDispatched()
    {
        // Arrange
        var state = DefaultState with { ErrorMessage = "Previous error" };

        // Act
        var newState = AutoUpdateReducers.Reduce(state, new UpdateCheckingAction());

        // Assert
        newState.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void ClearIsChecking_WhenUpdateAvailableActionDispatched()
    {
        // Arrange
        var state = DefaultState with { IsChecking = true };
        var action = new UpdateAvailableAction("v2.0.0");

        // Act
        var newState = AutoUpdateReducers.Reduce(state, action);

        // Assert
        newState.IsChecking.ShouldBeFalse();
    }

    [Fact]
    public void SetIsUpdateAvailableToTrue_WhenUpdateAvailableActionDispatched()
    {
        // Arrange
        var state = DefaultState;
        var action = new UpdateAvailableAction("v2.0.0");

        // Act
        var newState = AutoUpdateReducers.Reduce(state, action);

        // Assert
        newState.IsUpdateAvailable.ShouldBeTrue();
    }

    [Fact]
    public void StoreAvailableVersion_WhenUpdateAvailableActionDispatched()
    {
        // Arrange
        var state = DefaultState;
        var action = new UpdateAvailableAction("v2.0.0");

        // Act
        var newState = AutoUpdateReducers.Reduce(state, action);

        // Assert
        newState.AvailableVersion.ShouldBe("v2.0.0");
    }

    [Fact]
    public void ClearIsChecking_WhenUpdateNotAvailableActionDispatched()
    {
        // Arrange
        var state = DefaultState with { IsChecking = true };

        // Act
        var newState = AutoUpdateReducers.Reduce(state, new UpdateNotAvailableAction());

        // Assert
        newState.IsChecking.ShouldBeFalse();
    }

    [Fact]
    public void SetIsUpdateAvailableToFalse_WhenUpdateNotAvailableActionDispatched()
    {
        // Arrange
        var state = DefaultState with { IsUpdateAvailable = true };

        // Act
        var newState = AutoUpdateReducers.Reduce(state, new UpdateNotAvailableAction());

        // Assert
        newState.IsUpdateAvailable.ShouldBeFalse();
    }

    [Fact]
    public void SetIsDownloadingToTrue_WhenDownloadUpdateActionDispatched()
    {
        // Arrange
        var state = DefaultState;

        // Act
        var newState = AutoUpdateReducers.Reduce(state, new DownloadUpdateAction());

        // Assert
        newState.IsDownloading.ShouldBeTrue();
    }

    [Fact]
    public void ResetDownloadProgress_WhenDownloadUpdateActionDispatched()
    {
        // Arrange
        var state = DefaultState with { DownloadProgress = 42 };

        // Act
        var newState = AutoUpdateReducers.Reduce(state, new DownloadUpdateAction());

        // Assert
        newState.DownloadProgress.ShouldBe(0);
    }

    [Fact]
    public void UpdateDownloadProgress_WhenUpdateDownloadProgressActionDispatched()
    {
        // Arrange
        var state = DefaultState;
        var action = new UpdateDownloadProgressAction(73.5);

        // Act
        var newState = AutoUpdateReducers.Reduce(state, action);

        // Assert
        newState.DownloadProgress.ShouldBe(73.5);
    }

    [Fact]
    public void SetIsReadyToInstall_WhenUpdateReadyToInstallActionDispatched()
    {
        // Arrange
        var state = DefaultState with { IsDownloading = true };

        // Act
        var newState = AutoUpdateReducers.Reduce(state, new UpdateReadyToInstallAction());

        // Assert
        newState.IsReadyToInstall.ShouldBeTrue();
        newState.IsDownloading.ShouldBeFalse();
    }

    [Fact]
    public void SetDownloadProgressTo100_WhenUpdateReadyToInstallActionDispatched()
    {
        // Arrange
        var state = DefaultState with { DownloadProgress = 99 };

        // Act
        var newState = AutoUpdateReducers.Reduce(state, new UpdateReadyToInstallAction());

        // Assert
        newState.DownloadProgress.ShouldBe(100);
    }

    [Fact]
    public void StoreErrorMessage_WhenAutoUpdateErrorActionDispatched()
    {
        // Arrange
        var state = DefaultState with { IsChecking = true, IsDownloading = true };
        var action = new AutoUpdateErrorAction("Something went wrong");

        // Act
        var newState = AutoUpdateReducers.Reduce(state, action);

        // Assert
        newState.ErrorMessage.ShouldBe("Something went wrong");
        newState.IsChecking.ShouldBeFalse();
        newState.IsDownloading.ShouldBeFalse();
    }

    [Fact]
    public void ClearErrorMessage_WhenDismissUpdateErrorActionDispatched()
    {
        // Arrange
        var state = DefaultState with { ErrorMessage = "Something went wrong" };

        // Act
        var newState = AutoUpdateReducers.Reduce(state);

        // Assert
        newState.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void PreserveUnrelatedProperties_WhenUpdateCurrentVersionActionDispatched()
    {
        // Arrange
        var state = DefaultState with
        {
            IsChecking = true,
            IsUpdateAvailable = true,
            DownloadProgress = 50,
            AvailableVersion = "v1.1.0"
        };

        // Act
        var newState = AutoUpdateReducers.Reduce(state, new UpdateCurrentVersionAction("v1.0.0"));

        // Assert
        newState.IsChecking.ShouldBeTrue();
        newState.IsUpdateAvailable.ShouldBeTrue();
        newState.DownloadProgress.ShouldBe(50);
        newState.AvailableVersion.ShouldBe("v1.1.0");
    }
}

