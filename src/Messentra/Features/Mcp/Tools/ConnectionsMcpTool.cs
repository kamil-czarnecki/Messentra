using System.ComponentModel;
using FluentValidation;
using Mediator;
using Messentra.Domain;
using Messentra.Features.Mcp.ListConnections;
using Messentra.Features.Settings.Connections.CreateConnection;
using Messentra.Features.Settings.Connections.DeleteConnection;
using Messentra.Features.Settings.Connections.UpdateConnection;
using ModelContextProtocol.Server;
using CreateConnectionConfigDto = Messentra.Features.Settings.Connections.CreateConnection.ConnectionConfigDto;
using UpdateConnectionConfigDto = Messentra.Features.Settings.Connections.UpdateConnection.ConnectionConfigDto;

namespace Messentra.Features.Mcp.Tools;

[McpServerToolType]
public sealed class ConnectionsMcpTool(IMediator mediator, IMcpHelpers mcpHelpers)
{
    [McpServerTool, Description(
        "Returns all saved connections with their namespace. " +
        "Call this first — all other tools require a connection name.")]
    public async Task<IEnumerable<ConnectionSummary>> ListConnections(CancellationToken ct)
        => await mediator.Send(new ListConnectionsQuery(), ct);

    [McpServerTool, Description(
        "Adds a new connection. " +
        "connectionType must be 'ConnectionString' or 'EntraId'. " +
        "When connectionType is 'ConnectionString', connectionString is required. " +
        "When connectionType is 'EntraId', all three of namespace, tenantId, and clientId are required — " +
        "omitting any of them will result in a validation error.")]
    public async Task<McpToolResult<string>> AddConnection(
        [Description("Unique display name for the connection")] string name,
        [Description("'ConnectionString' or 'EntraId'")] string connectionType,
        [Description("Azure Service Bus connection string — required when connectionType is 'ConnectionString'")] string? connectionString,
        [Description("Service Bus namespace (e.g. my-namespace.servicebus.windows.net) — required when connectionType is 'EntraId'")] string? @namespace,
        [Description("Azure AD tenant ID — required when connectionType is 'EntraId'")] string? tenantId,
        [Description("Azure AD client (application) ID — required when connectionType is 'EntraId'")] string? clientId,
        CancellationToken ct)
    {
        if (!Enum.TryParse<ConnectionType>(connectionType, ignoreCase: true, out var parsedType))
            return new McpError($"Unknown connectionType '{connectionType}'. Valid values: ConnectionString, EntraId.");

        try
        {
            await mediator.Send(new CreateConnectionCommand(
                Name: name,
                ConnectionConfig: new CreateConnectionConfigDto(
                    ConnectionType: parsedType,
                    ConnectionString: connectionString,
                    Namespace: @namespace,
                    TenantId: tenantId,
                    ClientId: clientId)), ct);

            return $"Connection '{name}' added successfully.";
        }
        catch (ValidationException ex)
        {
            return new McpError(ex.Message);
        }
        catch
        {
            return new McpError("An unexpected error occurred.");
        }
    }

    [McpServerTool, Description(
        "Updates an existing connection identified by its current name. " +
        "newName may equal connectionName if no rename is needed. " +
        "connectionType must be 'ConnectionString' or 'EntraId'. " +
        "When connectionType is 'ConnectionString', connectionString is required. " +
        "When connectionType is 'EntraId', all three of namespace, tenantId, and clientId are required — " +
        "omitting any of them will result in a validation error.")]
    public async Task<McpToolResult<string>> UpdateConnection(
        [Description("Current connection name used to look up the connection (case-insensitive)")] string connectionName,
        [Description("New name for the connection — can equal connectionName if only the config is being changed")] string newName,
        [Description("'ConnectionString' or 'EntraId'")] string connectionType,
        [Description("Azure Service Bus connection string — required when connectionType is 'ConnectionString'")] string? connectionString,
        [Description("Service Bus namespace (e.g. my-namespace.servicebus.windows.net) — required when connectionType is 'EntraId'")] string? @namespace,
        [Description("Azure AD tenant ID — required when connectionType is 'EntraId'")] string? tenantId,
        [Description("Azure AD client (application) ID — required when connectionType is 'EntraId'")] string? clientId,
        CancellationToken ct)
    {
        var connection = await mcpHelpers.ResolveConnection(connectionName, ct);
        if (connection is null)
            return new McpError($"Connection '{connectionName}' not found.");

        if (!Enum.TryParse<ConnectionType>(connectionType, ignoreCase: true, out var parsedType))
            return new McpError($"Unknown connectionType '{connectionType}'. Valid values: ConnectionString, EntraId.");

        try
        {
            await mediator.Send(new UpdateConnectionCommand(
                Id: connection.Id,
                Name: newName,
                ConnectionConfig: new UpdateConnectionConfigDto(
                    ConnectionType: parsedType,
                    ConnectionString: connectionString,
                    Namespace: @namespace,
                    TenantId: tenantId,
                    ClientId: clientId)), ct);

            return $"Connection '{connectionName}' updated successfully.";
        }
        catch (ValidationException ex)
        {
            return new McpError(ex.Message);
        }
        catch
        {
            return new McpError("An unexpected error occurred.");
        }
    }

    [McpServerTool, Description(
        "Deletes a connection by name. This action is irreversible.")]
    public async Task<McpToolResult<string>> DeleteConnection(
        [Description("Connection name to delete (case-insensitive)")] string connectionName,
        CancellationToken ct)
    {
        var connection = await mcpHelpers.ResolveConnection(connectionName, ct);
        if (connection is null)
            return new McpError($"Connection '{connectionName}' not found.");

        try
        {
            await mediator.Send(new DeleteConnectionCommand(connection.Id), ct);

            return $"Connection '{connectionName}' deleted successfully.";
        }
        catch
        {
            return new McpError("An unexpected error occurred.");
        }
    }
}
