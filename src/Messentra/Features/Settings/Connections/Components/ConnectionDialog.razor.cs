using Azure.Messaging.ServiceBus;
using Messentra.Domain;
using Messentra.Features.Settings.Connections.GetConnections;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Messentra.Features.Settings.Connections.Components;

public partial class ConnectionDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;
    [Parameter]
    public bool IsEdit { get; set; }
    [Parameter]
    public ConnectionDto? ExistingConnection { get; set; }
    [Parameter]
    public IEnumerable<ConnectionDto> ExistingConnections { get; set; } = [];

    private string? ValidateName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var isDuplicate = ExistingConnections.Any(c =>
            c.Name.Equals(value, StringComparison.OrdinalIgnoreCase) &&
            (!IsEdit || ExistingConnection == null || c.Id != ExistingConnection.Id));

        return isDuplicate ? $"A connection named '{value}' already exists." : null;
    }

    private MudForm _form = null!;
    private bool _isValid;

    private string _name = string.Empty;
    private ConnectionType _selectedConnectionType = ConnectionType.ConnectionString;
    
    // Connection String fields
    private string _connectionString = string.Empty;
    
    // Entra ID fields
    private string _namespace = string.Empty;
    private string _tenantId = string.Empty;
    private string _clientId = string.Empty;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        
        if (!IsEdit || ExistingConnection == null) 
            return;
        
        _name = ExistingConnection.Name;
        _selectedConnectionType = ExistingConnection.ConnectionConfig.ConnectionType;

        switch (_selectedConnectionType)
        {
            case ConnectionType.ConnectionString:
                _connectionString = ExistingConnection.ConnectionConfig.ConnectionString ?? string.Empty;
                break;
            case ConnectionType.EntraId:
                _namespace = ExistingConnection.ConnectionConfig.Namespace ?? string.Empty;
                _tenantId = ExistingConnection.ConnectionConfig.TenantId ?? string.Empty;
                _clientId = ExistingConnection.ConnectionConfig.ClientId ?? string.Empty;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void Cancel()
    {
        MudDialog.Cancel();
    }

    private async Task Submit()
    {
        await _form.ValidateAsync();

        if (!_isValid)
            return;

        var connection = new ConnectionDto(
            Id: IsEdit && ExistingConnection != null ? ExistingConnection.Id : 0,
            Name: _name,
            ConnectionConfig: new ConnectionConfigDto(
                ConnectionType: _selectedConnectionType,
                ConnectionString: _connectionString,
                Namespace: _namespace,
                TenantId: _tenantId,
                ClientId: _clientId));

        MudDialog.Close(DialogResult.Ok(connection));
    }
    
    
    private static string? ValidateConnectionString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        
        try
        {
            _ = ServiceBusConnectionStringProperties.Parse(value);

            return null;
        }
        catch (Exception)
        {
            return "Incorrect ConnectionString";
        }
    }
}

