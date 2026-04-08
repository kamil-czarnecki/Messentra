using Microsoft.AspNetCore.Components;

namespace Messentra.Features.Explorer.Resources.Components;

public partial class ResourceTreeItemWrapper
{
    [Parameter, EditorRequired]
    public ResourceTreeItemData Presenter { get; init; } = null!;

    [Parameter, EditorRequired]
    public bool IsSelected { get; init; }

    [Parameter, EditorRequired]
    public bool IsExpanded { get; init; }

    [Parameter]
    public EventCallback<bool> ExpandedChanged { get; set; }

    [Parameter]
    public EventCallback<bool> SelectedChanged { get; set; }

    [Parameter]
    public RenderFragment? ItemContent { get; set; }

    private ResourceTreeItemData? _prevPresenter;
    private bool _prevIsSelected;
    private bool _prevIsExpanded;
    private bool _shouldRender = true;

    protected override void OnParametersSet()
    {
        _shouldRender = !ReferenceEquals(_prevPresenter, Presenter)
            || _prevIsSelected != IsSelected
            || _prevIsExpanded != IsExpanded;

        _prevPresenter = Presenter;
        _prevIsSelected = IsSelected;
        _prevIsExpanded = IsExpanded;
    }

    protected override bool ShouldRender() => _shouldRender;
}
