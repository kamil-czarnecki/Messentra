using Azure.Messaging.ServiceBus;
using Messentra.Features.Explorer.Messages;
using Messentra.Infrastructure.AzureServiceBus.Factories;
using ServiceBusMessage = Messentra.Features.Explorer.Messages.ServiceBusMessage;
using SubQueue = Azure.Messaging.ServiceBus.SubQueue;

namespace Messentra.Infrastructure.AzureServiceBus.Subscriptions;

public interface IAzureServiceBusSubscriptionMessagesProvider
{
    Task<IReadOnlyCollection<ServiceBusMessage>> Get(
        ConnectionInfo info,
        string topicName,
        string subscriptionName,
        FetchMessagesOptions options,
        CancellationToken cancellationToken);
}

public sealed class AzureServiceBusSubscriptionMessagesProvider : AzureServiceBusProviderBase, IAzureServiceBusSubscriptionMessagesProvider
{
    public AzureServiceBusSubscriptionMessagesProvider(IAzureServiceBusClientFactory clientFactory) : base(clientFactory)
    {
    }

    public async Task<IReadOnlyCollection<ServiceBusMessage>> Get(
        ConnectionInfo info,
        string topicName,
        string subscriptionName,
        FetchMessagesOptions options,
        CancellationToken cancellationToken)
    {
        var client = await GetClient(info, cancellationToken);
        var receiver = client.CreateReceiver(topicName, subscriptionName, new ServiceBusReceiverOptions
        {
            ReceiveMode = GetReceiveMode(options),
            SubQueue = options.SubQueue == Features.Explorer.Messages.SubQueue.DeadLetter
                ? SubQueue.DeadLetter
                : SubQueue.None
        });
        var sender = client.CreateSender(topicName);
        var messages = new List<ServiceBusReceivedMessage>();

        while (messages.Count < options.MessageCount)
        {
            var remaining = options.MessageCount - messages.Count;

            var batch = options.Mode == FetchMode.Peek
                ? await receiver.PeekMessagesAsync(options.MessageCount, options.StartSequence, cancellationToken)
                : await receiver.ReceiveMessagesAsync(Math.Min(remaining, 1000), options.WaitTime, cancellationToken);

            if (batch.Count == 0)
                break;

            messages.AddRange(batch);
        }

        return messages.Select(x => Map(receiver, sender, x)).ToList();
    }
}

