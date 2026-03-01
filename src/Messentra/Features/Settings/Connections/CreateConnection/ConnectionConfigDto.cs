using Messentra.Domain;

namespace Messentra.Features.Settings.Connections.CreateConnection;

public sealed record ConnectionConfigDto(
    ConnectionType ConnectionType,
    string? ConnectionString,
    string? Namespace,
    string? TenantId,
    string? ClientId);