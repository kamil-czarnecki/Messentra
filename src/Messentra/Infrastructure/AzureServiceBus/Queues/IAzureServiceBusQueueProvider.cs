namespace Messentra.Infrastructure.AzureServiceBus.Queues;

public interface IAzureServiceBusQueueProvider
{
    Task<IReadOnlyCollection<Resource.Queue>> GetAll(ConnectionInfo info, CancellationToken cancellationToken);
    Task<Resource.Queue> GetByName(ConnectionInfo info, string name, CancellationToken cancellationToken);
}

