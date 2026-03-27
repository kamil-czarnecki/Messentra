using AutoFixture;
using Messentra.Domain;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Domain;

public sealed class JobShould
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void InitializeWithExpectedDefaults_WhenCreated()
    {
        // Arrange
        var sut = CreateJob();

        // Act
        var status = sut.Status;

        // Assert
        status.ShouldBe(JobStatus.Queued);
        sut.StageProgress.ShouldBe(new StageProgress("Initializing", 0));
        sut.CurrentStageIndex.ShouldBe(0);
        sut.RetryCount.ShouldBe(0);
        sut.MaxRetries.ShouldBe(3);
        sut.LastError.ShouldBeNull();
        sut.StartedAt.ShouldBeNull();
        sut.CompletedAt.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateProgressSetStateAndReportUpdate_WhenCalled()
    {
        // Arrange
        var sut = CreateJob();
        var before = DateTime.UtcNow;

        // Act
        var update = await CaptureUpdateAsync(sut, () => sut.UpdateProgress("Fetch", 42));

        // Assert
        sut.StageProgress.ShouldBe(new StageProgress("Fetch", 42));
        sut.UpdatedAt.ShouldBeGreaterThanOrEqualTo(before);
        update.Id.ShouldBe(sut.Id);
        update.Status.ShouldBe(JobStatus.Queued);
        update.StageProgress.ShouldBe(new StageProgress("Fetch", 42));
        update.RetryCount.ShouldBe(0);
        update.LastError.ShouldBeNull();
    }

    [Fact]
    public void SetStartedAtOnlyOnce_WhenRunningStatusUpdatedMultipleTimes()
    {
        // Arrange
        var sut = CreateJob();

        // Act
        sut.UpdateStatus(JobStatus.Running);
        var firstStartedAt = sut.StartedAt;
        sut.UpdateStatus(JobStatus.Running);

        // Assert
        firstStartedAt.ShouldNotBeNull();
        sut.StartedAt.ShouldBe(firstStartedAt);
    }

    [Fact]
    public void SetCompletedAt_WhenStatusUpdatedToCompleted()
    {
        // Arrange
        var sut = CreateJob();
        sut.UpdateStatus(JobStatus.Running);

        // Act
        sut.UpdateStatus(JobStatus.Completed);

        // Assert
        sut.Status.ShouldBe(JobStatus.Completed);
        sut.CompletedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task SetErrorAndReportUpdate_WhenStatusUpdatedToFailed()
    {
        // Arrange
        var sut = CreateJob();

        // Act
        var update = await CaptureUpdateAsync(sut, () => sut.UpdateStatus(JobStatus.Failed, "boom"));

        // Assert
        sut.Status.ShouldBe(JobStatus.Failed);
        sut.LastError.ShouldBe("boom");
        update.Status.ShouldBe(JobStatus.Failed);
        update.LastError.ShouldBe("boom");
    }

    [Fact]
    public async Task UpdateRetryCountAndReportUpdate_WhenCalled()
    {
        // Arrange
        var sut = CreateJob();

        // Act
        var update = await CaptureUpdateAsync(sut, () => sut.UpdateRetryCount(2));

        // Assert
        sut.RetryCount.ShouldBe(2);
        update.RetryCount.ShouldBe(2);
    }

    private static async Task<JobProgressUpdate> CaptureUpdateAsync(TestJob job, Action action)
    {
        var tcs = new TaskCompletionSource<JobProgressUpdate>(TaskCreationOptions.RunContinuationsAsynchronously);
        job.Subscribe(update => tcs.TrySetResult(update));

        action();

        return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));
    }

    private TestJob CreateJob() =>
        new()
        {
            Id = _fixture.Create<long>(),
            Label = _fixture.Create<string>(),
            CreatedAt = DateTime.UtcNow
        };

    private sealed class TestJob : Job
    {
        public override IReadOnlyList<Type> Stages { get; } = [];
    }
}


