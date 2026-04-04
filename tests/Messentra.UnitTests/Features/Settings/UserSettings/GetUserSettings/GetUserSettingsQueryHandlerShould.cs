using Messentra.Features.Settings.UserSettings.GetUserSettings;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Settings.UserSettings.GetUserSettings;

public sealed class GetUserSettingsQueryHandlerShould : InMemoryDbTestBase
{
    private readonly GetUserSettingsQueryHandler _sut;

    public GetUserSettingsQueryHandlerShould()
    {
        _sut = new GetUserSettingsQueryHandler(DbContext);
    }

    [Fact]
    public async Task ReturnDefaultLightModeWhenNoSettingsExist()
    {
        // Act
        var result = await _sut.Handle(new GetUserSettingsQuery(), CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsDarkMode.ShouldBeFalse();
    }

    [Fact]
    public async Task ReturnSavedDarkModePreference()
    {
        // Arrange
        await GivenUserSettings(isDarkMode: true);

        // Act
        var result = await _sut.Handle(new GetUserSettingsQuery(), CancellationToken.None);

        // Assert
        result.IsDarkMode.ShouldBeTrue();
    }

    [Fact]
    public async Task ReturnSavedLightModePreference()
    {
        // Arrange
        await GivenUserSettings(isDarkMode: false);

        // Act
        var result = await _sut.Handle(new GetUserSettingsQuery(), CancellationToken.None);

        // Assert
        result.IsDarkMode.ShouldBeFalse();
    }

    private async Task GivenUserSettings(bool isDarkMode)
    {
        DbContext.Set<Messentra.Domain.UserSettings>().Add(new Messentra.Domain.UserSettings { Id = 1, IsDarkMode = isDarkMode });
        await DbContext.SaveChangesAsync();
    }
}
