using Fluxor;
using Messentra.Features.Explorer.Resources;
using Messentra.Features.Settings.Connections;

namespace Messentra.Features.Explorer;

public partial class ExplorerPage : IDisposable
{
    private readonly ResourceSelector _resourceSelector;
    private readonly IState<ConnectionState> _connectionState;
    private bool _treeReady;

    public ExplorerPage(ResourceSelector resourceSelector, IState<ConnectionState> connectionState)
    {
        _resourceSelector = resourceSelector;
        _connectionState = connectionState;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _resourceSelector.TreeItems.SelectedValueChanged += OnStateChanged;
            _resourceSelector.SelectedResource.SelectedValueChanged += OnStateChanged;
            _resourceSelector.SearchPhrase.SelectedValueChanged += OnStateChanged;
            _resourceSelector.ExpandedKeys.SelectedValueChanged += OnStateChanged;
            
            _treeReady = true;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void OnStateChanged<T>(object? sender, T _) => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        _resourceSelector.TreeItems.SelectedValueChanged -= OnStateChanged;
        _resourceSelector.SelectedResource.SelectedValueChanged -= OnStateChanged;
        _resourceSelector.SearchPhrase.SelectedValueChanged -= OnStateChanged;
        _resourceSelector.ExpandedKeys.SelectedValueChanged -= OnStateChanged;
    }
}