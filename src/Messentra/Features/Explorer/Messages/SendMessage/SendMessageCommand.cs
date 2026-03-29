using Mediator;
using Messentra.Features.Explorer.Resources;

namespace Messentra.Features.Explorer.Messages.SendMessage;

public sealed record SendMessageCommand(
    ResourceTreeNode ResourceTreeNode,
    string Body,
    string? MessageId,
    string? Label,
    string? CorrelationId,
    string? SessionId,
    string? ReplyToSessionId,
    string? PartitionKey,
    DateTime? ScheduledEnqueueTimeUtc,
    TimeSpan? TimeToLive,
    string? To,
    string? ReplyTo,
    string? ContentType,
    IReadOnlyDictionary<string, object> ApplicationProperties) : ICommand<SendMessageResult>;

