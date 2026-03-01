using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.AzureServiceBus;

namespace Messentra.Features.Explorer.Resources.Queues.GetAllQueueResources;

public sealed record GetAllQueueResourcesQuery(ConnectionConfig ConnectionConfig) : IQuery<IReadOnlyCollection<Resource.Queue>>;

