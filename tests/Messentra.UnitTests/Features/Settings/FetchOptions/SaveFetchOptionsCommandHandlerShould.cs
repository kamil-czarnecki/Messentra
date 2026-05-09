using Messentra.Features.Settings.FetchOptions.SaveFetchOptions;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Settings.FetchOptions;

public sealed class SaveFetchOptionsCommandHandlerShould : InMemoryDbTestBase
{
    private readonly SaveFetchOptionsCommandHandler _sut;

    public SaveFetchOptionsCommandHandlerShould()
    {
        _sut = new SaveFetchOptionsCommandHandler(DbContext);
    }

    [Fact]
    public async Task InsertRowWithDefaultMessageCountWhenNoSettingsExist()
    {
        // Act
        await _sut.Handle(new SaveFetchOptionsCommand(DefaultMessageCount: 50), CancellationToken.None);

        // Assert
        var saved = await DbContext.Set<Messentra.Domain.UserSettings>()
            .FindAsync([1L], TestContext.Current.CancellationToken);
        saved.ShouldNotBeNull();
        saved.DefaultMessageCount.ShouldBe(50);
    }

    [Fact]
    public async Task UpdateDefaultMessageCountWhenSettingsAlreadyExist()
    {
        // Arrange
        DbContext.Set<Messentra.Domain.UserSettings>().Add(
            new Messentra.Domain.UserSettings { Id = 1, DefaultMessageCount = 100 });
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await _sut.Handle(new SaveFetchOptionsCommand(DefaultMessageCount: 500), CancellationToken.None);

        // Assert
        var saved = await DbContext.Set<Messentra.Domain.UserSettings>()
            .AsNoTracking()
            .FirstAsync(cancellationToken: TestContext.Current.CancellationToken);
        saved.DefaultMessageCount.ShouldBe(500);
    }

    [Fact]
    public async Task OnlyHaveOneSingletonRowAfterMultipleSaves()
    {
        // Act
        await _sut.Handle(new SaveFetchOptionsCommand(DefaultMessageCount: 10), CancellationToken.None);
        await _sut.Handle(new SaveFetchOptionsCommand(DefaultMessageCount: 20), CancellationToken.None);

        // Assert
        var count = await DbContext.Set<Messentra.Domain.UserSettings>()
            .CountAsync(cancellationToken: TestContext.Current.CancellationToken);
        count.ShouldBe(1);
    }
}
