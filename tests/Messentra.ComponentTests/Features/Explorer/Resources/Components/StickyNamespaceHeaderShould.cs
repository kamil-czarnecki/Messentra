using Bunit;
using Messentra.Features.Explorer.Resources.Components;
using Microsoft.AspNetCore.Components;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Explorer.Resources.Components;

public sealed class StickyNamespaceHeaderShould : ComponentTestBase
{
    [Fact]
    public void RenderNothingWhenNamespaceIsNull()
    {
        // Arrange & Act
        var cut = Render<StickyNamespaceHeader>(p => p
            .Add(x => x.Namespace, (string?)null));

        // Assert
        cut.FindAll(".sticky-namespace-header").ShouldBeEmpty();
    }

    [Fact]
    public void RenderNamespaceNameWhenNamespaceIsSet()
    {
        // Arrange & Act
        var cut = Render<StickyNamespaceHeader>(p => p
            .Add(x => x.Namespace, "my-servicebus"));

        // Assert
        cut.Find(".sticky-namespace-header").TextContent.ShouldContain("my-servicebus");
    }

    [Fact]
    public void InvokeOnClickWithNamespaceNameWhenClicked()
    {
        // Arrange
        string? received = null;
        var cut = Render<StickyNamespaceHeader>(p => p
            .Add(x => x.Namespace, "my-servicebus")
            .Add(x => x.OnClick, EventCallback.Factory.Create<string>(this, v => received = v)));

        // Act
        cut.Find(".sticky-namespace-header").Click();

        // Assert
        received.ShouldBe("my-servicebus");
    }
}