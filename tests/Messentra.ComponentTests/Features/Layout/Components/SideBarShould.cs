using Bunit;
using Messentra.Features.Layout.Components;
using MudBlazor;
using MudBlazor.Extensions;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Layout.Components;

public sealed class SideBarShould : ComponentTestBase
{
    [Fact]
    public void RenderNavigationLinks()
    {
        // Arrange & Act
        var cut = Render<SideBar>();

        // Assert
        cut.FindComponents<MudNavLink>().Count.ShouldBe(3);
        cut.Markup.ShouldContain("Explorer");
        cut.Markup.ShouldContain("Jobs");
        cut.Markup.ShouldContain("Options");
    }

    [Fact]
    public void RenderLinksWithCorrectHrefs()
    {
        // Arrange & Act
        var cut = Render<SideBar>();

        // Assert
        var navLinks = cut.FindComponents<MudNavLink>();
        navLinks[0].Instance.Href.ShouldBe("/explorer");
        navLinks[1].Instance.Href.ShouldBe("/jobs");
        navLinks[2].Instance.Href.ShouldBe("/options");
    }

    [Fact]
    public void RenderDisabledLinkCorrectly()
    {
        // Arrange & Act
        var cut = Render<SideBar>();

        // Assert
        var navLinks = cut.FindComponents<MudNavLink>();
        navLinks[0].Instance.Disabled.ShouldBeFalse();
        navLinks[1].Instance.Disabled.ShouldBeTrue();
        navLinks[2].Instance.Disabled.ShouldBeFalse();
    }

    [Fact]
    public void RenderDrawerWithCorrectConfiguration()
    {
        // Arrange & Act
        var cut = Render<SideBar>();

        // Assert
        var drawer = cut.FindComponent<MudDrawer>();
        drawer.Instance.GetState(x => x.Open).ShouldBeTrue();
        drawer.Instance.Variant.ShouldBe(DrawerVariant.Persistent);
    }
}

