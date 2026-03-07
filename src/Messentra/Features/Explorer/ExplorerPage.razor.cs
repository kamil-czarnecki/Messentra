using Fluxor;
using Messentra.Features.Explorer.Resources;
using Messentra.Features.Settings.Connections;

namespace Messentra.Features.Explorer;

public partial class ExplorerPage : IDisposable
{
    private readonly ResourceSelector _resourceSelector;
    private readonly IState<ConnectionState> _connectionState;

    public ExplorerPage(ResourceSelector resourceSelector, IState<ConnectionState> connectionState)
    {
        _resourceSelector = resourceSelector;
        _connectionState = connectionState;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _resourceSelector.TreeItems.SelectedValueChanged += OnStateChanged;
        _resourceSelector.SelectedResource.SelectedValueChanged += OnStateChanged;
    }

    private void OnStateChanged<T>(object? sender, T _) => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        _resourceSelector.TreeItems.SelectedValueChanged -= OnStateChanged;
        _resourceSelector.SelectedResource.SelectedValueChanged -= OnStateChanged;
    }
}