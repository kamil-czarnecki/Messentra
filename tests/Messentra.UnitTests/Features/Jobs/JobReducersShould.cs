using System.Text.Json;
using Messentra.Domain;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Jobs;
using Messentra.Features.Jobs.ExportMessages;
using Messentra.Features.Jobs.ImportMessages;
using Messentra.Features.Jobs.Stages.CreateJsonFromMessages;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Jobs;

public sealed class JobReducersShould
{
    [Fact]
    public void SetIsLoadingTrue_WhenReducingFetchJobsAction()
    {
        // Arrange
        var state = new JobState(false, false, []);

        // Act
        var result = JobReducers.Reduce(state, new FetchJobsAction());

        // Assert
        result.IsLoading.ShouldBeTrue();
        result.IsLoaded.ShouldBe(state.IsLoaded);
        result.Jobs.ShouldBe(state.Jobs);
    }

    [Fact]
    public void SetLoadedAndMapJobs_WhenReducingFetchJobsSuccessAction()
    {
        // Arrange
        var exportJob = CreateExportJob(1, "export-job", "/tmp/messages.json");
        var importJob = CreateImportJob(2, "import-job");
        var state = new JobState(true, false, []);

        // Act
        var result = JobReducers.Reduce(state, new FetchJobsSuccessAction([exportJob, importJob]));

        // Assert
        result.IsLoading.ShouldBeFalse();
        result.IsLoaded.ShouldBeTrue();
        result.Jobs.Count.ShouldBe(2);

        var mappedExportJob = result.Jobs.Single(x => x.Id == exportJob.Id);
        mappedExportJob.Output.ShouldBe(new JobOutput.ExportMessagesJobOutput("/tmp/messages.json"));

        var mappedImportJob = result.Jobs.Single(x => x.Id == importJob.Id);
        mappedImportJob.Output.ShouldBeNull();
    }

    [Fact]
    public void SetIsLoadingFalse_WhenReducingFetchJobsFailureAction()
    {
        // Arrange
        var state = new JobState(true, false, []);

        // Act
        var result = JobReducers.Reduce(state, new FetchJobsFailureAction());

        // Assert
        result.IsLoading.ShouldBeFalse();
        result.IsLoaded.ShouldBe(state.IsLoaded);
        result.Jobs.ShouldBe(state.Jobs);
    }

    [Fact]
    public void UpdateOnlyMatchingJobAndMapOutput_WhenReducingJobProgressReceivedAction()
    {
        // Arrange
        var matchingJob = new JobListItem(
            Id: 1,
            Type: nameof(ExportMessagesJob),
            Label: "matching",
            Status: JobStatus.Running,
            StageProgress: new StageProgress("Fetch", 10),
            RetryCount: 0,
            MaxRetries: 3,
            LastError: null,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow,
            StartedAt: DateTime.UtcNow,
            CompletedAt: null,
            Output: null);

        var untouchedJob = new JobListItem(
            Id: 2,
            Type: nameof(ExportMessagesJob),
            Label: "untouched",
            Status: JobStatus.Running,
            StageProgress: new StageProgress("Fetch", 20),
            RetryCount: 1,
            MaxRetries: 3,
            LastError: "old-error",
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow,
            StartedAt: DateTime.UtcNow,
            CompletedAt: null,
            Output: new JobOutput.ExportMessagesJobOutput("/tmp/old.json"));

        var state = new JobState(false, true, [matchingJob, untouchedJob]);
        var outputRaw = JsonSerializer.Serialize(new ExportMessagesJobResponse("/tmp/new.json"));
        var update = new JobProgressUpdate(
            Id: 1,
            Status: JobStatus.Completed,
            StageProgress: null,
            RetryCount: 2,
            LastError: "error-text",
            OutputRaw: outputRaw);

        // Act
        var result = JobReducers.Reduce(state, new JobProgressReceivedAction(update));

        // Assert
        var updatedMatchingJob = result.Jobs.Single(x => x.Id == matchingJob.Id);
        updatedMatchingJob.Status.ShouldBe(JobStatus.Completed);
        updatedMatchingJob.StageProgress.ShouldBe(matchingJob.StageProgress);
        updatedMatchingJob.RetryCount.ShouldBe(2);
        updatedMatchingJob.LastError.ShouldBe("error-text");
        updatedMatchingJob.Output.ShouldBe(new JobOutput.ExportMessagesJobOutput("/tmp/new.json"));

        var stillUntouchedJob = result.Jobs.Single(x => x.Id == untouchedJob.Id);
        stillUntouchedJob.ShouldBe(untouchedJob);
    }

    private static ExportMessagesJob CreateExportJob(long id, string label, string pathToJson)
    {
        var job = new ExportMessagesJob
        {
            Id = id,
            Label = label,
            CreatedAt = DateTime.UtcNow,
            Input = new ExportMessagesJobRequest(
                new ConnectionConfig(
                    ConnectionType.ConnectionString,
                    new ConnectionStringConfig("Endpoint=sb://local/;SharedAccessKeyName=name;SharedAccessKey=key"),
                    null),
                new ResourceTarget.Queue("queue-1", SubQueue.Active),
                10)
        };

        job.StageCompleted(new CreateJsonStageResult(pathToJson));

        return job;
    }

    private static ImportMessagesJob CreateImportJob(long id, string label)
    {
        return new ImportMessagesJob
        {
            Id = id,
            Label = label,
            CreatedAt = DateTime.UtcNow,
            Input = new ImportMessagesJobRequest(
                new ConnectionConfig(
                    ConnectionType.ConnectionString,
                    new ConnectionStringConfig("Endpoint=sb://local/;SharedAccessKeyName=name;SharedAccessKey=key"),
                    null),
                new ResourceTarget.Queue("queue-2", SubQueue.Active))
        };
    }
}



