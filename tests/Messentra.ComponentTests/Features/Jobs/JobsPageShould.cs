using Bunit;
using Messentra.Domain;
using Messentra.Features.Jobs;
using MudBlazor;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Jobs;

public sealed class JobsPageShould : ComponentTestBase
{
    [Fact]
    public void DispatchFetchJobsOnFirstRender_WhenStateIsNotLoaded()
    {
        // Arrange
        var state = GetState<JobState>();
        state.SetState(new JobState(false, false, []));

        // Act
        _ = Render<JobsPage>();

        // Assert
        MockDispatcher.Verify(x => x.Dispatch(It.IsAny<FetchJobsAction>()), Times.Once);
    }

    [Fact]
    public void ShowFailedErrorInStatusTooltipAndHideErrorColumn_WhenJobsContainErrorsInDifferentStatuses()
    {
        // Arrange
        var state = GetState<JobState>();
        state.SetState(new JobState(false, true,
        [
            CreateJob(1, JobStatus.Failed, "failed-error"),
            CreateJob(2, JobStatus.Running, "running-error")
        ]));

        // Act
        var cut = Render<JobsPage>();

        // Assert
        cut.Markup.ShouldNotContain(">Error<");
        cut.Markup.ShouldNotContain("failed-error");
        cut.Markup.ShouldNotContain("running-error");
        cut.FindComponents<MudTooltip>().Count.ShouldBe(1);
    }

    [Fact]
    public void ShowResumeActionOnlyForPausedJobs()
    {
        // Arrange
        var state = GetState<JobState>();
        state.SetState(new JobState(false, true,
        [
            CreateJob(1, JobStatus.Paused),
            CreateJob(2, JobStatus.Running)
        ]));

        // Act
        var cut = Render<JobsPage>();

        // Assert
        cut.FindAll("button[title='Resume']").Count.ShouldBe(1);
        cut.FindAll("button[title='Pause']").Count.ShouldBe(1);
    }

    [Fact]
    public void ShowDownloadActionOnlyForCompletedExportJobs()
    {
        // Arrange
        var state = GetState<JobState>();
        state.SetState(new JobState(false, true,
        [
            CreateJob(1, JobStatus.Completed, output: new JobOutput.ExportMessagesJobOutput("/tmp/a.json")),
            CreateJob(2, JobStatus.Completed),
            CreateJob(3, JobStatus.Queued)
        ]));

        // Act
        var cut = Render<JobsPage>();

        // Assert
        cut.FindAll("button[title='Download']").Count.ShouldBe(1);
    }

    [Fact]
    public void ShowDeleteActionOnlyForNotRunningJobs()
    {
        // Arrange
        var state = GetState<JobState>();
        state.SetState(new JobState(false, true,
        [
            CreateJob(1, JobStatus.Running),
            CreateJob(2, JobStatus.Paused),
            CreateJob(3, JobStatus.Completed),
            CreateJob(4, JobStatus.Queued)
        ]));

        // Act
        var cut = Render<JobsPage>();

        // Assert
        cut.FindAll("button[title='Delete']").Count.ShouldBe(3);
    }

    private static JobListItem CreateJob(long id, JobStatus status, string? error = null, JobOutput? output = null)
    {
        return new JobListItem(
            Id: id,
            Type: output is JobOutput.ExportMessagesJobOutput ? "ExportMessagesJob" : "ImportMessagesJob",
            Label: $"job-{id}",
            Status: status,
            StageProgress: new StageProgress("Stage", 25),
            RetryCount: 0,
            MaxRetries: 3,
            LastError: error,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow,
            StartedAt: DateTime.UtcNow,
            CompletedAt: status == JobStatus.Completed ? DateTime.UtcNow : null,
            Output: output);
    }
}





