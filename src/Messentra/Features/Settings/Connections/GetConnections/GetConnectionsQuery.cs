using Mediator;

namespace Messentra.Features.Settings.Connections.GetConnections;

public sealed record GetConnectionsQuery : IQuery<IReadOnlyCollection<ConnectionDto>>;