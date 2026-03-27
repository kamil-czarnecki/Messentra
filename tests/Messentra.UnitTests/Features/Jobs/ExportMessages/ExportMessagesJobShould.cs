using AutoFixture;
using Messentra.Domain;
using Messentra.Features.Jobs;
using Messentra.Features.Jobs.ExportMessages;
using Messentra.Features.Jobs.Stages;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Jobs.ExportMessages;

public sealed class ExportMessagesJobShould
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void ExposeExpectedStagesInOrder_WhenCreated()
    {
        // Arrange
        var sut = CreateJob(CreateConnectionConfig());

        // Act
        var stages = sut.Stages;

        // Assert
        stages.Count.ShouldBe(2);
        stages[0].ShouldBe(typeof(FetchMessagesStage<ExportMessagesJob>));
        stages[1].ShouldBe(typeof(CreateJsonStage<ExportMessagesJob>));
    }

    [Fact]
    public void ReturnMessageFetchConfiguration_WhenInputProvided()
    {
        // Arrange
        var connectionDto = CreateConnectionConfig();
        var target = new ResourceTarget.Queue("queue1");
        var sut = CreateJob(connectionDto, target);

        // Act
        var result = sut.GetMessageFetchConfiguration();

        // Assert
        result.ConnectionConfig.ShouldBe(connectionDto);
        result.Target.ShouldBe(target);
    }

    [Fact]
    public void ThrowArgumentNullException_WhenInputIsNull()
    {
        // Arrange
        var sut = CreateJob(null);

        // Act
        var action = () => sut.GetMessageFetchConfiguration();

        // Assert
        action.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void SetOutputPath_WhenCreateJsonStageCompleted()
    {
        // Arrange
        var sut = CreateJob(CreateConnectionConfig(), new ResourceTarget.Queue("queue1"));

        // Act
        sut.StageCompleted(new CreateJsonStageResult("/tmp/export.json"));

        // Assert
        sut.Output.ShouldNotBeNull();
        sut.Output.PathToJson.ShouldBe("/tmp/export.json");
    }

    private ExportMessagesJob CreateJob(ConnectionConfig? input, ResourceTarget? target = null) =>
        new()
        {
            Id = _fixture.Create<long>(),
            Label = _fixture.Create<string>(),
            CreatedAt = DateTime.UtcNow,
            Input = input is null || target is null ? null : new ExportMessagesJobRequest(input, target)
        };

    private ConnectionConfig CreateConnectionConfig() =>
        new(
            ConnectionType.ConnectionString,
            new ConnectionStringConfig(_fixture.Create<string>()),
            null);
}
