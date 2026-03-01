using Mediator;
using Messentra.Domain;

namespace Messentra.Features.Explorer.Messages.FetchQueueMessages;

public sealed record FetchQueueMessagesQuery(
    string QueueName,
    ConnectionConfig ConnectionConfig,
    FetchMessagesOptions Options) : IQuery<IReadOnlyCollection<ServiceBusMessage>>;