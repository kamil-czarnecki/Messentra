using Fluxor;
using Messentra.Features.Jobs;
using Messentra.Features.Layout.State;
using MudBlazor;

namespace Messentra.Features.Layout.Components;

public partial class SideBar
{
    private readonly IReadOnlyCollection<Link> _links = new List<Link>
    {
        new("Explorer", "/explorer", Icons.Material.Filled.Layers),
        new("Jobs", "/jobs", Icons.Material.Filled.Sync),
        new("Options", "/options", Icons.Material.Filled.Cable)
    };

    private record Link(string Title, string Href, string Icon, bool IsDisabled = false);

    private readonly IState<JobState> _jobState;
    private readonly IState<ThemeState> _themeState;
    private readonly IDispatcher _dispatcher;

    public SideBar(IState<JobState> jobState, IState<ThemeState> themeState, IDispatcher dispatcher)
    {
        _jobState = jobState;
        _themeState = themeState;
        _dispatcher = dispatcher;
    }

    private string ThemeToggleIcon => _themeState.Value.IsDarkMode
        ? Icons.Material.Rounded.LightMode
        : Icons.Material.Outlined.DarkMode;

    private void OnToggleTheme() => _dispatcher.Dispatch(new ToggleThemeAction());
}