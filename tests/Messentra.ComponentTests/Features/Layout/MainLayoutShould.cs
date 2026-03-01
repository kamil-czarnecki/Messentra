using Bunit;
using Messentra.Features.Layout;
using Messentra.Features.Layout.Components;
using Messentra.Features.Settings.Connections;
using Moq;
using MudBlazor;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Layout;

public sealed class MainLayoutShould : ComponentTestBase
{
    protected override bool RenderMudProviders => false;

    [Fact]
    public void RenderSideBarComponent()
    {
        // Arrange & Act
        var cut = Render<MainLayout>();

        // Assert
        cut.FindComponent<SideBar>().ShouldNotBeNull();
    }

    [Fact]
    public void RenderActivityLogComponent()
    {
        // Arrange & Act
        var cut = Render<MainLayout>();

        // Assert
        cut.FindComponent<ActivityLog>().ShouldNotBeNull();
    }

    [Fact]
    public void RenderMudBlazorProviders()
    {
        // Arrange & Act
        var cut = Render<MainLayout>();

        // Assert
        cut.FindComponent<MudThemeProvider>().ShouldNotBeNull();
        cut.FindComponent<MudPopoverProvider>().ShouldNotBeNull();
        cut.FindComponent<MudDialogProvider>().ShouldNotBeNull();
        cut.FindComponent<MudSnackbarProvider>().ShouldNotBeNull();
    }

    [Fact]
    public void DispatchFetchConnectionsActionOnFirstRender()
    {
        // Arrange
        var state = GetState<ConnectionState>();
        state.SetState(new ConnectionState(false, false, [])); // Not loaded, not loading

        // Act
        _ = Render<MainLayout>();

        // Assert
        MockDispatcher.Verify(x => x.Dispatch(It.IsAny<FetchConnectionsAction>()), Times.Once);
    }

    [Fact]
    public void NotDispatchFetchConnectionsWhenAlreadyLoaded()
    {
        // Arrange
        var state = GetState<ConnectionState>();
        state.SetState(new ConnectionState(false, true, [])); // Already loaded

        // Act
        _ = Render<MainLayout>();

        // Assert
        MockDispatcher.Verify(x => x.Dispatch(It.IsAny<FetchConnectionsAction>()), Times.Never);
    }

    [Fact]
    public void NotDispatchFetchConnectionsWhenLoading()
    {
        // Arrange
        var state = GetState<ConnectionState>();
        state.SetState(new ConnectionState(true, false, [])); // Currently loading

        // Act
        _ = Render<MainLayout>();

        // Assert
        MockDispatcher.Verify(x => x.Dispatch(It.IsAny<FetchConnectionsAction>()), Times.Never);
    }

    [Fact]
    public void RenderMudLayoutStructure()
    {
        // Arrange & Act
        var cut = Render<MainLayout>();

        // Assert
        cut.FindComponent<MudLayout>().ShouldNotBeNull();
        cut.FindComponent<MudMainContent>().ShouldNotBeNull();
        cut.FindComponent<MudContainer>().ShouldNotBeNull();
    }
}

