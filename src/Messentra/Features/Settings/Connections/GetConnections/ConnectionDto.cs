using Azure.Messaging.ServiceBus;
using Messentra.Domain;

namespace Messentra.Features.Settings.Connections.GetConnections;

public sealed record ConnectionDto(long Id, string Name, ConnectionConfigDto ConnectionConfig)
{
    public static ConnectionDto From(Connection connection) =>
        new(
            connection.Id,
            connection.Name,
            new ConnectionConfigDto(
                connection.ConnectionConfig.ConnectionType,
                connection.ConnectionConfig.ConnectionStringConfig?.ConnectionString,
                connection.ConnectionConfig.EntraIdConfig?.Namespace,
                connection.ConnectionConfig.EntraIdConfig?.TenantId,
                connection.ConnectionConfig.EntraIdConfig?.ClientId));
}

public record ConnectionConfigDto(
    ConnectionType ConnectionType,
    string? ConnectionString,
    string? Namespace,
    string? TenantId,
    string? ClientId)
{
    public string GetNamespace()
    {
        return ConnectionType switch
        {
            ConnectionType.ConnectionString => ServiceBusConnectionStringProperties.Parse(ConnectionString)
                .FullyQualifiedNamespace,
            ConnectionType.EntraId => Namespace!,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}