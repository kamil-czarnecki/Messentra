using Bunit;
using Messentra.Features.Explorer.Resources.Components.Details.Tabs;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Explorer.Resources.Components.Details.Tabs;

public sealed class SaveViewAsDialogShould : ComponentTestBase
{
    [Fact]
    public void RenderWithEmptyInputWhenNoInitialName()
    {
        // Arrange & Act
        var cut = RenderDialog<SaveViewAsDialog>(p =>
            p[nameof(SaveViewAsDialog.ExistingNames)] = new List<string>());

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBeNullOrEmpty();
    }

    [Fact]
    public void RenderWithPrefilledNameFromInitialName()
    {
        // Arrange & Act
        var cut = RenderDialog<SaveViewAsDialog>(p =>
        {
            p[nameof(SaveViewAsDialog.InitialName)] = "My View";
            p[nameof(SaveViewAsDialog.ExistingNames)] = new List<string>();
        });

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBe("My View");
    }

    [Fact]
    public void DisableSaveButtonWhenNameIsEmpty()
    {
        // Arrange & Act
        var cut = RenderDialog<SaveViewAsDialog>(p =>
            p[nameof(SaveViewAsDialog.ExistingNames)] = new List<string>());

        // Assert
        cut.Find("button:contains('Save')").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public async Task DisableSaveButtonWhenNameMatchesExistingView()
    {
        // Arrange
        var cut = RenderDialog<SaveViewAsDialog>(p =>
        {
            p[nameof(SaveViewAsDialog.ExistingNames)] = new List<string> { "Default" };
        });

        // Act
        await cut.Find("input").InputAsync("Default");

        // Assert
        cut.Find("button:contains('Save')").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public async Task DisableSaveButtonWhenNameMatchesExistingViewCaseInsensitively()
    {
        // Arrange
        var cut = RenderDialog<SaveViewAsDialog>(p =>
        {
            p[nameof(SaveViewAsDialog.ExistingNames)] = new List<string> { "Default" };
        });

        // Act
        await cut.Find("input").InputAsync("default");

        // Assert
        cut.Find("button:contains('Save')").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]

    public async Task EnableSaveButtonWhenNameIsUniqueAndNonEmpty()
    {
        // Arrange
        var cut = RenderDialog<SaveViewAsDialog>(p =>
        {
            p[nameof(SaveViewAsDialog.ExistingNames)] = new List<string> { "Default" };
        });

        // Act
        await cut.Find("input").InputAsync("My Custom View");

        // Assert
        cut.Find("button:contains('Save')").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public async Task CloseDialogWithTrimmedNameOnSave()
    {
        // Arrange
        var cut = RenderDialog<SaveViewAsDialog>(p =>
        {
            p[nameof(SaveViewAsDialog.ExistingNames)] = new List<string>();
        }, out var dialogRef);

        await cut.Find("input").InputAsync("  My View  ");

        // Act
        await cut.Find("button:contains('Save')").ClickAsync();

        // Assert
        var result = await dialogRef.Result;
        result.ShouldNotBeNull();
        result.Canceled.ShouldBeFalse();
        result.Data.ShouldBe("My View");
    }

    [Fact]
    public async Task CloseDialogAsCancelledOnCancel()
    {
        // Arrange
        var cut = RenderDialog<SaveViewAsDialog>(p =>
            p[nameof(SaveViewAsDialog.ExistingNames)] = new List<string>(),
            out var dialogRef);

        // Act
        await cut.Find("button:contains('Cancel')").ClickAsync();

        // Assert
        var result = await dialogRef.Result;
        result.ShouldNotBeNull();
        result.Canceled.ShouldBeTrue();
    }
}
