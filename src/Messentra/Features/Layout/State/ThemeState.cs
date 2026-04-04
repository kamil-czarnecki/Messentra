using Fluxor;

namespace Messentra.Features.Layout.State;

[FeatureState]
public sealed record ThemeState(bool IsDarkMode)
{
    private ThemeState() : this(false) { }
}
