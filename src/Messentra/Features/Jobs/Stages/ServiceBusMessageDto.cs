using System.Text.Json;
using Messentra.Features.Explorer.Messages;

namespace Messentra.Features.Jobs.Stages;

public sealed record ServiceBusMessageDto(
    object Message,
    ServiceBusProperties Properties,
    IReadOnlyDictionary<string, object> ApplicationProperties)
{
    public static ServiceBusMessageDto From(MessageDto message) =>
        new(
            GetBody(message),
            new ServiceBusProperties(
                message.BrokerProperties.ContentType,
                message.BrokerProperties.CorrelationId,
                message.BrokerProperties.Label,
                message.BrokerProperties.MessageId,
                message.BrokerProperties.To,
                message.BrokerProperties.ReplyTo,
                message.BrokerProperties.TimeToLive,
                message.BrokerProperties.ReplyToSessionId,
                message.BrokerProperties.SessionId,
                message.BrokerProperties.PartitionKey,
                message.BrokerProperties.ScheduledEnqueueTimeUtc,
                null
            ),
            message.ApplicationProperties);
    
    private static object GetBody(MessageDto message) => message switch
    {
        {BrokerProperties.ContentType: "application/json" } => JsonDocument.Parse(message.Body).RootElement,
        _ => message.Body
    };
}
    
public sealed record ServiceBusProperties(
    string? ContentType,
    string? CorrelationId,
    string? Subject,
    string? MessageId,
    string? To,
    string? ReplyTo,
    TimeSpan? TimeToLive,
    string? ReplyToSessionId,
    string? SessionId,
    string? PartitionKey,
    DateTimeOffset? ScheduledEnqueueTime,
    string? TransactionPartitionKey);