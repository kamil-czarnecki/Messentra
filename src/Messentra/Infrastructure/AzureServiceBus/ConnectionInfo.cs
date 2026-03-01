namespace Messentra.Infrastructure.AzureServiceBus;

public abstract record ConnectionInfo
{
    public sealed record ConnectionString(string Value) : ConnectionInfo;

    public sealed record ManagedIdentity(
        string FullyQualifiedNamespace,
        string TenantId,
        string ClientId) : ConnectionInfo;

    private ConnectionInfo() { }
}

