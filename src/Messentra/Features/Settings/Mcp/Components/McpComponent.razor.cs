using Mediator;
using Messentra.Features.Settings.Mcp.SaveMcpSettings;
using Messentra.Features.Settings.UserSettings.GetUserSettings;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Messentra.Features.Settings.Mcp.Components;

public partial class McpComponent
{
    private readonly IMediator _mediator;
    private readonly NavigationManager _navigation;
    private readonly IJSRuntime _js;

    private bool _isMcpEnabled;
    private bool _saved;
    private bool _copied;

    public McpComponent(IMediator mediator, NavigationManager navigation, IJSRuntime js)
    {
        _mediator = mediator;
        _navigation = navigation;
        _js = js;
    }

    private string McpEndpoint => _navigation.ToAbsoluteUri("mcp").ToString();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        var settings = await _mediator.Send(new GetUserSettingsQuery());
        _isMcpEnabled = settings.IsMcpEnabled;
    }

    private async Task OnToggled(bool value)
    {
        _isMcpEnabled = value;
        _saved = false;
        await _mediator.Send(new SaveMcpSettingsCommand(_isMcpEnabled));
        _saved = true;
    }

    private async Task CopyEndpoint()
    {
        await _js.InvokeVoidAsync("navigator.clipboard.writeText", McpEndpoint);
        _copied = true;
        StateHasChanged();
        _ = ResetCopiedAsync();
    }

    private async Task ResetCopiedAsync()
    {
        await Task.Delay(2000);
        _copied = false;
        await InvokeAsync(StateHasChanged);
    }
}
