using Messentra.Infrastructure.AzureServiceBus.Factories;

namespace Messentra.Infrastructure.AzureServiceBus.Queues;

public sealed class AzureServiceBusResourceQueueProvider(IAzureServiceBusAdminClientFactory clientFactory)
    : AzureServiceBusResourceProviderBase(clientFactory), IAzureServiceBusQueueProvider
{
    public async Task<IReadOnlyCollection<Resource.Queue>> GetAll(ConnectionInfo info, CancellationToken cancellationToken)
    {
        return await ExecuteWithClientRecovery(info, async client =>
        {
            var @namespace = GetNamespace(info);
            var queuesProperties = new Dictionary<string, Azure.Messaging.ServiceBus.Administration.QueueProperties>();
            var queuesRuntimeProperties = new Dictionary<string, Azure.Messaging.ServiceBus.Administration.QueueRuntimeProperties>();


            var queuesPropertiesTask = Task.Run(
                async () =>
                {
                    await foreach (var queue in client.GetQueuesAsync(cancellationToken))
                        queuesProperties.Add(queue.Name, queue);
                },
                cancellationToken);
            var queuesRuntimePropertiesTask = Task.Run(
                async () =>
                {
                    await foreach (var queue in client.GetQueuesRuntimePropertiesAsync(cancellationToken))
                        queuesRuntimeProperties.Add(queue.Name, queue);
                },
                cancellationToken);

            await Task.WhenAll(queuesPropertiesTask, queuesRuntimePropertiesTask);

            return queuesProperties
                .Select(x =>
            {
                var queue = x.Value;
                var runtimeProperties = queuesRuntimeProperties[x.Key];

                return MapToQueue(queue, runtimeProperties, @namespace);
            })
            .ToList();
        }, cancellationToken);
    }

    public async Task<Resource.Queue> GetByName(ConnectionInfo info, string name, CancellationToken cancellationToken)
    {
        return await ExecuteWithClientRecovery(info, async client =>
        {
            var @namespace = GetNamespace(info);

            var queue = await client.GetQueueAsync(name, cancellationToken);
            var runtimeProperties = await client.GetQueueRuntimePropertiesAsync(name, cancellationToken);

            return MapToQueue(queue.Value, runtimeProperties.Value, @namespace);
        }, cancellationToken);
    }

    private static Resource.Queue MapToQueue(
        Azure.Messaging.ServiceBus.Administration.QueueProperties queue,
        Azure.Messaging.ServiceBus.Administration.QueueRuntimeProperties runtimeProperties,
        string @namespace) =>
        new(
            Name: queue.Name,
            Url: $"https://{@namespace}/{queue.Name}",
            Overview: new ResourceOverview(
                Status: queue.Status.ToString(),
                CreatedAt: runtimeProperties.CreatedAt,
                UpdatedAt: runtimeProperties.UpdatedAt,
                MessageInfo: new MessageInfo(
                    Active: runtimeProperties.ActiveMessageCount,
                    DeadLetter: runtimeProperties.DeadLetterMessageCount,
                    Scheduled: runtimeProperties.ScheduledMessageCount,
                    Transfer: runtimeProperties.TransferMessageCount,
                    TransferDeadLetter: runtimeProperties.TransferDeadLetterMessageCount,
                    Total: runtimeProperties.TotalMessageCount),
                SizeInfo: new SizeInfo(
                    CurrentSizeInBytes: runtimeProperties.SizeInBytes,
                    MaxSizeInMegabytes: queue.MaxSizeInMegabytes)),
            Properties: new QueueProperties(
                DefaultMessageTimeToLive: queue.DefaultMessageTimeToLive,
                LockDuration: queue.LockDuration,
                AutoDeleteOnIdle: queue.AutoDeleteOnIdle,
                MaxDeliveryCount: queue.MaxDeliveryCount,
                DeadLetteringOnMessageExpiration: queue.DeadLetteringOnMessageExpiration,
                ForwardDeadLetteredMessagesTo: queue.ForwardDeadLetteredMessagesTo,
                ForwardTo: queue.ForwardTo,
                EnablePartitioning: queue.EnablePartitioning,
                RequiresDuplicateDetection: queue.RequiresDuplicateDetection,
                DuplicateDetectionWindow: queue.DuplicateDetectionHistoryTimeWindow,
                RequiresSession: queue.RequiresSession,
                MaxMessageSizeInKilobytes: queue.MaxMessageSizeInKilobytes,
                UserMetadata: queue.UserMetadata));
}

