using Azure.Messaging.ServiceBus;
using Mediator;
using Messentra.Features.Explorer.Messages;
using Messentra.Infrastructure.AzureServiceBus.Queues;
using Messentra.Infrastructure.AzureServiceBus.Subscriptions;
using SubQueue = Messentra.Features.Explorer.Messages.SubQueue;

namespace Messentra.Features.Mcp.GetDlqSummary;

public sealed class GetDlqSummaryQueryHandler(
    IAzureServiceBusQueueMessagesProvider queueMessagesProvider,
    IAzureServiceBusSubscriptionMessagesProvider subscriptionMessagesProvider)
    : IQueryHandler<GetDlqSummaryQuery, GetDlqSummaryQueryResult>
{
    public async ValueTask<GetDlqSummaryQueryResult> Handle(GetDlqSummaryQuery query, CancellationToken ct)
    {
        var connectionInfo = query.Config.ToConnectionInfo();
        var options = new FetchMessagesOptions(
            FetchMode.Peek,
            FetchReceiveMode.PeekLock,
            query.SampleSize,
            StartSequence: query.FromSequenceNumber,
            TimeSpan.Zero,
            SubQueue.DeadLetter);

        try
        {
            var messages = (query.TopicName is null
                ? await queueMessagesProvider.Get(connectionInfo, query.ResourceName, options, ct)
                : await subscriptionMessagesProvider.Get(connectionInfo, query.TopicName, query.ResourceName, options, ct))
                .ToList();

            var nextSeq = messages.Count == query.SampleSize
                ? messages.Last().Message.BrokerProperties.SequenceNumber + 1
                : (long?)null;

            var groups = messages
                .GroupBy(m => (
                    m.Message.BrokerProperties.DeadLetterReason,
                    m.Message.BrokerProperties.DeadLetterErrorDescription))
                .OrderByDescending(g => g.Count())
                .Select(g => new DlqReasonGroup(
                    g.Key.DeadLetterReason,
                    g.Key.DeadLetterErrorDescription,
                    g.Count(),
                    g.First().Message.Body))
                .ToList();

            return new DlqSummaryResult(messages.Count, nextSeq, groups);
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
            return new McpError($"Resource '{query.ResourceName}' not found.");
        }
    }
}
