using Messentra.Domain;
using Messentra.Features.Settings.Cache;

namespace Messentra.Features;

public static class Extensions
{
    extension(IServiceCollection services)
    {
        public void AddServices()
        {
            services.AddSingleton<ICacheClearConfirmationService, CacheClearConfirmationService>();
            services.AddSingleton<IApplicationLifecycleService, ApplicationLifecycleService>();
        }
    }
    
    extension(ConnectionConfig config)
    {
        public Infrastructure.AzureServiceBus.ConnectionInfo ToConnectionInfo() =>
            config.ConnectionType switch
            {
                ConnectionType.ConnectionString => new Infrastructure.AzureServiceBus.ConnectionInfo.ConnectionString(
                    config.ConnectionStringConfig!.ConnectionString),

                ConnectionType.EntraId => new Infrastructure.AzureServiceBus.ConnectionInfo.ManagedIdentity(
                    FullyQualifiedNamespace: config.EntraIdConfig!.Namespace,
                    TenantId: config.EntraIdConfig.TenantId,
                    ClientId: config.EntraIdConfig.ClientId),

                _ => throw new InvalidOperationException($"Unsupported connection type: {config.ConnectionType}")
            };
    }
}