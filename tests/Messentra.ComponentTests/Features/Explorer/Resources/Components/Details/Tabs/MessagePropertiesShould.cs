using Messentra.Features.Explorer.Messages;
using Messentra.Features.Explorer.Resources.Components.Details.Tabs;
using Messentra.Infrastructure.AzureServiceBus;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Explorer.Resources.Components.Details.Tabs;

public sealed class MessagePropertiesShould : ComponentTestBase
{
    [Fact]
    public void RenderCustomPropertiesWithLowercaseBooleanAndIsoUtcDate()
    {
        // Arrange
        var customDate = new DateTime(2026, 3, 29, 11, 6, 13, 630, DateTimeKind.Utc);
        var message = BuildServiceBusMessage(new Dictionary<string, object>
        {
            ["isEnabled"] = false,
            ["createdAt"] = customDate
        });

        // Act
        var cut = Render<MessageProperties>(p => p
            .Add(x => x.Message, message)
            .Add(x => x.SubQueue, SubQueue.Active));

        // Assert
        cut.Markup.ShouldContain("isEnabled");
        cut.Markup.ShouldContain("false");
        cut.Markup.ShouldContain("createdAt");
        cut.Markup.ShouldContain("2026-03-29T11:06:13.630Z");
    }

    private static ServiceBusMessage BuildServiceBusMessage(IReadOnlyDictionary<string, object> appProperties)
    {
        var brokerProperties = new BrokerProperties(
            MessageId: "msg-1",
            SequenceNumber: 1,
            CorrelationId: null,
            SessionId: null,
            ReplyToSessionId: null,
            EnqueuedTimeUtc: DateTime.UtcNow,
            ScheduledEnqueueTimeUtc: DateTime.UtcNow,
            TimeToLive: TimeSpan.FromDays(1),
            LockedUntilUtc: DateTime.UtcNow.AddMinutes(5),
            ExpiresAtUtc: DateTime.UtcNow.AddDays(1),
            DeliveryCount: 1,
            Label: null,
            To: null,
            ReplyTo: null,
            PartitionKey: null,
            ContentType: null,
            DeadLetterReason: null,
            DeadLetterErrorDescription: null);

        var dto = new MessageDto("Body", brokerProperties, appProperties);
        return new ServiceBusMessage(dto, new Mock<IServiceBusMessageContext>().Object);
    }
}

