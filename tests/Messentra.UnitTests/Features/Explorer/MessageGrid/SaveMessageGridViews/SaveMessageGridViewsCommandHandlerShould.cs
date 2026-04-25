using Messentra.Features.Explorer.MessageGrid;
using Messentra.Features.Explorer.MessageGrid.SaveMessageGridViews;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using System.Text.Json;
using Xunit;

namespace Messentra.UnitTests.Features.Explorer.MessageGrid.SaveMessageGridViews;

public sealed class SaveMessageGridViewsCommandHandlerShould : InMemoryDbTestBase
{
    private readonly SaveMessageGridViewsCommandHandler _sut;

    public SaveMessageGridViewsCommandHandlerShould()
    {
        _sut = new SaveMessageGridViewsCommandHandler(DbContext);
    }

    [Fact]
    public async Task InsertRowWithViewsWhenNoSettingsExist()
    {
        var views = new List<ColumnView>
        {
            new("v1", "My View", false, DefaultColumns.DefaultView.Columns)
        };

        await _sut.Handle(new SaveMessageGridViewsCommand(views, "v1"), CancellationToken.None);

        var saved = await DbContext.Set<Messentra.Domain.UserSettings>().FindAsync([1L], TestContext.Current.CancellationToken);
        saved.ShouldNotBeNull();
        saved.ActiveMessageGridViewId.ShouldBe("v1");
        saved.MessageGridViewsJson.ShouldNotBeNullOrEmpty();
        var deserialized = JsonSerializer.Deserialize<List<ColumnView>>(saved.MessageGridViewsJson!);
        deserialized!.Count.ShouldBe(1);
        deserialized[0].Id.ShouldBe("v1");
    }

    [Fact]
    public async Task UpdateExistingRowWithoutLosingIsDarkMode()
    {
        // Arrange — existing settings with dark mode
        DbContext.Set<Messentra.Domain.UserSettings>().Add(new Messentra.Domain.UserSettings { Id = 1, IsDarkMode = true });
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await _sut.Handle(new SaveMessageGridViewsCommand([], "default"), CancellationToken.None);

        // Assert — IsDarkMode unchanged
        var saved = await DbContext.Set<Messentra.Domain.UserSettings>()
            .AsNoTracking()
            .FirstAsync(TestContext.Current.CancellationToken);
        saved.IsDarkMode.ShouldBeTrue();
        saved.ActiveMessageGridViewId.ShouldBe("default");
    }

    [Fact]
    public async Task OnlyHaveOneSingletonRowAfterMultipleSaves()
    {
        await _sut.Handle(new SaveMessageGridViewsCommand([], "default"), CancellationToken.None);
        await _sut.Handle(new SaveMessageGridViewsCommand([], "v1"), CancellationToken.None);

        var count = await DbContext.Set<Messentra.Domain.UserSettings>().CountAsync(TestContext.Current.CancellationToken);
        count.ShouldBe(1);
    }
}
