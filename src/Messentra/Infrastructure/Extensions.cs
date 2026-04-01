using System.Reflection;
using Messentra.Features.Jobs;
using Messentra.Features.Jobs.Stages;
using Messentra.Infrastructure.AutoUpdater;
using Messentra.Infrastructure.AzureServiceBus;
using Messentra.Infrastructure.AzureServiceBus.Factories;
using Messentra.Infrastructure.AzureServiceBus.Queues;
using Messentra.Infrastructure.AzureServiceBus.Subscriptions;
using Messentra.Infrastructure.AzureServiceBus.Topics;
using Messentra.Infrastructure.Jobs;

namespace Messentra.Infrastructure;

public static class Extensions
{
    extension(IServiceCollection services)
    {
        public void AddInfrastructure()
        {
            services.AddSingleton<IAutoUpdaterService, AutoUpdaterService>();
            services
                .AddOptions<AutoUpdatePollingOptions>()
                .BindConfiguration(AutoUpdatePollingOptions.SectionName);
            services.AddHostedService<AutoUpdateCheckerHostedService>();
            services.AddSingleton<IAzureServiceBusTokenCredentialFactory, AzureServiceBusTokenCredentialFactory>();
            services.AddSingleton<IAzureServiceBusClientFactory, AzureServiceBusClientFactory>();
            services.AddSingleton<IAzureServiceBusAdminClientFactory, AzureServiceBusAdminClientFactory>();
            services.AddSingleton<IAzureServiceBusQueueProvider, AzureServiceBusResourceQueueProvider>();
            services.AddSingleton<IAzureServiceBusSubscriptionProvider, AzureServiceBusResourceSubscriptionProvider>();
            services.AddSingleton<IAzureServiceBusTopicProvider, AzureServiceBusResourceTopicProvider>();
            services.AddSingleton<IAzureServiceBusQueueMessagesProvider, AzureServiceBusQueueMessagesProvider>();
            services.AddSingleton<IAzureServiceBusSubscriptionMessagesProvider, AzureServiceBusSubscriptionMessagesProvider>();
            services.AddSingleton<IAzureServiceBusSender, AzureServiceBusSender>();
            services.AddSingleton<IAuthenticationRecordStore, AuthenticationRecordStore>();
            services.AddSingleton<IInteractiveAuthBootstrapper, InteractiveAuthBootstrapper>();
            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddSingleton<IJobCancellationRegistry, JobCancellationRegistry>();
            services.AddSingleton<IJobProgressNotifier, JobProgressNotifier>();
            services.AddSingleton<IJobRunner, JobRunner>();
            services.AddSingleton<IBackgroundJobQueue, BackgroundJobQueue>();
            services.AddHostedService<JobWorker>();
            
            services.AddAllStages();
        }
        
        private void AddAllStages(Assembly? assembly = null)
        {
            assembly ??= Assembly.GetExecutingAssembly();

            var stageTypes = assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false })
                .SelectMany(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IStage<>) || i.GetGenericTypeDefinition() == typeof(IStage<,>)))
                    .Select(i => new { StageType = t, InterfaceType = i }))
                .ToList();

            foreach (var stage in stageTypes)
            {
                services.AddScoped(stage.StageType);
            }
        }
    }
}