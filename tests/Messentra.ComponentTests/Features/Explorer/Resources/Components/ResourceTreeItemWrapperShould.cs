using Bunit;
using Messentra.Features.Explorer.Resources;
using Messentra.Features.Explorer.Resources.Components;
using MudBlazor;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Explorer.Resources.Components;

public sealed class ResourceTreeItemWrapperShould : ComponentTestBase
{
    private static ResourceTreeItemData BuildPresenter(string text = "queue-1") =>
        new() { Text = text, Icon = Icons.Material.Filled.ViewList, IconColor = Color.Secondary };

    [Fact]
    public void RendersItemContent()
    {
        // Arrange
        var presenter = BuildPresenter();

        // Act
        var cut = Render<ResourceTreeItemWrapper>(p => p
            .Add(x => x.Presenter, presenter)
            .Add(x => x.IsSelected, false)
            .Add(x => x.IsExpanded, false)
            .Add(x => x.ItemContent, b => b.AddMarkupContent(0, "<span class='test-content'>item</span>")));

        // Assert
        cut.Find(".test-content").ShouldNotBeNull();
    }

    [Fact]
    public void RendersPresenterTextInsideTreeItem()
    {
        // Arrange
        var presenter = BuildPresenter("my-queue");

        // Act
        var cut = Render<ResourceTreeItemWrapper>(p => p
            .Add(x => x.Presenter, presenter)
            .Add(x => x.IsSelected, false)
            .Add(x => x.IsExpanded, false)
            .Add(x => x.ItemContent, b => b.AddMarkupContent(0, "<span>my-queue</span>")));

        // Assert
        cut.Markup.ShouldContain("my-queue");
    }

    [Fact]
    public async Task InvokesExpandedChangedCallbackWhenTriggered()
    {
        // Arrange
        bool? captured = null;
        var presenter = BuildPresenter();
        var cut = Render<ResourceTreeItemWrapper>(p => p
            .Add(x => x.Presenter, presenter)
            .Add(x => x.IsSelected, false)
            .Add(x => x.IsExpanded, false)
            .Add(x => x.ExpandedChanged, v => captured = v));

        // Act
        await cut.InvokeAsync(() =>
            cut.Instance.ExpandedChanged.InvokeAsync(true));

        // Assert
        captured.ShouldBe(true);
    }

    [Fact]
    public async Task InvokesSelectedChangedCallbackWhenTriggered()
    {
        // Arrange
        bool? captured = null;
        var presenter = BuildPresenter();
        var cut = Render<ResourceTreeItemWrapper>(p => p
            .Add(x => x.Presenter, presenter)
            .Add(x => x.IsSelected, false)
            .Add(x => x.IsExpanded, false)
            .Add(x => x.SelectedChanged, v => captured = v));

        // Act
        await cut.InvokeAsync(() =>
            cut.Instance.SelectedChanged.InvokeAsync(true));

        // Assert
        captured.ShouldBe(true);
    }
}
