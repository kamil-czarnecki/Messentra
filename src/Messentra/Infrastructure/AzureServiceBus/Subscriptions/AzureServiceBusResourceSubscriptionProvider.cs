using Messentra.Infrastructure.AzureServiceBus.Factories;

namespace Messentra.Infrastructure.AzureServiceBus.Subscriptions;

public sealed class AzureServiceBusResourceSubscriptionProvider(IAzureServiceBusAdminClientFactory clientFactory)
    : AzureServiceBusResourceProviderBase(clientFactory), IAzureServiceBusSubscriptionProvider
{
    public async Task<IReadOnlyCollection<Resource.Subscription>> GetAll(
        ConnectionInfo info,
        string topicName,
        CancellationToken cancellationToken)
    {
        var client = await GetClient(info, cancellationToken);
        var @namespace = GetNamespace(info);
        var subscriptionsProperties = new Dictionary<string, Azure.Messaging.ServiceBus.Administration.SubscriptionProperties>();
        var subscriptionsRuntimeProperties = new Dictionary<string, Azure.Messaging.ServiceBus.Administration.SubscriptionRuntimeProperties>();
        var subscriptionsPropertiesTask = Task.Run(
            async () =>
            {
                await foreach (var sub in client.GetSubscriptionsAsync(topicName, cancellationToken))
                {
                    subscriptionsProperties.Add(sub.SubscriptionName, sub);
                }
            },
            cancellationToken);
        var subscriptionsRuntimePropertiesTask = Task.Run(
            async () =>
            {
                await foreach (var sub in client.GetSubscriptionsRuntimePropertiesAsync(topicName, cancellationToken))
                {
                    subscriptionsRuntimeProperties.Add(sub.SubscriptionName, sub);
                }
            },
            cancellationToken);
        
        await Task.WhenAll(subscriptionsPropertiesTask, subscriptionsRuntimePropertiesTask);

        return subscriptionsProperties.Select(x =>
            {
                var subscription = x.Value;
                var runtimeProps = subscriptionsRuntimeProperties[subscription.SubscriptionName];

                return MapToSubscription(subscription, runtimeProps, topicName, @namespace);
            })
            .ToList();
    }

    public async Task<Resource.Subscription> GetByName(
        ConnectionInfo info,
        string topicName,
        string name,
        CancellationToken cancellationToken)
    {
        var client = await GetClient(info, cancellationToken);
        var @namespace = GetNamespace(info);

        var sub = await client.GetSubscriptionAsync(topicName, name, cancellationToken);
        var runtimeProps = await client.GetSubscriptionRuntimePropertiesAsync(topicName, name, cancellationToken);

        return MapToSubscription(sub.Value, runtimeProps.Value, topicName, @namespace);
    }

    private static Resource.Subscription MapToSubscription(
        Azure.Messaging.ServiceBus.Administration.SubscriptionProperties sub,
        Azure.Messaging.ServiceBus.Administration.SubscriptionRuntimeProperties runtimeProps,
        string topicName,
        string @namespace) =>
        new(
            Name: sub.SubscriptionName,
            TopicName: topicName,
            Url: $"https://{@namespace}/{topicName}/subscriptions/{sub.SubscriptionName}",
            Overview: new ResourceOverview(
                Status: sub.Status.ToString(),
                CreatedAt: runtimeProps.CreatedAt,
                UpdatedAt: runtimeProps.UpdatedAt,
                MessageInfo: new MessageInfo(
                    Active: runtimeProps.ActiveMessageCount,
                    DeadLetter: runtimeProps.DeadLetterMessageCount,
                    Scheduled: 0,
                    Transfer: runtimeProps.TransferMessageCount,
                    TransferDeadLetter: runtimeProps.TransferDeadLetterMessageCount,
                    Total: runtimeProps.TotalMessageCount),
                SizeInfo: new SizeInfo(
                    CurrentSizeInBytes: 0,
                    MaxSizeInMegabytes: 0)),
            Properties: new SubscriptionProperties(
                DefaultMessageTimeToLive: sub.DefaultMessageTimeToLive,
                LockDuration: sub.LockDuration,
                AutoDeleteOnIdle: sub.AutoDeleteOnIdle,
                MaxDeliveryCount: sub.MaxDeliveryCount,
                DeadLetteringOnMessageExpiration: sub.DeadLetteringOnMessageExpiration,
                ForwardDeadLetteredMessagesTo: sub.ForwardDeadLetteredMessagesTo,
                ForwardTo: sub.ForwardTo,
                RequiresSession: sub.RequiresSession,
                UserMetadata: sub.UserMetadata));
}

