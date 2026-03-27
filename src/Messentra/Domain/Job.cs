namespace Messentra.Domain;

public abstract class Job
{
    public long Id { get; init; }
    protected string Type { get; init; } = null!;
    public required string Label { get; init; }
    public JobStatus Status { get; private set; } = JobStatus.Queued;

    public StageProgress StageProgress { get; private set; } = new("Initializing", 0);
    protected IProgress<JobProgressUpdate> ProgressReporter { get; private set; } = new Progress<JobProgressUpdate>();
    public int CurrentStageIndex { get; set; }

    protected string? InputRaw { get; init; }
    protected string? OutputRaw { get; set; }

    public int RetryCount { get; private set; }
    public int MaxRetries { get; init; } = 3;

    public string? LastError { get; private set; }

    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public abstract IReadOnlyList<Type> Stages { get; }

    public void Subscribe(Action<JobProgressUpdate> callback) =>
        ProgressReporter = new Progress<JobProgressUpdate>(callback);
    
    public void UpdateProgress(string stage, int progress)
    {
        StageProgress = new StageProgress(stage, progress);
        UpdatedAt = DateTime.UtcNow;
        
        var update = new JobProgressUpdate(Id, Status, StageProgress, RetryCount, LastError);
        
        ProgressReporter.Report(update);
    }
    
    public void UpdateStatus(JobStatus newStatus, string? error = null)
    {
        var now = DateTime.UtcNow;
        
        Status = newStatus;
        LastError = error;
        UpdatedAt = now;

        switch (Status)
        {
            case JobStatus.Running:
                StartedAt ??= now;
                break;
            case JobStatus.Completed:
                CompletedAt = now;
                break;
        }

        var update = new JobProgressUpdate(Id, Status, StageProgress, RetryCount, LastError);
        
        ProgressReporter.Report(update);
    }
    
    public void UpdateRetryCount(int newRetryCount)
    {
        RetryCount = newRetryCount;
        UpdatedAt = DateTime.UtcNow;
        
        ProgressReporter.Report(new JobProgressUpdate(Id, Status, StageProgress, RetryCount, LastError));
    }
}

public sealed record JobProgressUpdate(
    long Id,
    JobStatus Status,
    StageProgress? StageProgress,
    int RetryCount = 0,
    string? LastError = null
);

public sealed record StageProgress(string Stage, int Progress);

public enum JobStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    Paused
}