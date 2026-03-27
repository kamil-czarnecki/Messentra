using System.Text.Json.Serialization;

namespace Messentra.Features.Jobs;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(Queue), "queue")]
[JsonDerivedType(typeof(TopicSubscription), "topic-subscription")]
public abstract record ResourceTarget
{
    public sealed record Queue(string QueueName) : ResourceTarget;
    public sealed record TopicSubscription(string TopicName, string SubscriptionName) : ResourceTarget;
}