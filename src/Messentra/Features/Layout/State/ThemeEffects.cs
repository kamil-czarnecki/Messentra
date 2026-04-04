using Fluxor;
using Mediator;
using Messentra.Features.Settings.UserSettings.SaveUserSettings;

namespace Messentra.Features.Layout.State;

public sealed class ThemeEffects(IMediator mediator, IState<ThemeState> themeState)
{
    [EffectMethod(typeof(ToggleThemeAction))]
    public async Task HandleToggleTheme(IDispatcher dispatcher)
    {
        try
        {
            await mediator.Send(new SaveUserSettingsCommand(themeState.Value.IsDarkMode), CancellationToken.None);
        }
        catch
        {
            // saving preference is best-effort — silently ignore failures
        }
    }
}
