using System.Collections.Concurrent;

namespace Messentra.Features.Jobs;

public interface IJobCancellationRegistry
{
    CancellationTokenSource Register(long jobId);
    void RequestPause(long jobId);
    void Unregister(long jobId);
}

public sealed class JobCancellationRegistry : IJobCancellationRegistry
{
    private readonly ConcurrentDictionary<long, CancellationTokenSource> _tokens = new();

    public CancellationTokenSource Register(long jobId)
    {
        var cts = new CancellationTokenSource();
        _tokens.TryAdd(jobId, cts);
        return cts;
    }

    public void RequestPause(long jobId)
    {
        if (_tokens.TryGetValue(jobId, out var cts))
        {
            cts.Cancel();
        }
    }

    public void Unregister(long jobId)
    {
        if (_tokens.TryRemove(jobId, out var cts))
        {
            cts.Dispose();
        }
    }
}