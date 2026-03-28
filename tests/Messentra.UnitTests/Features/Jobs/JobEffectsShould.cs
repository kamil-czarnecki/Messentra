using Fluxor;
using Mediator;
using Messentra.Domain;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Jobs;
using Messentra.Features.Jobs.ExportMessages;
using Messentra.Features.Jobs.GetJobs;
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



