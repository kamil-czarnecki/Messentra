using Mediator;
using Messentra.Infrastructure.AzureServiceBus;

namespace Messentra.Features.Settings.Cache;

public sealed class ClearCacheCommandHandler : ICommandHandler<ClearCacheCommand>
{
    private readonly IAuthenticationRecordStore _authenticationRecordStore;
    private readonly ICacheClearConfirmationService _confirmationService;
    private readonly IApplicationLifecycleService _applicationLifecycleService;

    public ClearCacheCommandHandler(
        IAuthenticationRecordStore authenticationRecordStore,
        ICacheClearConfirmationService confirmationService,
        IApplicationLifecycleService applicationLifecycleService)
    {
        _authenticationRecordStore = authenticationRecordStore;
        _confirmationService = confirmationService;
        _applicationLifecycleService = applicationLifecycleService;
    }

    public async ValueTask<Unit> Handle(ClearCacheCommand command, CancellationToken cancellationToken)
    {
        var confirmed = await _confirmationService.ConfirmClearAsync(cancellationToken);
        if (!confirmed)
            return Unit.Value;
        
        _authenticationRecordStore.ClearAll();
        
        _applicationLifecycleService.Relaunch();
        _applicationLifecycleService.Exit();
        
        return Unit.Value;
    }
}