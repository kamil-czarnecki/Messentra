using Fluxor;
using Messentra.Features.Settings.Connections.GetConnections;

namespace Messentra.Features.Settings.Connections;

[FeatureState]
public sealed record ConnectionState(bool IsLoading, bool IsLoaded, IEnumerable<ConnectionDto> Connections)
{
    private ConnectionState() : this(false, false, [])
    {
    }
}