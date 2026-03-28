using Fluxor;
using Messentra.Domain;
using Messentra.Features.Jobs.ExportMessages;
using Messentra.Features.Jobs.ImportMessages;

namespace Messentra.Features.Jobs;

[FeatureState]
public sealed record JobState(bool IsLoading, bool IsLoaded, IReadOnlyCollection<JobListItem> Jobs)
{
    private JobState() : this(false, false, [])
    {
    }
}

public sealed record JobListItem(
    long Id,
    string Type,
    string Label,
    JobStatus Status,
    StageProgress StageProgress,
    int RetryCount,
    int MaxRetries,
    string? LastError,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    JobOutput? Output)
{
    public static JobListItem FromJob(Job job) =>
        new(
            job.Id,
            job.Type,
            job.Label,
            job.Status,
            job.StageProgress,
            job.RetryCount,
            job.MaxRetries,
            job.LastError,
            job.CreatedAt,
            job.UpdatedAt,
            job.StartedAt,
            job.CompletedAt,
            GetOutput(job));

    private static JobOutput? GetOutput(Job job) =>
        job switch
        {
            ImportMessagesJob importMessagesJob => null,
            ExportMessagesJob exportMessagesJob =>
                exportMessagesJob.Output is null
                    ? null
                    : new JobOutput.ExportMessagesJobOutput(exportMessagesJob.Output.PathToJson),
            _ => throw new ArgumentOutOfRangeException(nameof(job))
        };
}

public abstract record JobOutput
{
    public sealed record ExportMessagesJobOutput(string FilePath) : JobOutput;
}