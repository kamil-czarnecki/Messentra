using Bunit;
using ElectronNET.API.Entities;
using Messentra.Features.Layout.Components;
using Messentra.Features.Layout.State;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Extensions;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Layout.Components;

public sealed class UpdateNotificationShould : ComponentTestBase
{
    private readonly FakeAutoUpdaterService _fakeAutoUpdater;

    public UpdateNotificationShould()
    {
        _fakeAutoUpdater = Services.GetRequiredService<FakeAutoUpdaterService>();
    }

    [Fact]
    public void RenderCurrentVersion_FromState()
    {
        // Arrange
        var state = GetState<AutoUpdateState>();
        state.SetState(state.Value with { CurrentVersion = "v1.2.3" });

        // Act
        var cut = Render<UpdateNotification>();

        // Assert
        cut.Markup.ShouldContain("v1.2.3");
    }

    [Fact]
    public void RenderMudTextWithCaptionTypo()
    {
        // Arrange
        var state = GetState<AutoUpdateState>();
        state.SetState(state.Value with { CurrentVersion = "v1.0.0" });

        // Act
        var cut = Render<UpdateNotification>();

        // Assert
        var text = cut.FindComponent<MudText>();
        text.Instance.Typo.ShouldBe(Typo.caption);
        text.Instance.Align.ShouldBe(Align.Center);
    }

    [Fact]
    public void DispatchRunAutoUpdaterAction_OnFirstRender()
    {
        // Arrange & Act
        Render<UpdateNotification>();

        // Assert
        MockDispatcher.Verify(d => d.Dispatch(new RunAutoUpdaterAction()), Times.Once);
    }

    [Fact]
    public void NotDispatchRunAutoUpdaterAction_OnSubsequentRenders()
    {
        // Arrange
        Render<UpdateNotification>();
        MockDispatcher.Invocations.Clear();

        // Act — trigger re-render by pushing new state
        var state = GetState<AutoUpdateState>();
        state.SetState(state.Value with { CurrentVersion = "v2.0.0" });

        // Assert
        MockDispatcher.Verify(d => d.Dispatch(new RunAutoUpdaterAction()), Times.Never);
    }

    [Fact]
    public void DispatchUpdateAvailableAction_WhenUpdateAvailableEventRaised()
    {
        // Arrange
        Render<UpdateNotification>();
        var updateInfo = new UpdateInfo { Version = "2.0.0" };

        // Act
        _fakeAutoUpdater.RaiseUpdateAvailable(updateInfo);

        // Assert
        MockDispatcher.Verify(d => d.Dispatch(new UpdateAvailableAction("2.0.0")), Times.Once);
    }

    [Fact]
    public void DispatchUpdateNotAvailableAction_WhenUpdateNotAvailableEventRaised()
    {
        // Arrange
        Render<UpdateNotification>();

        // Act
        _fakeAutoUpdater.RaiseUpdateNotAvailable();

        // Assert
        MockDispatcher.Verify(d => d.Dispatch(new UpdateNotAvailableAction()), Times.Once);
    }

    [Fact]
    public void DispatchUpdateReadyToInstallAction_WhenUpdateDownloadedEventRaised()
    {
        // Arrange
        Render<UpdateNotification>();
        var updateInfo = new UpdateInfo { Version = "2.0.0" };

        // Act
        _fakeAutoUpdater.RaiseUpdateDownloaded(updateInfo);

        // Assert
        MockDispatcher.Verify(d => d.Dispatch(new UpdateReadyToInstallAction()), Times.Once);
    }

    [Fact]
    public void DispatchUpdateDownloadProgressAction_WhenDownloadProgressEventRaised()
    {
        // Arrange
        Render<UpdateNotification>();
        var progressInfo = new ProgressInfo { Percent = 42.5 };

        // Act
        _fakeAutoUpdater.RaiseDownloadProgress(progressInfo);

        // Assert
        MockDispatcher.Verify(d => d.Dispatch(new UpdateDownloadProgressAction(42.5)), Times.Once);
    }

    [Fact]
    public void DispatchAutoUpdateErrorAction_WhenErrorEventRaised()
    {
        // Arrange
        Render<UpdateNotification>();

        // Act
        _fakeAutoUpdater.RaiseError("Something went wrong");

        // Assert
        MockDispatcher.Verify(d => d.Dispatch(new AutoUpdateErrorAction("Something went wrong")), Times.Once);
    }

    [Fact]
    public void UnsubscribeFromEvents_WhenDisposed()
    {
        // Arrange
        var cut = Render<UpdateNotification>();
        cut.Instance.Dispose();
        MockDispatcher.Invocations.Clear();

        // Act — raise events after disposal
        _fakeAutoUpdater.RaiseUpdateAvailable(new UpdateInfo { Version = "2.0.0" });
        _fakeAutoUpdater.RaiseUpdateNotAvailable();
        _fakeAutoUpdater.RaiseUpdateDownloaded(new UpdateInfo { Version = "2.0.0" });
        _fakeAutoUpdater.RaiseDownloadProgress(new ProgressInfo { Percent = 100 });
        _fakeAutoUpdater.RaiseError("error after dispose");

        // Assert — no actions dispatched after disposal
        MockDispatcher.Verify(d => d.Dispatch(It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public void SubscribeToAllEvents_OnFirstRender()
    {
        // Act
        Render<UpdateNotification>();

        // Assert — raising each event triggers a dispatch (confirms subscription)
        _fakeAutoUpdater.RaiseUpdateAvailable(new UpdateInfo { Version = "1.0.0" });
        _fakeAutoUpdater.RaiseUpdateNotAvailable();
        _fakeAutoUpdater.RaiseUpdateDownloaded(new UpdateInfo { Version = "1.0.0" });
        _fakeAutoUpdater.RaiseDownloadProgress(new ProgressInfo { Percent = 50 });
        _fakeAutoUpdater.RaiseError("test error");

        MockDispatcher.Verify(d => d.Dispatch(It.IsAny<object>()), Times.Exactly(6)); // RunAutoUpdater + 5 events
    }

    // --- Update available ---

    [Fact]
    public void RenderUpdateAvailableButton_WhenUpdateIsAvailable()
    {
        // Arrange
        var state = GetState<AutoUpdateState>();
        state.SetState(state.Value with
        {
            CurrentVersion = "v1.0.0",
            IsUpdateAvailable = true,
            AvailableVersion = "v2.0.0",
            IsDownloading = false,
            IsReadyToInstall = false
        });

        // Act
        var cut = Render<UpdateNotification>();

        // Assert
        cut.FindComponent<MudIconButton>().Instance.Icon.ShouldBe(Icons.Material.Outlined.NewReleases);
    }

    [Fact]
    public void NotRenderUpdateAvailableButton_WhenDownloading()
    {
        // Arrange
        var state = GetState<AutoUpdateState>();
        state.SetState(state.Value with
        {
            CurrentVersion = "v1.0.0",
            IsUpdateAvailable = true,
            IsDownloading = true,
            IsReadyToInstall = false
        });

        // Act
        var cut = Render<UpdateNotification>();

        // Assert — no icon buttons at all (no download, no install)
        cut.FindComponents<MudIconButton>().ShouldBeEmpty();
    }

    [Fact]
    public void NotRenderUpdateAvailableButton_WhenReadyToInstall()
    {
        // Arrange
        var state = GetState<AutoUpdateState>();
        state.SetState(state.Value with
        {
            CurrentVersion = "v1.0.0",
            IsUpdateAvailable = true,
            IsReadyToInstall = true
        });

        // Act
        var cut = Render<UpdateNotification>();

        // Assert — install button shown, but NOT the download button
        cut.FindComponents<MudIconButton>()
            .ShouldNotContain(b => b.Instance.Icon == Icons.Material.Outlined.NewReleases);
    }

    [Fact]
    public void DispatchDownloadUpdateAction_WhenDownloadButtonClicked()
    {
        // Arrange
        var state = GetState<AutoUpdateState>();
        state.SetState(state.Value with
        {
            CurrentVersion = "v1.0.0",
            IsUpdateAvailable = true,
            AvailableVersion = "v2.0.0",
            IsDownloading = false,
            IsReadyToInstall = false
        });
        var cut = Render<UpdateNotification>();

        // Act
        cut.Find("button").Click();

        // Assert
        MockDispatcher.Verify(d => d.Dispatch(new DownloadUpdateAction()), Times.Once);
    }

    // --- Error ---

    [Fact]
    public void RenderErrorAlert_WhenErrorMessageSet()
    {
        // Arrange
        var state = GetState<AutoUpdateState>();
        state.SetState(state.Value with { ErrorMessage = "Update failed" });

        // Act
        var cut = Render<UpdateNotification>();

        // Assert
        cut.FindComponents<MudAlert>().ShouldNotBeEmpty();
        cut.Markup.ShouldContain("Update failed");
    }

    [Fact]
    public void NotRenderErrorAlert_WhenNoErrorMessage()
    {
        // Arrange & Act
        var cut = Render<UpdateNotification>();

        // Assert
        cut.FindComponents<MudAlert>().ShouldBeEmpty();
    }

    [Fact]
    public void DispatchDismissUpdateErrorAction_WhenErrorAlertClosed()
    {
        // Arrange
        var state = GetState<AutoUpdateState>();
        state.SetState(state.Value with { ErrorMessage = "Update failed" });
        var cut = Render<UpdateNotification>();

        // Act — click the close button inside the alert (CurrentVersion is null so it's the only button)
        cut.Find("button").Click();

        // Assert
        MockDispatcher.Verify(d => d.Dispatch(new DismissUpdateErrorAction()), Times.Once);
    }

    // --- Downloading ---

    [Fact]
    public void RenderProgressCircular_WhenDownloading()
    {
        // Arrange
        var state = GetState<AutoUpdateState>();
        state.SetState(state.Value with
        {
            CurrentVersion = "v1.0.0",
            IsDownloading = true,
            DownloadProgress = 65.0
        });

        // Act
        var cut = Render<UpdateNotification>();

        // Assert
        cut.FindComponent<MudProgressCircular>().Instance.GetState(x => x.Value).ShouldBe(65.0);
    }

    [Fact]
    public void NotRenderProgressCircular_WhenNotDownloading()
    {
        // Arrange & Act
        var cut = Render<UpdateNotification>();

        // Assert
        cut.FindComponents<MudProgressCircular>().ShouldBeEmpty();
    }

    // --- Ready to install ---

    [Fact]
    public void RenderInstallButton_WhenReadyToInstall()
    {
        // Arrange
        var state = GetState<AutoUpdateState>();
        state.SetState(state.Value with
        {
            CurrentVersion = "v1.0.0",
            IsReadyToInstall = true
        });

        // Act
        var cut = Render<UpdateNotification>();

        // Assert
        cut.FindComponent<MudIconButton>().Instance.Icon.ShouldBe(Icons.Material.Outlined.SystemUpdateAlt);
    }

    [Fact]
    public void DispatchInstallUpdateAction_WhenInstallButtonClicked()
    {
        // Arrange
        var state = GetState<AutoUpdateState>();
        state.SetState(state.Value with
        {
            CurrentVersion = "v1.0.0",
            IsReadyToInstall = true
        });
        var cut = Render<UpdateNotification>();

        // Act
        cut.Find("button").Click();

        // Assert
        MockDispatcher.Verify(d => d.Dispatch(new InstallUpdateAction()), Times.Once);
    }
}


