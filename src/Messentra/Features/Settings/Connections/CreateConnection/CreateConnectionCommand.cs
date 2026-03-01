using Mediator;

namespace Messentra.Features.Settings.Connections.CreateConnection;

public sealed record CreateConnectionCommand(
    string Name,
    ConnectionConfigDto ConnectionConfig) : ICommand;