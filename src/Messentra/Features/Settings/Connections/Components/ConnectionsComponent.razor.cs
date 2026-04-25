using Fluxor;
using Messentra.Domain;
using Messentra.Features.Settings.Connections.GetConnections;
using MudBlazor;

namespace Messentra.Features.Settings.Connections.Components;

public partial class ConnectionsComponent
{
    private readonly IState<ConnectionState> _connectionsState;
    private readonly IDispatcher _dispatcher;
    private readonly IDialogService _dialogService;

    public ConnectionsComponent(
        IState<ConnectionState> connectionsState,
        IDispatcher dispatcher, 
        IDialogService dialogService)
    {
        _connectionsState = connectionsState;
        _dispatcher = dispatcher;
        _dialogService = dialogService;
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        if (firstRender && !_connectionsState.Value.IsLoading && !_connectionsState.Value.IsLoaded)
        {
            FetchConnections();
        }
    }
    
    private void FetchConnections()
    {
        _dispatcher.Dispatch(new FetchConnectionsAction());
    }

    private async Task OpenAddConnectionDialog()
    {
        var parameters = new DialogParameters
        {
            ["IsEdit"] = false,
            ["ExistingConnections"] = _connectionsState.Value.Connections
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseButton = true,
            CloseOnEscapeKey = true
        };

        var dialog = await _dialogService.ShowAsync<ConnectionDialog>("Add Connection", parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: ConnectionDto newConnection })
        {
            _dispatcher.Dispatch(new CreateConnectionAction(newConnection));
        }
    }

    private async Task OpenEditConnectionDialog(ConnectionDto connection)
    {
        var parameters = new DialogParameters
        {
            ["IsEdit"] = true,
            ["ExistingConnection"] = connection,
            ["ExistingConnections"] = _connectionsState.Value.Connections
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseButton = true,
            CloseOnEscapeKey = true
        };

        var dialog = await _dialogService.ShowAsync<ConnectionDialog>("Edit Connection", parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: ConnectionDto updatedConnection })
        {
            _dispatcher.Dispatch(new UpdateConnectionAction(updatedConnection));
        }
    }

    private async Task DeleteConnection(ConnectionDto connection)
    {
        var result = await _dialogService.ShowMessageBoxAsync(
            "Delete Connection",
            $"Are you sure you want to delete the connection '{connection.Name}'? This action cannot be undone.",
            yesText: "Delete",
            cancelText: "Cancel");

        if (result == true)
        {
            _dispatcher.Dispatch(new DeleteConnectionAction(connection.Id));
        }
    }

    private static string GetConnectionDetails(ConnectionDto connection)
    {
        return connection.ConnectionConfig.ConnectionType switch
        {
            ConnectionType.ConnectionString => $"{connection.ConnectionConfig.GetNamespace()} ***",
            ConnectionType.EntraId => connection.ConnectionConfig.Namespace ?? "",
            _ => ""
        };
    }
}