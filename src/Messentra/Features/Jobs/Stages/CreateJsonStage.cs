using Messentra.Domain;

namespace Messentra.Features.Jobs.Stages;

public sealed record CreateJsonStageResult(string FilePath);

public sealed class CreateJsonStage<TJob> : IStage<TJob, CreateJsonStageResult>
    where TJob : Job, IStageCompletionHandler<CreateJsonStageResult>
{
    public Task Run(TJob job, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}