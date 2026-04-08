using Azure.Messaging.ServiceBus;
using Messentra.Domain;

namespace Messentra.Features.Explorer.Folders;

public static class FolderResourceUrl
{
    public static string GetNamespacePrefix(ConnectionConfig config) =>
        config.ConnectionType switch
        {
            ConnectionType.ConnectionString =>
                $"https://{ServiceBusConnectionStringProperties.Parse(config.ConnectionStringConfig!.ConnectionString).FullyQualifiedNamespace}",
            ConnectionType.EntraId =>
                $"https://{config.EntraIdConfig!.Namespace}",
            _ => throw new NotSupportedException($"Unsupported connection type: {config.ConnectionType}")
        };

    public static string ToRelative(string resourceUrl, string namespacePrefix) =>
        resourceUrl.StartsWith(namespacePrefix + "/", StringComparison.OrdinalIgnoreCase)
            ? resourceUrl[(namespacePrefix.Length + 1)..]
            : throw new ArgumentException(
                $"Resource URL '{resourceUrl}' does not belong to namespace '{namespacePrefix}'.",
                nameof(resourceUrl));

    public static string ToAbsolute(string relativePath, string namespacePrefix) =>
        $"{namespacePrefix}/{relativePath}";
}
