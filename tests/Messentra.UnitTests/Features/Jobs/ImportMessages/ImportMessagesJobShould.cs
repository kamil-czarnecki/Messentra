using AutoFixture;
using Messentra.Domain;
using Messentra.Features.Jobs;
using Messentra.Features.Jobs.ExportMessages;
using Messentra.Features.Jobs.ImportMessages;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Jobs.ImportMessages;

public sealed class ImportMessagesJobShould
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void ExposeNoStages_WhenCreated()
    {
        // Arrange
        var sut = CreateJob(new ResourceTarget.Queue("queue1"));

        // Act
        var stages = sut.Stages;

        // Assert
        stages.ShouldBeEmpty();
    }

    [Fact]
    public void RoundTripQueueTarget_WhenInputInitialized()
    {
        // Arrange
        var target = new ResourceTarget.Queue("queue1");
        var sut = CreateJob(target);

        // Act
        var input = sut.Input;

        // Assert
        input.ShouldNotBeNull();
        input.Target.ShouldBe(target);
    }

    [Fact]
    public void RoundTripTopicSubscriptionTarget_WhenInputInitialized()
    {
        // Arrange
        var target = new ResourceTarget.TopicSubscription("topic-a", "sub-a");
        var sut = CreateJob(target);

        // Act
        var input = sut.Input;

        // Assert
        input.ShouldNotBeNull();
        input.Target.ShouldBe(target);
    }

    private ImportMessagesJob CreateJob(ResourceTarget target) =>
        new()
        {
            Id = _fixture.Create<long>(),
            Label = _fixture.Create<string>(),
            CreatedAt = DateTime.UtcNow,
            Input = new ImportMessagesJobRequest(CreateConnectionConfig(), target)
        };

    private ConnectionConfig CreateConnectionConfig() =>
        new(
            ConnectionType.ConnectionString,
            new ConnectionStringConfig(_fixture.Create<string>()),
            null);
}

