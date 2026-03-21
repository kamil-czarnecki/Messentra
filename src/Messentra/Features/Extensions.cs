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
}