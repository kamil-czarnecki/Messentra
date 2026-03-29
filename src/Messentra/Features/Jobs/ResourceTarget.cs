using System.Text.Json.Serialization;
using Messentra.Features.Explorer.Messages;

namespace Messentra.Features.Jobs;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(Queue), "queue")]
[JsonDerivedType(typeof(TopicSubscription), "topic-subscription")]
public abstract record ResourceTarget(SubQueue SubQueue)
{
    public sealed record Queue(string QueueName, SubQueue SubQueue) : ResourceTarget(SubQueue);

    public sealed record TopicSubscription(string TopicName, string SubscriptionName, SubQueue SubQueue)
        : ResourceTarget(SubQueue);
}