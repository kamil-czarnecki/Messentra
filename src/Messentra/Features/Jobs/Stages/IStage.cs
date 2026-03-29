using Messentra.Domain;

namespace Messentra.Features.Jobs.Stages;

public interface IStage<in TJob> where TJob : Job
{
    Task Run(TJob job, CancellationToken ct);
}

public interface IStage<in TJob, TOutput> : IStage<TJob>
    where TJob : Job, IStageCompletionHandler<TOutput>;

public interface IStageCompletionHandler<in TOutput>
{
    void StageCompleted(TOutput result);
}