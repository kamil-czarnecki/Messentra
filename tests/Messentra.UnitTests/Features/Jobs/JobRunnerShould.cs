using AutoFixture;
using Messentra.Domain;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Jobs;
using Messentra.Features.Jobs.ExportMessages;
using Messentra.Features.Jobs.Stages;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Jobs;

public sealed class JobRunnerShould : InMemoryDbTestBase
{
    private readonly Fixture _fixture = new();

    [Fact]
    public async Task CompleteAndPersistJob_WhenJobExists()
    {
        // Arrange
        var registry = new Mock<IJobCancellationRegistry>();
        registry.Setup(x => x.Register(It.IsAny<long>())).Returns(new CancellationTokenSource());

        var sut = CreateSut(registry.Object);
        var job = CreateJob();

        DbContext.Set<Job>().Add(job);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await sut.Run(job.Id, CancellationToken.None);

        DbContext.ChangeTracker.Clear();
        var savedJob = await DbContext.Set<Job>()
            .SingleAsync(x => x.Id == job.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        savedJob.Status.ShouldBe(JobStatus.Completed);
        savedJob.StartedAt.ShouldNotBeNull();
        savedJob.CompletedAt.ShouldNotBeNull();
        savedJob.CurrentStageIndex.ShouldBe(job.Stages.Count - 1);
        registry.Verify(x => x.Register(job.Id), Times.Once);
        registry.Verify(x => x.Unregister(job.Id), Times.Once);
    }

    [Fact]
    public async Task ThrowInvalidOperationException_WhenJobDoesNotExist()
    {
        // Arrange
        var registry = new Mock<IJobCancellationRegistry>();
        var sut = CreateSut(registry.Object);
        var missingJobId = _fixture.Create<long>();

        // Act
        var action = () => sut.Run(missingJobId, CancellationToken.None);

        // Assert
        await action.ShouldThrowAsync<InvalidOperationException>();
        registry.Verify(x => x.Register(It.IsAny<long>()), Times.Never);
        registry.Verify(x => x.Unregister(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task ResumeFromCurrentStageIndex_WhenJobAlreadyFinished()
    {
        // Arrange
        var registry = new Mock<IJobCancellationRegistry>();
        registry.Setup(x => x.Register(It.IsAny<long>())).Returns(new CancellationTokenSource());

        var sut = CreateSut(registry.Object);
        var job = CreateJob();

        DbContext.Set<Job>().Add(job);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await sut.Run(job.Id, CancellationToken.None);

        DbContext.ChangeTracker.Clear();
        var savedJob = await DbContext.Set<Job>()
            .SingleAsync(x => x.Id == job.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        savedJob.Status.ShouldBe(JobStatus.Completed);
        savedJob.CurrentStageIndex.ShouldBe(job.Stages.Count - 1);
        registry.Verify(x => x.Unregister(job.Id), Times.Once);
    }

    private JobRunner CreateSut(IJobCancellationRegistry registry)
    {
        var serviceFactoryMock = new Mock<IServiceScopeFactory>();
        var serviceScopeMock = new Mock<IServiceScope>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(It.IsAny<Type>()))
            .Returns((Type t) =>
            {
                if (t.GetInterfaces()
                    .Any(i => i.IsGenericType &&
                              i.GetGenericTypeDefinition() == typeof(IStage<>)))
                    return Mock.Of<IStage<Job>>();

                if (t == typeof(MessentraDbContext))
                    return DbContext;

                if (t == typeof(IJobCancellationRegistry))
                    return registry;

                return null;
            });
        serviceScopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
        serviceFactoryMock.Setup(x => x.CreateScope()).Returns(serviceScopeMock.Object);
        
        return new JobRunner(serviceFactoryMock.Object);
    }

    private ExportMessagesJob CreateJob()
    {
        return new ExportMessagesJob
        {
            Id = _fixture.Create<long>(),
            Label = _fixture.Create<string>(),
            CreatedAt = DateTime.UtcNow,
            Input = new ExportMessagesJobRequest(CreateConnectionConfig(), new ResourceTarget.Queue("queue1", SubQueue.Active), 100)
        };
    }

    private ConnectionConfig CreateConnectionConfig() =>
        new(
            ConnectionType.ConnectionString,
            new ConnectionStringConfig(_fixture.Create<string>()),
            null);


}
