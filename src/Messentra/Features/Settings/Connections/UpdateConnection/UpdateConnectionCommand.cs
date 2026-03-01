using Mediator;

namespace Messentra.Features.Settings.Connections.UpdateConnection;

public sealed record UpdateConnectionCommand(
    long Id,
    string Name,
    ConnectionConfigDto ConnectionConfig) : ICommand;