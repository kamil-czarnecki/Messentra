using Messentra.Features.Explorer.Messages;
using Microsoft.AspNetCore.Components;

namespace Messentra.Features.Explorer.Resources.Components.Details.Tabs.Messages;

public partial class MessagesTab
{
    [Parameter, EditorRequired]
    public ResourceTreeNode ResourceTreeNode { get; init; } = null!;

    [Parameter]
    public SubQueue SubQueue { get; init; } = SubQueue.Active;
    
    [Parameter, EditorRequired]
    public EventCallback OnRefresh { get; set; }
}

