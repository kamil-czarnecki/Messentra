using Messentra.Features.Explorer.MessageGrid;
using Messentra.Features.Explorer.MessageGrid.State;
using Shouldly;
using System.Text.Json;
using Xunit;

namespace Messentra.UnitTests.Features.Explorer.MessageGrid.State;

public sealed class MessageGridFeatureShould : InMemoryDbTestBase
{
    [Fact]
    public void DefaultToBuiltInViewWhenNoSettingsExist()
    {
        var sut = new MessageGridFeature(DbContextFactory);

        sut.State.Views.Count.ShouldBe(1);
        sut.State.Views[0].Id.ShouldBe("default");
        sut.State.ActiveViewId.ShouldBe("default");
        sut.State.Columns.Count.ShouldBe(5);
    }

    [Fact]
    public void LoadSavedUserViewsAndPrependDefault()
    {
        // Arrange
        var userViews = new List<ColumnView>
        {
            new("v1", "My View", false, DefaultColumns.DefaultView.Columns)
        };
        DbContext.Set<Messentra.Domain.UserSettings>().Add(new Messentra.Domain.UserSettings
        {
            Id = 1,
            MessageGridViewsJson = JsonSerializer.Serialize(userViews),
            ActiveMessageGridViewId = "v1"
        });
        DbContext.SaveChanges();

        // Act
        var sut = new MessageGridFeature(DbContextFactory);

        // Assert
        sut.State.Views.Count.ShouldBe(2);          // default + v1
        sut.State.Views[0].Id.ShouldBe("default");   // default always first
        sut.State.Views[1].Id.ShouldBe("v1");
        sut.State.ActiveViewId.ShouldBe("v1");
    }

    [Fact]
    public void FallBackToDefaultWhenActiveViewIdNotFound()
    {
        // Arrange — saved active ID doesn't exist in user views
        DbContext.Set<Messentra.Domain.UserSettings>().Add(new Messentra.Domain.UserSettings
        {
            Id = 1,
            MessageGridViewsJson = "[]",
            ActiveMessageGridViewId = "nonexistent"
        });
        DbContext.SaveChanges();

        var sut = new MessageGridFeature(DbContextFactory);

        sut.State.ActiveViewId.ShouldBe("default");
    }

    [Fact]
    public void FallBackToDefaultWhenJsonIsInvalid()
    {
        DbContext.Set<Messentra.Domain.UserSettings>().Add(new Messentra.Domain.UserSettings
        {
            Id = 1,
            MessageGridViewsJson = "not-valid-json"
        });
        DbContext.SaveChanges();

        var sut = new MessageGridFeature(DbContextFactory);

        sut.State.Views.Count.ShouldBe(1);
        sut.State.ActiveViewId.ShouldBe("default");
    }
}
