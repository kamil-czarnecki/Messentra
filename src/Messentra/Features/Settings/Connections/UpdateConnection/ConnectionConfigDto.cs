using Messentra.Domain;

namespace Messentra.Features.Settings.Connections.UpdateConnection;

public sealed record ConnectionConfigDto(
    ConnectionType ConnectionType,
    string? ConnectionString,
    string? Namespace,
    string? TenantId,
    string? ClientId);