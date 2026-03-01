using Messentra.Features.Settings.Connections.GetConnections;

namespace Messentra.Features.Settings.Connections;

public sealed record FetchConnectionsAction;
public sealed record FetchConnectionsSuccessAction(IEnumerable<ConnectionDto> Connections);
public sealed record FetchConnectionsFailureAction;

public sealed record CreateConnectionAction(ConnectionDto Connection);
public sealed record CreateConnectionSuccessAction;
public sealed record CreateConnectionFailureAction;

public sealed record UpdateConnectionAction(ConnectionDto Connection);
public sealed record UpdateConnectionSuccessAction;
public sealed record UpdateConnectionFailureAction;

public sealed record DeleteConnectionAction(long Id);
public sealed record DeleteConnectionSuccessAction;
public sealed record DeleteConnectionFailureAction;