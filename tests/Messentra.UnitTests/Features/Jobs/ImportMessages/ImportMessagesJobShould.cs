using AutoFixture;
using Messentra.Domain;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Jobs;
using Messentra.Features.Jobs.ImportMessages;
using Messentra.Features.Jobs.Stages.ImportMessagesFromJson;
using Messentra.Features.Jobs.Stages.SendImportedMessages;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Jobs.ImportMessages;

public sealed class ImportMessagesJobShould
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void ExposeExpectedStagesInOrder_WhenCreated()
    {
        // Arrange
        var sut = CreateJob(new ResourceTarget.Queue("queue1", SubQueue.Active));

        // Act
        var stages = sut.Stages;

        // Assert
        stages.Count.ShouldBe(2);
        stages[0].ShouldBe(typeof(PrepareMessagesFromJsonStage<ImportMessagesJob>));
        stages[1].ShouldBe(typeof(SendImportedMessagesStage<ImportMessagesJob>));
    }

    [Fact]
    public void RoundTripQueueTarget_WhenInputInitialized()
    {
        // Arrange
        var target = new ResourceTarget.Queue("queue1", SubQueue.Active);
        var sut = CreateJob(target, generateNewMessageId: true);

        // Act
        var input = sut.Input;

        // Assert
        input.ShouldNotBeNull();
        input.Target.ShouldBe(target);
        input.SourceFilePath.ShouldBe("/tmp/import.json");
        input.SourceFileHash.ShouldBe("HASH");
        input.GenerateNewMessageId.ShouldBeTrue();
    }

    [Fact]
    public void RoundTripTopicSubscriptionTarget_WhenInputInitialized()
    {
        // Arrange
        var target = new ResourceTarget.TopicSubscription("topic-a", "sub-a", SubQueue.Active);
        var sut = CreateJob(target);

        // Act
        var input = sut.Input;

        // Assert
        input.ShouldNotBeNull();
        input.Target.ShouldBe(target);
        input.SourceFilePath.ShouldBe("/tmp/import.json");
        input.SourceFileHash.ShouldBe("HASH");
        input.GenerateNewMessageId.ShouldBeFalse();
    }

    private ImportMessagesJob CreateJob(ResourceTarget target, bool generateNewMessageId = false) =>
        new()
        {
            Id = _fixture.Create<long>(),
            Label = _fixture.Create<string>(),
            CreatedAt = DateTime.UtcNow,
            Input = new ImportMessagesJobRequest(CreateConnectionConfig(), target, "/tmp/import.json", "HASH", generateNewMessageId)
        };

    private ConnectionConfig CreateConnectionConfig() =>
        new(
            ConnectionType.ConnectionString,
            new ConnectionStringConfig(_fixture.Create<string>()),
            null);
}

