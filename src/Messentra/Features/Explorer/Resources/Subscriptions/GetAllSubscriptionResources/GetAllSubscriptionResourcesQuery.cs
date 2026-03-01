using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.AzureServiceBus;

namespace Messentra.Features.Explorer.Resources.Subscriptions.GetAllSubscriptionResources;

public sealed record GetAllSubscriptionResourcesQuery(string TopicName, ConnectionConfig ConnectionConfig) : IQuery<IReadOnlyCollection<Resource.Subscription>>;

