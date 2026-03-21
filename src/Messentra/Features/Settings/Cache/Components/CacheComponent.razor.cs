using Mediator;

namespace Messentra.Features.Settings.Cache.Components;

public partial class CacheComponent
{
    private readonly IMediator _mediator;

    public CacheComponent(IMediator mediator)
    {
        _mediator = mediator;
    }

    private async Task ClearCache()
    {
        await _mediator.Send(new ClearCacheCommand());
    } 
}