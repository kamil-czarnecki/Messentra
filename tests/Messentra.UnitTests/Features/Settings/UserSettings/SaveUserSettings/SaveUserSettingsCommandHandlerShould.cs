using Messentra.Features.Settings.UserSettings.SaveUserSettings;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Settings.UserSettings.SaveUserSettings;

public sealed class SaveUserSettingsCommandHandlerShould : InMemoryDbTestBase
{
    private readonly SaveUserSettingsCommandHandler _sut;

    public SaveUserSettingsCommandHandlerShould()
    {
        _sut = new SaveUserSettingsCommandHandler(DbContext);
    }

    [Fact]
    public async Task InsertRowWhenNoSettingsExist()
    {
        // Act
        await _sut.Handle(new SaveUserSettingsCommand(IsDarkMode: true), CancellationToken.None);

        // Assert
        var saved = await DbContext.Set<Messentra.Domain.UserSettings>().FindAsync([1L], TestContext.Current.CancellationToken);
        saved.ShouldNotBeNull();
        saved.IsDarkMode.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateRowWhenSettingsAlreadyExist()
    {
        // Arrange
        DbContext.Set<Messentra.Domain.UserSettings>().Add(new Messentra.Domain.UserSettings { Id = 1, IsDarkMode = false });
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await _sut.Handle(new SaveUserSettingsCommand(IsDarkMode: true), CancellationToken.None);

        // Assert
        var saved = await DbContext.Set<Messentra.Domain.UserSettings>()
            .AsNoTracking()
            .FirstAsync(cancellationToken: TestContext.Current.CancellationToken);
        saved.IsDarkMode.ShouldBeTrue();
    }

    [Fact]
    public async Task OnlyHaveOneSingletonRowAfterMultipleSaves()
    {
        // Act
        await _sut.Handle(new SaveUserSettingsCommand(IsDarkMode: true), CancellationToken.None);
        await _sut.Handle(new SaveUserSettingsCommand(IsDarkMode: false), CancellationToken.None);

        // Assert
        var count = await DbContext.Set<Messentra.Domain.UserSettings>().CountAsync(cancellationToken: TestContext.Current.CancellationToken);
        count.ShouldBe(1);
    }
}
