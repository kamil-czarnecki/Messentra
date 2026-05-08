using Microsoft.AspNetCore.Components;

namespace Messentra.Features.Explorer.Resources.Components;

public partial class StickyNamespaceHeader
{
    [Parameter]
    public string? Namespace { get; set; }

    [Parameter]
    public EventCallback<string> OnClick { get; set; }

    private async Task HandleClick()
    {
        if (Namespace is not null)
            await OnClick.InvokeAsync(Namespace);
    }
}
