using Fluxor;

namespace Messentra.Features.Layout.State;

public static class ThemeReducers
{
    [ReducerMethod]
    public static ThemeState OnToggleTheme(ThemeState state, ToggleThemeAction _)
        => state with { IsDarkMode = !state.IsDarkMode };
}
