namespace Messentra.Features.Layout.State;

public sealed record ToggleThemeAction;
public sealed record LoadThemeSettingsAction;
public sealed record LoadThemeSettingsSuccessAction(bool IsDarkMode);
public sealed record LoadThemeSettingsFailureAction;
