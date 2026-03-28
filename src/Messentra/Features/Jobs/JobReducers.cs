using System.Text.Json;
using Fluxor;
using Messentra.Features.Jobs.ExportMessages;
using Messentra.Features.Jobs.ImportMessages;

namespace Messentra.Features.Jobs;

public static class JobReducers
{
    [ReducerMethod]
    public static JobState Reduce(JobState state, FetchJobsAction _) =>
        state with { IsLoading = true };

    [ReducerMethod]
    public static JobState Reduce(JobState state, FetchJobsSuccessAction action) =>
        new(IsLoading: false, IsLoaded: true, Jobs: action.Jobs.Select(JobListItem.FromJob).ToList());

    [ReducerMethod]
    public static JobState Reduce(JobState state, FetchJobsFailureAction _) =>
        state with { IsLoading = false };

    [ReducerMethod]
    public static JobState Reduce(JobState state, JobProgressReceivedAction action)
    {
        var jobs = state.Jobs
            .Select(job =>
                job.Id == action.Update.Id
                    ? job with
                    {
                        Status = action.Update.Status,
                        StageProgress = action.Update.StageProgress ?? job.StageProgress,
                        RetryCount = action.Update.RetryCount,
                        LastError = action.Update.LastError,
                        Output = action.Update.OutputRaw is null
                            ? job.Output
                            : job.Type switch
                            {
                                nameof(ExportMessagesJob) => JsonSerializer.Deserialize<ExportMessagesJobResponse>(action.Update.OutputRaw) is { } export
                                    ? new JobOutput.ExportMessagesJobOutput(export.PathToJson)
                                    : job.Output,
                                nameof(ImportMessagesJob) => null,
                                _ => job.Output
                            }
                    }
                    : job)
            .ToList();

        return state with { Jobs = jobs };
    }

    [ReducerMethod]
    public static JobState Reduce(JobState state, ResumeJobSuccessAction action)
    {
        var jobs = state.Jobs
            .Select(job => job.Id == action.JobId
                ? job with { Status = Domain.JobStatus.Queued, LastError = null }
                : job)
            .ToList();

        return state with { Jobs = jobs };
    }

    [ReducerMethod]
    public static JobState Reduce(JobState state, DeleteJobSuccessAction action)
    {
        var jobs = state.Jobs
            .Where(x => x.Id != action.JobId)
            .ToList();

        return state with { Jobs = jobs };
    }
}
