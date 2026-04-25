using Bunit;
using Messentra.Features.Explorer.MessageGrid;
using Messentra.Features.Explorer.Resources.Components.Details.Tabs;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Explorer.Resources.Components.Details.Tabs;

public sealed class AddColumnDialogShould : ComponentTestBase
{
    private static IReadOnlyList<ColumnConfig> NoExistingColumns => [];

    [Fact]
    public void RenderWithBrokerPropertySourceByDefault()
    {
        // Arrange & Act
        var cut = RenderDialog<AddColumnDialog>(p =>
            p[nameof(AddColumnDialog.ExistingColumns)] = NoExistingColumns);

        // Assert
        cut.Markup.ShouldContain("Broker Property");
    }

    [Fact]
    public void DisableAddColumnButtonWhenNoBrokerKeySelected()
    {
        // Arrange & Act
        var cut = RenderDialog<AddColumnDialog>(p =>
            p[nameof(AddColumnDialog.ExistingColumns)] = NoExistingColumns);

        // Assert
        cut.Find("button:contains('Add Column')").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public async Task ShowAppPropertyKeyFieldWhenSourceSwitchedToAppProperty()
    {
        // Arrange
        var cut = RenderDialog<AddColumnDialog>(p =>
            p[nameof(AddColumnDialog.ExistingColumns)] = NoExistingColumns);

        // Act
        await cut.Find("button:contains('Application Property')").ClickAsync();

        // Assert
        cut.Markup.ShouldContain("Property key");
    }

    [Fact]
    public async Task DisableAddColumnButtonWhenAppPropertyKeyIsEmpty()
    {
        // Arrange
        var cut = RenderDialog<AddColumnDialog>(p =>
            p[nameof(AddColumnDialog.ExistingColumns)] = NoExistingColumns);

        // Act
        await cut.Find("button:contains('Application Property')").ClickAsync();

        // Assert
        cut.Find("button:contains('Add Column')").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public async Task EnableAddColumnButtonWhenAppPropertyKeyIsEntered()
    {
        // Arrange
        var cut = RenderDialog<AddColumnDialog>(p =>
            p[nameof(AddColumnDialog.ExistingColumns)] = NoExistingColumns);

        await cut.Find("button:contains('Application Property')").ClickAsync();

        // Act
        await cut.FindAll("input")[0].ChangeAsync("my-app-key");

        // Assert
        cut.Find("button:contains('Add Column')").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public async Task AutoFillTitleFromAppPropertyKeyWhenTitleIsBlank()
    {
        // Arrange
        var cut = RenderDialog<AddColumnDialog>(p =>
            p[nameof(AddColumnDialog.ExistingColumns)] = NoExistingColumns);

        await cut.Find("button:contains('Application Property')").ClickAsync();

        // Act
        await cut.FindAll("input")[0].ChangeAsync("my-app-key");

        // Assert
        var inputs = cut.FindAll("input");
        inputs[1].GetAttribute("value").ShouldBe("my-app-key");
    }

    [Fact]
    public async Task CloseDialogWithAppPropertyColumnConfigOnConfirm()
    {
        // Arrange
        var cut = RenderDialog<AddColumnDialog>(p =>
            p[nameof(AddColumnDialog.ExistingColumns)] = NoExistingColumns,
            out var dialogRef);

        await cut.Find("button:contains('Application Property')").ClickAsync();
        await cut.FindAll("input")[0].ChangeAsync("correlation-key");

        // Act
        await cut.Find("button:contains('Add Column')").ClickAsync();

        // Assert
        var result = await dialogRef.Result;
        result.ShouldNotBeNull();
        result.Canceled.ShouldBeFalse();
        var config = result.Data.ShouldBeOfType<ColumnConfig>();
        config.Source.ShouldBe(ColumnSource.AppProperty);
        config.PropertyKey.ShouldBe("correlation-key");
        config.IsRemovable.ShouldBeTrue();
    }

    [Fact]
    public async Task UseCustomTitleInColumnConfigWhenTitleIsExplicitlySet()
    {
        // Arrange
        var cut = RenderDialog<AddColumnDialog>(p =>
            p[nameof(AddColumnDialog.ExistingColumns)] = NoExistingColumns,
            out var dialogRef);

        await cut.Find("button:contains('Application Property')").ClickAsync();
        await cut.FindAll("input")[0].ChangeAsync("my-key");
        await cut.FindAll("input")[1].ChangeAsync("Custom Title");

        // Act
        await cut.Find("button:contains('Add Column')").ClickAsync();

        // Assert
        var result = await dialogRef.Result;
        var config = result!.Data.ShouldBeOfType<ColumnConfig>();
        config.Title.ShouldBe("Custom Title");
    }

    [Fact]
    public async Task CloseDialogAsCancelledOnCancel()
    {
        // Arrange
        var cut = RenderDialog<AddColumnDialog>(p =>
            p[nameof(AddColumnDialog.ExistingColumns)] = NoExistingColumns,
            out var dialogRef);

        // Act
        await cut.Find("button:contains('Cancel')").ClickAsync();

        // Assert
        var result = await dialogRef.Result;
        result.ShouldNotBeNull();
        result.Canceled.ShouldBeTrue();
    }
}
