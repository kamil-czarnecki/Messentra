namespace Messentra.Infrastructure.AzureServiceBus.Topics;

public interface IAzureServiceBusTopicProvider
{
    Task<IReadOnlyCollection<Resource.Topic>> GetAll(ConnectionInfo info, CancellationToken cancellationToken);
    Task<Resource.Topic> GetByName(ConnectionInfo info, string name, CancellationToken cancellationToken);
}

