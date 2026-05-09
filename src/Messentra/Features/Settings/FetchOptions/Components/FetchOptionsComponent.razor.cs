using Mediator;
using Messentra.Features.Settings.FetchOptions.SaveFetchOptions;
using Messentra.Features.Settings.UserSettings.GetUserSettings;

namespace Messentra.Features.Settings.FetchOptions.Components;

public sealed partial class FetchOptionsComponent
{
    private readonly IMediator _mediator;

    private int _defaultMessageCount;

    public FetchOptionsComponent(IMediator mediator)
    {
        _mediator = mediator;
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        var settings = await _mediator.Send(new GetUserSettingsQuery());
        _defaultMessageCount = settings.DefaultMessageCount;
    }

    private async Task OnValueChanged(int value)
    {
        _defaultMessageCount = value;
        await _mediator.Send(new SaveFetchOptionsCommand(_defaultMessageCount));
    }
}
