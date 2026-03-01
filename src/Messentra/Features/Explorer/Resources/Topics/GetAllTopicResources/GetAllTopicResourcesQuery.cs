using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.AzureServiceBus;

namespace Messentra.Features.Explorer.Resources.Topics.GetAllTopicResources;

public sealed record GetAllTopicResourcesQuery(ConnectionConfig ConnectionConfig) : IQuery<IReadOnlyCollection<Resource.Topic>>;

