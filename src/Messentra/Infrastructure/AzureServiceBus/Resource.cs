namespace Messentra.Infrastructure.AzureServiceBus;

public abstract record Resource(string Name, string Url, ResourceOverview Overview)
{
    public sealed record Queue(
        string Name,
        string Url,
        ResourceOverview Overview,
        QueueProperties Properties) : Resource(Name, Url, Overview);

    public sealed record Topic(
        string Name,
        string Url,
        ResourceOverview Overview,
        TopicProperties Properties,
        IReadOnlyCollection<Subscription> Subscriptions) : Resource(Name, Url, Overview);

    public sealed record Subscription(
        string Name,
        string TopicName,
        string Url,
        ResourceOverview Overview,
        SubscriptionProperties Properties) : Resource(Name, Url, Overview);
}

