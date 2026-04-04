using Messentra.Features.Layout.State;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Layout.State;

public sealed class ThemeFeatureShould : InMemoryDbTestBase
{
    [Fact]
    public void DefaultToLightModeWhenNoSettingsExist()
    {
        var sut = new ThemeFeature(DbContextFactory);

        sut.State.IsDarkMode.ShouldBeFalse();
    }

    [Fact]
    public void LoadSavedDarkModePreference()
    {
        // Arrange
        DbContext.Set<Messentra.Domain.UserSettings>().Add(new Messentra.Domain.UserSettings { Id = 1, IsDarkMode = true });
        DbContext.SaveChanges();

        // Act
        var sut = new ThemeFeature(DbContextFactory);

        // Assert
        sut.State.IsDarkMode.ShouldBeTrue();
    }

    [Fact]
    public void LoadSavedLightModePreference()
    {
        // Arrange
        DbContext.Set<Messentra.Domain.UserSettings>().Add(new Messentra.Domain.UserSettings { Id = 1, IsDarkMode = false });
        DbContext.SaveChanges();

        // Act
        var sut = new ThemeFeature(DbContextFactory);

        // Assert
        sut.State.IsDarkMode.ShouldBeFalse();
    }
}
