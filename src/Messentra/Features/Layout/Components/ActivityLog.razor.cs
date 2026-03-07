using Fluxor;
using Messentra.Features.Explorer.Resources;
using Messentra.Features.Layout.State;
using MudBlazor;

namespace Messentra.Features.Layout.Components;

public partial class ActivityLog
{
    private string DrawerHeight => _open ? "var(--drawer-height-bottom-opened)" : "var(--drawer-height-bottom)";
    private bool _open;
    private readonly IReadOnlyCollection<string> _logLevels = new List<string> { "All Levels", "Debug", "Info", "Warning", "Error" };

    private List<string> Connections =>
        ["All Connections", .._resourceState.Value.Namespaces.Select(x => x.ConnectionName).ToList()];
    private string _selectedLogLevel = "";
    private string _selectedConnection = "";
    private readonly IState<ResourceState> _resourceState;

    public ActivityLog(IState<ResourceState> resourceState)
    {
        _resourceState = resourceState;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        
        _selectedLogLevel = _logLevels.First();
        _selectedConnection = Connections.First();
    }

    private void ToggleDrawer()
    {
        _open = !_open;
    }

    private void OnDeleteClicked()
    {
        Dispatcher.Dispatch(new ClearActivityLogAction());
    }

    private static string GetIcon(string level) => level.ToLower() switch
    {
        "info" => Icons.Material.Filled.Info,
        "warning" => Icons.Material.Filled.Warning,
        "error" => Icons.Material.Filled.Error,
        "debug" => Icons.Material.Filled.BugReport,
        _ => Icons.Material.Filled.Info
    };

    private static Severity GetSeverity(string level) => level.ToLower() switch
    {
        "info" => Severity.Info,
        "warning" => Severity.Warning,
        "error" => Severity.Error,
        _ => Severity.Normal
    };

    private IEnumerable<ActivityLogEntry> GetFilteredLogs()
    {
        var logs = ActivityLogState.Value.Logs;

        // Filter by connection
        if (_selectedConnection != "All Connections")
        {
            logs = logs.Where(l => l.Connection == _selectedConnection);
        }

        // Filter by log level
        if (_selectedLogLevel != "All Levels")
        {
            logs = logs.Where(l => l.Level.Equals(_selectedLogLevel, StringComparison.OrdinalIgnoreCase));
        }

        return logs.OrderByDescending(l => l.Timestamp);
    }
}