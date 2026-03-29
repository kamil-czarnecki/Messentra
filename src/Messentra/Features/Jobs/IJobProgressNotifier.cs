using Messentra.Domain;

namespace Messentra.Features.Jobs;

public interface IJobProgressNotifier
{
    IDisposable Subscribe(Action<JobProgressUpdate> callback);
    void Publish(JobProgressUpdate update);
}
