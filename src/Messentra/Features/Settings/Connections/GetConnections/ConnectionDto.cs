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
            ConnectionType.ConnectionString => ParseNamespace(ConnectionString),
            ConnectionType.EntraId => Namespace!,
            ConnectionType.Corrupted => string.Empty,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static string ParseNamespace(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return string.Empty;

        try
        {
            return ServiceBusConnectionStringProperties.Parse(connectionString).FullyQualifiedNamespace;
        }
        catch (FormatException)
        {
            return connectionString;
        }
    }
}