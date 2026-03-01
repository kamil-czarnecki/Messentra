namespace Messentra.Infrastructure.AzureServiceBus.Subscriptions;

public interface IAzureServiceBusSubscriptionProvider
{
    Task<IReadOnlyCollection<Resource.Subscription>> GetAll(ConnectionInfo info, string topicName, CancellationToken cancellationToken);
    Task<Resource.Subscription> GetByName(ConnectionInfo info, string topicName, string name, CancellationToken cancellationToken);
}

