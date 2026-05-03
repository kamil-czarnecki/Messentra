using Azure.Messaging.ServiceBus;
using Mediator;
using Messentra.Features.Explorer.Messages;
using Messentra.Infrastructure.AzureServiceBus.Queues;
using Messentra.Infrastructure.AzureServiceBus.Subscriptions;

namespace Messentra.Features.Mcp.PeekMessages;

public sealed class PeekMessagesQueryHandler(
    IAzureServiceBusQueueMessagesProvider queueMessagesProvider,
    IAzureServiceBusSubscriptionMessagesProvider subscriptionMessagesProvider)
    : IQueryHandler<PeekMessagesQuery, PeekMessagesQueryResult>
{
    public async ValueTask<PeekMessagesQueryResult> Handle(PeekMessagesQuery query, CancellationToken ct)
    {
        var connectionInfo = query.Config.ToConnectionInfo();
        var options = new FetchMessagesOptions(
            FetchMode.Peek,
            FetchReceiveMode.PeekLock,
            query.MaxMessages,
            query.FromSequenceNumber,
            TimeSpan.Zero,
            query.Subqueue);

        try
        {
            var messages = query.TopicName is null
                ? await queueMessagesProvider.Get(connectionInfo, query.ResourceName, options, ct)
                : await subscriptionMessagesProvider.Get(connectionInfo, query.TopicName, query.ResourceName, options, ct);

            var peeked = messages.Select(PeekedMessage.From).ToList().AsReadOnly();
            var nextSeq = peeked.Count == query.MaxMessages
                ? peeked[^1].SequenceNumber + 1
                : (long?)null;

            return new PeekMessagesResult(peeked, nextSeq);
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
            return new McpError($"Resource '{query.ResourceName}' not found.");
        }
    }
}
