using Bunit;
using Messentra.Features.Explorer.Resources.Components.Details;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Explorer.Resources.Components.Details;

public sealed class ImportMessagesDialogShould : ComponentTestBase
{
    [Fact]
    public void RenderExpectedHelpTextTemplateAndActions()
    {
        // Arrange & Act
        var cut = RenderDialog<ImportMessagesDialog>();

        // Assert
        cut.Markup.ShouldContain("Select a JSON file.");
        cut.FindAll("button:contains('Browse files')").Count.ShouldBe(1);
        cut.Markup.ShouldContain("Example JSON template");
        cut.Markup.ShouldContain("json-wrapper");
        cut.FindAll(".json-copy-btn").Count.ShouldBe(1);
    }
}


