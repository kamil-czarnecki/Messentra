using Mediator;
using Messentra.Domain;

namespace Messentra.Features.Explorer.Resources.Subscriptions.GetSubscriptionResource;

public sealed record GetSubscriptionResourceQuery(string TopicName, string SubscriptionName, ConnectionConfig ConnectionConfig) : IQuery<GetSubscriptionResult>;

