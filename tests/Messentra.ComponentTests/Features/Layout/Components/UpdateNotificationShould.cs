using Bunit;
using ElectronNET.API.Entities;
using Messentra.Features.Layout.Components;
using Messentra.Features.Layout.State;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
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
        // Arrange & Act
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
}


