using System.Collections.Concurrent;
using Messentra.Infrastructure.AzureServiceBus.Factories;
using Messentra.Infrastructure.AzureServiceBus.Subscriptions;

namespace Messentra.Infrastructure.AzureServiceBus.Topics;

public sealed class AzureServiceBusResourceTopicProvider(
    IAzureServiceBusAdminClientFactory clientFactory,
    IAzureServiceBusSubscriptionProvider subscriptionProvider)
    : AzureServiceBusResourceProviderBase(clientFactory), IAzureServiceBusTopicProvider
{
    public async Task<IReadOnlyCollection<Resource.Topic>> GetAll(ConnectionInfo info, CancellationToken cancellationToken)
    {
        var client = await GetClient(info);
        var @namespace = GetNamespace(info);
        var topicsProperties = new ConcurrentDictionary<string, Azure.Messaging.ServiceBus.Administration.TopicProperties>();
        var topicsRuntimeProperties = new ConcurrentDictionary<string, Azure.Messaging.ServiceBus.Administration.TopicRuntimeProperties>();
        var subscriptions = new ConcurrentDictionary<string, IReadOnlyCollection<Resource.Subscription>>();
        
        await Task.WhenAll(LoadTopicProperties(), LoadTopicRuntimeProperties());
        
        await LoadSubscriptions();

        return topicsProperties
            .Select(x =>
            {
                var topic = x.Value;
                var runtimeProps = topicsRuntimeProperties[topic.Name];
                var subs = subscriptions[topic.Name];

                return MapToTopic(topic, runtimeProps, subs, @namespace);
            })
            .ToList();
        
        async Task LoadTopicProperties()
        {
            await foreach (var topic in client.GetTopicsAsync(cancellationToken))
            {
                topicsProperties[topic.Name] = topic;
            }
        }

        async Task LoadTopicRuntimeProperties()
        {
            await foreach (var topic in client.GetTopicsRuntimePropertiesAsync(cancellationToken))
            {
                topicsRuntimeProperties[topic.Name] = topic;
            }
        }
        
        async Task LoadSubscriptions()
        {
            var semaphore = new SemaphoreSlim(10, 10);
            
            try
            {
                await Task.WhenAll(topicsProperties.Keys.Select(async topicName =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        var subs = await subscriptionProvider.GetAll(info, topicName, cancellationToken);
                        subscriptions[topicName] = subs;
                    }
                    finally
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        semaphore.Release();
                    }
                }));
            }
            finally
            {
                semaphore.Dispose();
            }
        }
    }

    public async Task<Resource.Topic> GetByName(ConnectionInfo info, string name, CancellationToken cancellationToken)
    {
        var client = await GetClient(info);
        var @namespace = GetNamespace(info);

        var topicTask = client.GetTopicAsync(name, cancellationToken);
        var runtimeTask = client.GetTopicRuntimePropertiesAsync(name, cancellationToken);
        var subscriptionsTask = subscriptionProvider.GetAll(info, name, cancellationToken);

        await Task.WhenAll(topicTask, runtimeTask, subscriptionsTask);

        return MapToTopic(topicTask.Result.Value, runtimeTask.Result.Value, subscriptionsTask.Result, @namespace);
    }

    private static Resource.Topic MapToTopic(
        Azure.Messaging.ServiceBus.Administration.TopicProperties topic,
        Azure.Messaging.ServiceBus.Administration.TopicRuntimeProperties runtimeProperties,
        IReadOnlyCollection<Resource.Subscription> subscriptions,
        string @namespace) =>
        new(
            Name: topic.Name,
            Url: $"https://{@namespace}/{topic.Name}",
            Overview: new ResourceOverview(
                Status: topic.Status.ToString(),
                CreatedAt: runtimeProperties.CreatedAt,
                UpdatedAt: runtimeProperties.UpdatedAt,
                MessageInfo: new MessageInfo(
                    Active: 0,
                    DeadLetter: 0,
                    Scheduled: runtimeProperties.ScheduledMessageCount,
                    Transfer: 0,
                    TransferDeadLetter: 0,
                    Total: 0),
                SizeInfo: new SizeInfo(
                    CurrentSizeInBytes: runtimeProperties.SizeInBytes,
                    MaxSizeInMegabytes: topic.MaxSizeInMegabytes)),
            Properties: new TopicProperties(
                DefaultMessageTimeToLive: topic.DefaultMessageTimeToLive,
                AutoDeleteOnIdle: topic.AutoDeleteOnIdle,
                EnablePartitioning: topic.EnablePartitioning,
                RequiresDuplicateDetection: topic.RequiresDuplicateDetection,
                DuplicateDetectionWindow: topic.DuplicateDetectionHistoryTimeWindow,
                MaxMessageSizeInKilobytes: topic.MaxMessageSizeInKilobytes,
                UserMetadata: topic.UserMetadata),
            Subscriptions: subscriptions);
}

