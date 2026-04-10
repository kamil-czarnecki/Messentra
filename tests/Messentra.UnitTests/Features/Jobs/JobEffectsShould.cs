using Fluxor;
using Mediator;
using Messentra.Domain;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Jobs;
using Messentra.Features.Jobs.DeleteJob;
using Messentra.Features.Jobs.EnqueueJob;
using Messentra.Features.Jobs.ExportMessages;
using Messentra.Features.Jobs.ExportMessages.CreateExportMessagesJob;
using Messentra.Features.Jobs.ExportSelectedMessages;
using Messentra.Features.Jobs.ExportSelectedMessages.CreateExportSelectedMessagesJob;
using Messentra.Features.Jobs.GetJobs;
using Messentra.Features.Jobs.ImportMessages;
using Messentra.Features.Jobs.ImportMessages.CreateImportMessagesJob;
using Messentra.Features.Jobs.PauseJob;
using Messentra.Features.Jobs.ResumeJob;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Messentra.UnitTests.Features.Jobs;

public sealed class JobEffectsShould
{
    [Fact]
    public async Task DispatchFetchJobsSuccessAction_WhenMediatorReturnsJobs()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<JobEffects>>();
        var dispatcher = new Mock<IDispatcher>();
        var expectedJobs = new List<Job> { CreateExportJob(1, "job-1") };

        mediator
            .Setup(x => x.Send(It.IsAny<GetJobsQuery>(), CancellationToken.None))
            .ReturnsAsync(expectedJobs);
        
        var sut = new JobEffects(mediator.Object, logger.Object);

        // Act
        await sut.HandleFetchJobs(new FetchJobsAction(), dispatcher.Object);

        // Assert
        mediator.Verify(x => x.Send(It.IsAny<GetJobsQuery>(), CancellationToken.None), Times.Once);
        dispatcher.Verify(
            x => x.Dispatch(It.Is<FetchJobsSuccessAction>(a => a.Jobs.SequenceEqual(expectedJobs))),
            Times.Once);
        dispatcher.Verify(x => x.Dispatch(It.IsAny<FetchJobsFailureAction>()), Times.Never);
    }

    [Fact]
    public async Task DispatchFetchJobsFailureActionAndLogError_WhenMediatorThrows()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<JobEffects>>();
        var dispatcher = new Mock<IDispatcher>();
        var expectedException = new InvalidOperationException("boom");

        mediator
            .Setup(x => x.Send(It.IsAny<GetJobsQuery>(), CancellationToken.None))
            .ThrowsAsync(expectedException);
        
        var sut = new JobEffects(mediator.Object, logger.Object);

        // Act
        await sut.HandleFetchJobs(new FetchJobsAction(), dispatcher.Object);

        // Assert
        dispatcher.Verify(x => x.Dispatch(It.IsAny<FetchJobsFailureAction>()), Times.Once);
        dispatcher.Verify(x => x.Dispatch(It.IsAny<FetchJobsSuccessAction>()), Times.Never);
        logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Failed to fetch jobs.")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DispatchPauseJobSuccessAction_WhenPauseCommandReturnsTrue()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<JobEffects>>();
        var dispatcher = new Mock<IDispatcher>();
        const long jobId = 42;

        mediator
            .Setup(x => x.Send(It.Is<PauseJobCommand>(c => c.JobId == jobId), CancellationToken.None))
            .ReturnsAsync(true);

        var sut = new JobEffects(mediator.Object, logger.Object);

        // Act
        await sut.HandlePauseJob(new PauseJobAction(jobId), dispatcher.Object);

        // Assert
        dispatcher.Verify(x => x.Dispatch(new PauseJobSuccessAction(jobId)), Times.Once);
        dispatcher.Verify(x => x.Dispatch(new PauseJobFailureAction(jobId)), Times.Never);
    }

    [Fact]
    public async Task DispatchPauseJobFailureAction_WhenPauseCommandThrows()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<JobEffects>>();
        var dispatcher = new Mock<IDispatcher>();
        const long jobId = 42;

        mediator
            .Setup(x => x.Send(It.Is<PauseJobCommand>(c => c.JobId == jobId), CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("pause-error"));

        var sut = new JobEffects(mediator.Object, logger.Object);

        // Act
        await sut.HandlePauseJob(new PauseJobAction(jobId), dispatcher.Object);

        // Assert
        dispatcher.Verify(x => x.Dispatch(new PauseJobFailureAction(jobId)), Times.Once);
    }

    [Fact]
    public async Task DispatchResumeJobSuccessAction_WhenResumeCommandReturnsTrue()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<JobEffects>>();
        var dispatcher = new Mock<IDispatcher>();
        const long jobId = 77;

        mediator
            .Setup(x => x.Send(It.Is<ResumeJobCommand>(c => c.JobId == jobId), CancellationToken.None))
            .ReturnsAsync(true);

        var sut = new JobEffects(mediator.Object, logger.Object);

        // Act
        await sut.HandleResumeJob(new ResumeJobAction(jobId), dispatcher.Object);

        // Assert
        dispatcher.Verify(x => x.Dispatch(new ResumeJobSuccessAction(jobId)), Times.Once);
        dispatcher.Verify(x => x.Dispatch(new ResumeJobFailureAction(jobId)), Times.Never);
    }

    [Fact]
    public async Task DispatchResumeJobFailureAction_WhenResumeCommandThrows()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<JobEffects>>();
        var dispatcher = new Mock<IDispatcher>();
        const long jobId = 77;

        mediator
            .Setup(x => x.Send(It.Is<ResumeJobCommand>(c => c.JobId == jobId), CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("resume-error"));

        var sut = new JobEffects(mediator.Object, logger.Object);

        // Act
        await sut.HandleResumeJob(new ResumeJobAction(jobId), dispatcher.Object);

        // Assert
        dispatcher.Verify(x => x.Dispatch(new ResumeJobFailureAction(jobId)), Times.Once);
    }

    [Fact]
    public async Task DispatchDeleteJobSuccessAction_WhenDeleteCommandReturnsTrue()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<JobEffects>>();
        var dispatcher = new Mock<IDispatcher>();
        const long jobId = 88;

        mediator
            .Setup(x => x.Send(It.Is<DeleteJobCommand>(c => c.JobId == jobId), CancellationToken.None))
            .ReturnsAsync(true);

        var sut = new JobEffects(mediator.Object, logger.Object);

        // Act
        await sut.HandleDeleteJob(new DeleteJobAction(jobId), dispatcher.Object);

        // Assert
        dispatcher.Verify(x => x.Dispatch(new DeleteJobSuccessAction(jobId)), Times.Once);
        dispatcher.Verify(x => x.Dispatch(new DeleteJobFailureAction(jobId)), Times.Never);
    }

    [Fact]
    public async Task DispatchDeleteJobFailureAction_WhenDeleteCommandThrows()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<JobEffects>>();
        var dispatcher = new Mock<IDispatcher>();
        const long jobId = 88;

        mediator
            .Setup(x => x.Send(It.Is<DeleteJobCommand>(c => c.JobId == jobId), CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("delete-error"));

        var sut = new JobEffects(mediator.Object, logger.Object);

        // Act
        await sut.HandleDeleteJob(new DeleteJobAction(jobId), dispatcher.Object);

        // Assert
        dispatcher.Verify(x => x.Dispatch(new DeleteJobFailureAction(jobId)), Times.Once);
    }

    [Fact]
    public async Task DispatchJobCreatedActionAndEnqueueJob_WhenEnqueueExportMessagesSucceeds()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<JobEffects>>();
        var dispatcher = new Mock<IDispatcher>();
        var expectedJob = CreateJobListItem(1);
        var request = new ExportMessagesJobRequest(
            new ConnectionConfig(ConnectionType.ConnectionString,
                new ConnectionStringConfig("Endpoint=sb://local/;SharedAccessKeyName=name;SharedAccessKey=key"), null),
            new ResourceTarget.Queue("queue-1", SubQueue.Active),
            10);

        mediator
            .Setup(x => x.Send(It.IsAny<CreateExportMessagesJobCommand>(), CancellationToken.None))
            .ReturnsAsync(expectedJob);

        var sut = new JobEffects(mediator.Object, logger.Object);

        // Act
        await sut.HandleEnqueueExportMessages(new EnqueueExportMessagesAction(request), dispatcher.Object);

        // Assert
        dispatcher.Verify(x => x.Dispatch(new JobCreatedAction(expectedJob)), Times.Once);
        mediator.Verify(x => x.Send(It.Is<EnqueueJobCommand>(c => c.JobId == expectedJob.Id), CancellationToken.None), Times.Once);
        dispatcher.Verify(x => x.Dispatch(It.IsAny<EnqueueExportMessagesFailureAction>()), Times.Never);
    }

    [Fact]
    public async Task DispatchEnqueueExportMessagesFailureAction_WhenMediatorThrows()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<JobEffects>>();
        var dispatcher = new Mock<IDispatcher>();
        var request = new ExportMessagesJobRequest(
            new ConnectionConfig(ConnectionType.ConnectionString,
                new ConnectionStringConfig("Endpoint=sb://local/;SharedAccessKeyName=name;SharedAccessKey=key"), null),
            new ResourceTarget.Queue("queue-1", SubQueue.Active),
            10);

        mediator
            .Setup(x => x.Send(It.IsAny<CreateExportMessagesJobCommand>(), CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("create-error"));

        var sut = new JobEffects(mediator.Object, logger.Object);

        // Act
        await sut.HandleEnqueueExportMessages(new EnqueueExportMessagesAction(request), dispatcher.Object);

        // Assert
        dispatcher.Verify(x => x.Dispatch(It.IsAny<EnqueueExportMessagesFailureAction>()), Times.Once);
        dispatcher.Verify(x => x.Dispatch(It.IsAny<JobCreatedAction>()), Times.Never);
    }

    [Fact]
    public async Task DispatchJobCreatedActionAndEnqueueJob_WhenEnqueueImportMessagesSucceeds()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<JobEffects>>();
        var dispatcher = new Mock<IDispatcher>();
        var expectedJob = CreateJobListItem(2);
        var request = new ImportMessagesJobRequest(
            new ConnectionConfig(ConnectionType.ConnectionString,
                new ConnectionStringConfig("Endpoint=sb://local/;SharedAccessKeyName=name;SharedAccessKey=key"), null),
            new ResourceTarget.Queue("queue-1", SubQueue.Active),
            "/tmp/import.json",
            "hash");

        mediator
            .Setup(x => x.Send(It.IsAny<CreateImportMessagesJobCommand>(), CancellationToken.None))
            .ReturnsAsync(expectedJob);

        var sut = new JobEffects(mediator.Object, logger.Object);

        // Act
        await sut.HandleEnqueueImportMessages(new EnqueueImportMessagesAction(request), dispatcher.Object);

        // Assert
        dispatcher.Verify(x => x.Dispatch(new JobCreatedAction(expectedJob)), Times.Once);
        mediator.Verify(x => x.Send(It.Is<EnqueueJobCommand>(c => c.JobId == expectedJob.Id), CancellationToken.None), Times.Once);
        dispatcher.Verify(x => x.Dispatch(It.IsAny<EnqueueImportMessagesFailureAction>()), Times.Never);
    }

    [Fact]
    public async Task DispatchEnqueueImportMessagesFailureAction_WhenMediatorThrows()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<JobEffects>>();
        var dispatcher = new Mock<IDispatcher>();
        var request = new ImportMessagesJobRequest(
            new ConnectionConfig(ConnectionType.ConnectionString,
                new ConnectionStringConfig("Endpoint=sb://local/;SharedAccessKeyName=name;SharedAccessKey=key"), null),
            new ResourceTarget.Queue("queue-1", SubQueue.Active),
            "/tmp/import.json",
            "hash");

        mediator
            .Setup(x => x.Send(It.IsAny<CreateImportMessagesJobCommand>(), CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("create-error"));

        var sut = new JobEffects(mediator.Object, logger.Object);

        // Act
        await sut.HandleEnqueueImportMessages(new EnqueueImportMessagesAction(request), dispatcher.Object);

        // Assert
        dispatcher.Verify(x => x.Dispatch(It.IsAny<EnqueueImportMessagesFailureAction>()), Times.Once);
        dispatcher.Verify(x => x.Dispatch(It.IsAny<JobCreatedAction>()), Times.Never);
    }

    [Fact]
    public async Task DispatchJobCreatedActionAndEnqueueJob_WhenEnqueueExportSelectedMessagesSucceeds()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<JobEffects>>();
        var dispatcher = new Mock<IDispatcher>();
        var expectedJob = CreateJobListItem(3);
        var request = new ExportSelectedMessagesJobRequest([], "queue-1");

        mediator
            .Setup(x => x.Send(It.IsAny<CreateExportSelectedMessagesJobCommand>(), CancellationToken.None))
            .ReturnsAsync(expectedJob);

        var sut = new JobEffects(mediator.Object, logger.Object);

        // Act
        await sut.HandleEnqueueExportSelectedMessages(new EnqueueExportSelectedMessagesAction(request), dispatcher.Object);

        // Assert
        dispatcher.Verify(x => x.Dispatch(new JobCreatedAction(expectedJob)), Times.Once);
        mediator.Verify(x => x.Send(It.Is<EnqueueJobCommand>(c => c.JobId == expectedJob.Id), CancellationToken.None), Times.Once);
        dispatcher.Verify(x => x.Dispatch(It.IsAny<EnqueueExportSelectedMessagesFailureAction>()), Times.Never);
    }

    [Fact]
    public async Task DispatchEnqueueExportSelectedMessagesFailureAction_WhenMediatorThrows()
    {
        // Arrange
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<JobEffects>>();
        var dispatcher = new Mock<IDispatcher>();
        var request = new ExportSelectedMessagesJobRequest([], "queue-1");

        mediator
            .Setup(x => x.Send(It.IsAny<CreateExportSelectedMessagesJobCommand>(), CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("create-error"));

        var sut = new JobEffects(mediator.Object, logger.Object);

        // Act
        await sut.HandleEnqueueExportSelectedMessages(new EnqueueExportSelectedMessagesAction(request), dispatcher.Object);

        // Assert
        dispatcher.Verify(x => x.Dispatch(It.IsAny<EnqueueExportSelectedMessagesFailureAction>()), Times.Once);
        dispatcher.Verify(x => x.Dispatch(It.IsAny<JobCreatedAction>()), Times.Never);
    }

    private static JobListItem CreateJobListItem(long id) =>
        new(
            Id: id,
            Type: nameof(ExportMessagesJob),
            Label: "test-job",
            Status: JobStatus.Queued,
            StageProgress: new StageProgress(string.Empty, 0),
            RetryCount: 0,
            MaxRetries: 3,
            LastError: null,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow,
            StartedAt: null,
            CompletedAt: null,
            Output: null);

    private static ExportMessagesJob CreateExportJob(long id, string label)
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

        return job;
    }
}



