using AutoFixture;
using System.Text.Json;
using Messentra.Features.Jobs.ExportSelectedMessages;
using Messentra.Features.Jobs.Stages;
using Messentra.Features.Jobs.Stages.CreateJsonFromMessages;
using Messentra.Features.Jobs.Stages.PersistSelectedMessages;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Jobs.ExportSelectedMessages;

public sealed class ExportSelectedMessagesJobShould
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void ExposeExpectedStagesInOrder_WhenCreated()
    {
        // Arrange
        var sut = CreateJob([]);

        // Act
        var stages = sut.Stages;

        // Assert
        stages.Count.ShouldBe(2);
        stages[0].ShouldBe(typeof(PersistSelectedMessagesStage<ExportSelectedMessagesJob>));
        stages[1].ShouldBe(typeof(CreateJsonFromMessagesStage<ExportSelectedMessagesJob>));
    }

    [Fact]
    public void ReturnSelectedMessages_WhenInputProvided()
    {
        // Arrange
        var messages = new List<ServiceBusMessageDto>
        {
            CreateMessageDto("msg-1"),
            CreateMessageDto("msg-2")
        };
        var sut = CreateJob(messages);

        // Act
        var result = sut.GetSelectedMessages();

        // Assert
        result.Count.ShouldBe(messages.Count);

        for (var index = 0; index < messages.Count; index++)
        {
            result[index].Properties.ShouldBe(messages[index].Properties);
            result[index].ApplicationProperties.ShouldBe(messages[index].ApplicationProperties);
            GetMessageValue(result[index].Message).ShouldBe(GetMessageValue(messages[index].Message));
        }
    }

    [Fact]
    public void ThrowArgumentNullException_WhenInputIsNull()
    {
        // Arrange
        var sut = new ExportSelectedMessagesJob
        {
            Id = _fixture.Create<long>(),
            Label = _fixture.Create<string>(),
            CreatedAt = DateTime.UtcNow,
            Input = null
        };

        // Act
        var action = sut.GetSelectedMessages;

        // Assert
        action.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void SetOutputPath_WhenCreateJsonStageCompleted()
    {
        // Arrange
        var sut = CreateJob([]);

        // Act
        sut.StageCompleted(new CreateJsonStageResult("/tmp/export.json"));

        // Assert
        sut.Output.ShouldNotBeNull();
        sut.Output.PathToJson.ShouldBe("/tmp/export.json");
    }

    private ExportSelectedMessagesJob CreateJob(IReadOnlyList<ServiceBusMessageDto> messages) =>
        new()
        {
            Id = _fixture.Create<long>(),
            Label = _fixture.Create<string>(),
            CreatedAt = DateTime.UtcNow,
            Input = new ExportSelectedMessagesJobRequest(messages, "my-queue-Active")
        };

    private static ServiceBusMessageDto CreateMessageDto(string messageId) =>
        new(
            messageId,
            new ServiceBusProperties(null, null, null, messageId, null, null, null, null, null, null, null, null, null),
            new Dictionary<string, object>());

    private static string? GetMessageValue(object message) =>
        message switch
        {
            JsonElement { ValueKind: JsonValueKind.String } jsonElement => jsonElement.GetString(),
            JsonElement jsonElement => jsonElement.GetRawText(),
            _ => message.ToString()
        };
}
