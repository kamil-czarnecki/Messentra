using Mediator;
using Messentra.Domain;

namespace Messentra.Features.Explorer.Messages.FetchSubscriptionMessages;

public sealed record FetchSubscriptionMessagesQuery(
    string TopicName,
    string SubscriptionName,
    ConnectionConfig ConnectionConfig,
    FetchMessagesOptions Options) : IQuery<IReadOnlyCollection<ServiceBusMessage>>;

