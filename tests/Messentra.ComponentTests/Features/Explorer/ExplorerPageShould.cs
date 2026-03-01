using Bunit;
using Messentra.Features.Explorer;
using Messentra.Features.Explorer.Resources.Components;
using Messentra.Features.Explorer.Resources.Components.Details;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Explorer;

public sealed class ExplorerPageShould : ComponentTestBase
{
    [Fact]
    public void RenderNamespaceTreeComponent()
    {
        // Arrange & Act
        var cut = Render<ExplorerPage>();

        // Assert
        cut.FindComponent<NamespaceTree>().ShouldNotBeNull();
    }

    [Fact]
    public void RenderResourceDetailsComponent()
    {
        // Arrange & Act
        var cut = Render<ExplorerPage>();

        // Assert
        cut.FindComponent<ResourceDetails>().ShouldNotBeNull();
    }
}

