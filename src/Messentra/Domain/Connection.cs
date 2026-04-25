namespace Messentra.Domain;

public class Connection
{
    public long Id { get; init; }
    public required string Name { get; set; }
    public required ConnectionConfig ConnectionConfig { get; set; }

}

public record ConnectionConfig(
    ConnectionType ConnectionType,
    ConnectionStringConfig? ConnectionStringConfig,
    EntraIdConfig? EntraIdConfig)
{
    public static ConnectionConfig CreateConnectionString(string connectionString) =>
        new(ConnectionType.ConnectionString, new ConnectionStringConfig(connectionString), null);
    
    public static ConnectionConfig CreateEntraId(string @namespace, string tenantId, string clientId) =>
        new(ConnectionType.EntraId, null, new EntraIdConfig(@namespace, tenantId, clientId));

    public static ConnectionConfig CreateCorrupted() =>
        new(ConnectionType.Corrupted, null, null);
}

public enum ConnectionType
{
    ConnectionString,
    EntraId,
    Corrupted = 99
}

public record ConnectionStringConfig(string ConnectionString);
public record EntraIdConfig(string Namespace, string TenantId, string ClientId);