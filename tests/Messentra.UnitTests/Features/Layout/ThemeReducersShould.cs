using Messentra.Features.Layout.State;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Layout;

public sealed class ThemeReducersShould
{
    [Fact]
    public void EnableDarkModeWhenCurrentlyLight()
    {
        // Arrange
        var state = new ThemeState(IsDarkMode: false);

        // Act
        var result = ThemeReducers.OnToggleTheme(state, new ToggleThemeAction());

        // Assert
        result.IsDarkMode.ShouldBeTrue();
    }

    [Fact]
    public void EnableLightModeWhenCurrentlyDark()
    {
        // Arrange
        var state = new ThemeState(IsDarkMode: true);

        // Act
        var result = ThemeReducers.OnToggleTheme(state, new ToggleThemeAction());

        // Assert
        result.IsDarkMode.ShouldBeFalse();
    }

    [Fact]
    public void DefaultToLightMode()
    {
        // Arrange & Act
        var state = new ThemeState(IsDarkMode: false);

        // Assert
        state.IsDarkMode.ShouldBeFalse();
    }

    [Fact]
    public void ApplyLoadedDarkModePreference()
    {
        // Arrange
        var state = new ThemeState(IsDarkMode: false);

        // Act
        var result = ThemeReducers.OnLoadThemeSettingsSuccess(state, new LoadThemeSettingsSuccessAction(IsDarkMode: true));

        // Assert
        result.IsDarkMode.ShouldBeTrue();
    }

    [Fact]
    public void ApplyLoadedLightModePreference()
    {
        // Arrange
        var state = new ThemeState(IsDarkMode: true);

        // Act
        var result = ThemeReducers.OnLoadThemeSettingsSuccess(state, new LoadThemeSettingsSuccessAction(IsDarkMode: false));

        // Assert
        result.IsDarkMode.ShouldBeFalse();
    }
}
