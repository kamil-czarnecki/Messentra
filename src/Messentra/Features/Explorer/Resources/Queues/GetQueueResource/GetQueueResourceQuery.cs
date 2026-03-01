using Mediator;
using Messentra.Domain;

namespace Messentra.Features.Explorer.Resources.Queues.GetQueueResource;

public sealed record GetQueueResourceQuery(string QueueName, ConnectionConfig ConnectionConfig) : IQuery<GetQueueResult>;
