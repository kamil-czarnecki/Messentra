using Mediator;
using Messentra.Domain;
using Messentra.Features.Explorer.Messages;

namespace Messentra.Features.Mcp.PeekMessages;

public sealed record PeekMessagesQuery(
    long ConnectionId,
    ConnectionConfig Config,
    string ResourceName,
    string? TopicName,
    SubQueue Subqueue,
    int MaxMessages,
    long? FromSequenceNumber) : IQuery<PeekMessagesQueryResult>;
