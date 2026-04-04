using Fluxor;
using Messentra.Features.Jobs;
using Messentra.Features.Layout.State;
using Messentra.Features.Settings.Connections;
using MudBlazor;

namespace Messentra.Features.Layout;

public partial class MainLayout : IDisposable
{
    private readonly IState<ConnectionState> _connectionsState;
    private readonly IState<ThemeState> _themeState;
    private readonly IDispatcher _dispatcher;
    private readonly IJobProgressNotifier _jobProgressNotifier;
    private bool _isActivityLogExpanded;
    private MudTheme? _theme;
    private IDisposable? _progressSubscription;

    public MainLayout(IState<ConnectionState> connectionsState, IState<ThemeState> themeState, IDispatcher dispatcher, IJobProgressNotifier jobProgressNotifier)
    {
        _connectionsState = connectionsState;
        _themeState = themeState;
        _dispatcher = dispatcher;
        _jobProgressNotifier = jobProgressNotifier;
    }
    
    private string MainContentClass => _isActivityLogExpanded
        ? "mud-height-full activity-log-expanded"
        : "mud-height-full activity-log-collapsed";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _themeState.StateChanged += OnThemeStateChanged;

        _theme = new MudTheme
        {
            PaletteLight = _lightPalette,
            PaletteDark = _darkPalette,
            LayoutProperties = new LayoutProperties
            {
                AppbarHeight = "0px",
                DrawerWidthLeft = "200px"
            }
        };
    }
    
    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        if (!firstRender)
            return;

        if (!_connectionsState.Value.IsLoaded && !_connectionsState.Value.IsLoading)
        {
            _dispatcher.Dispatch(new FetchConnectionsAction());
        }
        
        _dispatcher.Dispatch(new LoadThemeSettingsAction());
        _progressSubscription = _jobProgressNotifier.Subscribe(update =>
        {
            _dispatcher.Dispatch(new JobProgressReceivedAction(update));
        });
    }

    private void OnThemeStateChanged(object? sender, EventArgs e) => InvokeAsync(StateHasChanged);

    private Task OnActivityLogExpandedChanged(bool isExpanded)
    {
        _isActivityLogExpanded = isExpanded;
        return Task.CompletedTask;
    }

    private readonly PaletteLight _lightPalette = new()
    {
        Primary = "#2F90F2",
        Secondary = "#f97516",
        Tertiary = "#0b1220",
        Background = "#F8FAFC",
        TextPrimary = "#0b1220",
        TextSecondary = "#64748B",
        Black = "#110e2d",
        AppbarText = "#424242",
        AppbarBackground = "rgba(255,255,255,0.8)",
        DrawerBackground = "#0b1220",
        DrawerText = Colors.Gray.Lighten5,
        DrawerIcon = Colors.Gray.Lighten3,
        GrayLight = "#e8e8e8",
        GrayLighter = "#f9f9f9",
        TextDisabled =  Colors.Gray.Darken2
    };

    private readonly PaletteDark _darkPalette = new()
    {
        Primary = "#2F90F2",
        Secondary = "#f97516",
        Tertiary = "#0b1220",
        Surface = "#161b22",
        Background = "#0d1117",
        BackgroundGray = "#090d13",
        AppbarText = "#8b949e",
        AppbarBackground = "rgba(13,17,23,0.8)",
        DrawerBackground = "#0d1117",
        ActionDefault = "#8b949e",
        ActionDisabled = "#8b949e4d",
        ActionDisabledBackground = "#30363d4d",
        TextPrimary = "#e6edf3",
        TextSecondary = "#8b949e",
        TextDisabled = "#ffffff33",
        DrawerText = Colors.Gray.Lighten5,
        DrawerIcon = Colors.Gray.Lighten3,
        GrayLight = "#21262d",
        GrayLighter = "#161b22",
        Info = "#2F90F2",
        Success = "#3dcb6c",
        Warning = "#ffb545",
        Error = "#ff3f5f",
        LinesDefault = "#30363d",
        TableLines = "#30363d",
        Divider = "#21262d",
        OverlayLight = "#161b2280"
    };

    public void Dispose()
    {
        _themeState.StateChanged -= OnThemeStateChanged;
        _progressSubscription?.Dispose();
    }
}