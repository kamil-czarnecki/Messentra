using Messentra.Infrastructure.AutoUpdater;
using Messentra.Infrastructure.AzureServiceBus;
using Messentra.Infrastructure.AzureServiceBus.Factories;
using Messentra.Infrastructure.AzureServiceBus.Queues;
using Messentra.Infrastructure.AzureServiceBus.Subscriptions;
using Messentra.Infrastructure.AzureServiceBus.Topics;

namespace Messentra.Infrastructure;

public static class Extensions
{
    extension(IServiceCollection services)
    {
        public void AddInfrastructure()
        {
            services.AddSingleton<IAutoUpdaterService, AutoUpdaterService>();
            services.AddSingleton<IAzureServiceBusTokenCredentialFactory, AzureServiceBusTokenCredentialFactory>();
            services.AddSingleton<IAzureServiceBusClientFactory, AzureServiceBusClientFactory>();
            services.AddSingleton<IAzureServiceBusAdminClientFactory, AzureServiceBusAdminClientFactory>();
            services.AddSingleton<IAzureServiceBusQueueProvider, AzureServiceBusResourceQueueProvider>();
            services.AddSingleton<IAzureServiceBusSubscriptionProvider, AzureServiceBusResourceSubscriptionProvider>();
            services.AddSingleton<IAzureServiceBusTopicProvider, AzureServiceBusResourceTopicProvider>();
            services.AddSingleton<IAzureServiceBusQueueMessagesProvider, AzureServiceBusQueueMessagesProvider>();
            services.AddSingleton<IAzureServiceBusSubscriptionMessagesProvider, AzureServiceBusSubscriptionMessagesProvider>();
            services.AddSingleton<IAzureServiceBusSender, AzureServiceBusSender>();
        }
    }
}