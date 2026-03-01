using Mediator;

namespace Messentra.Features.Settings.Connections.DeleteConnection;

public sealed record DeleteConnectionCommand(long Id) : ICommand;