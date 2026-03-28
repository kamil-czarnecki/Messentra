namespace Messentra.Features.Jobs;

public interface IBackgroundJobQueue
{
    Task Enqueue(long jobId, CancellationToken cancellationToken);
    ValueTask<long> Dequeue(CancellationToken cancellationToken);
}