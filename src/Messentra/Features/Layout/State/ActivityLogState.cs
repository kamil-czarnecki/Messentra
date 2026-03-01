using Fluxor;

namespace Messentra.Features.Layout.State;

[FeatureState]
public sealed record ActivityLogState(IEnumerable<ActivityLogEntry> Logs)
{
    private ActivityLogState() : this([])
    {
    }
}


public sealed record ActivityLogEntry(
    string Connection,
    string Level,
    string Message,
    DateTime Timestamp);