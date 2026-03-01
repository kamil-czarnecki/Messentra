using Messentra.Infrastructure.AzureServiceBus;
using OneOf;

namespace Messentra.Features.Explorer.Resources.Queues.GetQueueResource;

[GenerateOneOf]
public partial class GetQueueResult : OneOfBase<Resource.Queue, QueueNotFound>;