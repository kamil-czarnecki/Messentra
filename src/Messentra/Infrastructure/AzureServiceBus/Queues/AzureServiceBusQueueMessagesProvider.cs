using Azure.Messaging.ServiceBus;
using Messentra.Features.Explorer.Messages;
using Messentra.Infrastructure.AzureServiceBus.Factories;
using ServiceBusMessage = Messentra.Features.Explorer.Messages.ServiceBusMessage;
using SubQueue = Azure.Messaging.ServiceBus.SubQueue;

namespace Messentra.Infrastructure.AzureServiceBus.Queues;

public interface IAzureServiceBusQueueMessagesProvider
{
    Task<IReadOnlyCollection<ServiceBusMessage>> Get(
        ConnectionInfo info,
        string queueName,
        FetchMessagesOptions options,
        CancellationToken cancellationToken);
}

public sealed class AzureServiceBusQueueMessagesProvider : AzureServiceBusProviderBase, IAzureServiceBusQueueMessagesProvider 
{
    public AzureServiceBusQueueMessagesProvider(IAzureServiceBusClientFactory clientFactory) : base(clientFactory)
    {
    }

    public async Task<IReadOnlyCollection<ServiceBusMessage>> Get(
        ConnectionInfo info,
        string queueName,
        FetchMessagesOptions options,
        CancellationToken cancellationToken)
    {
        return await ExecuteWithClientRecovery(info, async client =>
        {
            var receiver = client.CreateReceiver(queueName, new ServiceBusReceiverOptions
            {
                ReceiveMode = GetReceiveMode(options),
                SubQueue = options.SubQueue == Features.Explorer.Messages.SubQueue.DeadLetter
                    ? SubQueue.DeadLetter
                    : SubQueue.None,
                PrefetchCount = 0
            });
            var messages = new List<ServiceBusReceivedMessage>();
            var nextPeekSequence = options.StartSequence;

            try
            {
                while (messages.Count < options.MessageCount)
                {
                    var remaining = options.MessageCount - messages.Count;

                    var batch = options.Mode == FetchMode.Peek
                        ? await receiver.PeekMessagesAsync(Math.Min(remaining, 1000), nextPeekSequence, cancellationToken)
                        : await receiver.ReceiveMessagesAsync(Math.Min(remaining, 1000), options.WaitTime, cancellationToken);

                    if (batch.Count == 0)
                        break;

                    messages.AddRange(batch);

                    if (options.Mode == FetchMode.Peek)
                    {
                        nextPeekSequence = batch[^1].SequenceNumber + 1;
                    }
                }
                
                if (options is { Mode: FetchMode.Peek } or
                    { Mode: FetchMode.Receive, ReceiveMode: FetchReceiveMode.ReceiveAndDelete })
                    await receiver.DisposeAsync();
                
                return messages.Select(x => Map(options, receiver, x)).ToList();
            }
            catch
            {
                await receiver.DisposeAsync();
                throw;
            }
            
        }, cancellationToken);
    }
}