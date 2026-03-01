using Fluxor;
using Messentra.Features.Settings.Connections;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Messentra.Features.Layout;

public partial class MainLayout
{
    [Inject] private IState<ConnectionState> ConnectionsState { get; set; } = null!;
    [Inject] private IDispatcher Dispatcher { get; set; } = null!;
    private bool _isDarkMode = false;
    private MudTheme? _theme = null;

    protected override void OnInitialized()
    {
        base.OnInitialized();

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

        if (firstRender && !ConnectionsState.Value.IsLoaded && !ConnectionsState.Value.IsLoading)
        {
            Dispatcher.Dispatch(new FetchConnectionsAction());
        }
    }

    private void DarkModeToggle()
    {
        _isDarkMode = !_isDarkMode;
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
        TextDisabled =  Colors.Gray.Darken2,
    };

    private readonly PaletteDark _darkPalette = new()
    {
        Primary = "#7e6fff",
        Surface = "#1e1e2d",
        Background = "#1a1a27",
        BackgroundGray = "#151521",
        AppbarText = "#92929f",
        AppbarBackground = "rgba(26,26,39,0.8)",
        DrawerBackground = "#1a1a27",
        ActionDefault = "#74718e",
        ActionDisabled = "#9999994d",
        ActionDisabledBackground = "#605f6d4d",
        TextPrimary = "#b2b0bf",
        TextSecondary = "#92929f",
        TextDisabled = "#ffffff33",
        DrawerIcon = "#92929f",
        DrawerText = "#92929f",
        GrayLight = "#2a2833",
        GrayLighter = "#1e1e2d",
        Info = "#4a86ff",
        Success = "#3dcb6c",
        Warning = "#ffb545",
        Error = "#ff3f5f",
        LinesDefault = "#33323e",
        TableLines = "#33323e",
        Divider = "#292838",
        OverlayLight = "#1e1e2d80",
    };

    public string DarkLightModeButtonIcon => _isDarkMode switch
    {
        true => Icons.Material.Rounded.AutoMode,
        false => Icons.Material.Outlined.DarkMode,
    };
}