using System.Text.Json;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Jobs.Stages;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Jobs.Stages;

public sealed class ServiceBusMessageDtoShould
{
    [Fact]
    public void ParseJsonBody_WhenContentTypeIsApplicationJsonAndPayloadIsValid()
    {
        // Arrange
        var message = new MessageDto(
            Body: "{\"key\":\"value\"}",
            BrokerProperties: CreateBrokerProperties("application/json"),
            ApplicationProperties: new Dictionary<string, object>());

        // Act
        var result = ServiceBusMessageDto.From(message);

        // Assert
        result.Message.ShouldBeOfType<JsonElement>();
        ((JsonElement)result.Message).GetProperty("key").GetString().ShouldBe("value");
    }

    [Fact]
    public void ReturnRawBody_WhenContentTypeIsApplicationJsonButPayloadIsMalformed()
    {
        // Arrange
        var message = new MessageDto(
            Body: "{ invalid-json",
            BrokerProperties: CreateBrokerProperties("application/json"),
            ApplicationProperties: new Dictionary<string, object>());

        // Act
        var result = ServiceBusMessageDto.From(message);

        // Assert
        result.Message.ShouldBe("{ invalid-json");
    }

    private static BrokerProperties CreateBrokerProperties(string? contentType) => new(
        MessageId: "msg-1",
        SequenceNumber: 1,
        CorrelationId: null,
        SessionId: null,
        ReplyToSessionId: null,
        EnqueuedTimeUtc: DateTime.UtcNow,
        ScheduledEnqueueTimeUtc: DateTime.UtcNow,
        TimeToLive: TimeSpan.FromMinutes(5),
        LockedUntilUtc: DateTime.UtcNow,
        ExpiresAtUtc: DateTime.UtcNow.AddHours(1),
        DeliveryCount: 1,
        Label: null,
        To: null,
        ReplyTo: null,
        PartitionKey: null,
        ContentType: contentType,
        DeadLetterReason: null,
        DeadLetterErrorDescription: null);
}


