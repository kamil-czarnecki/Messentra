using Fluxor;
using Messentra.Features.Explorer.Resources;
using Messentra.Features.Settings.Connections;

namespace Messentra.Features.Explorer;

public partial class ExplorerPage
{
    private readonly IState<ResourceState> _resourceState;
    private readonly IState<ConnectionState> _connectionState;

    public ExplorerPage(IState<ResourceState> resourceState, IState<ConnectionState> connectionState)
    {
        _resourceState = resourceState;
        _connectionState = connectionState;
    }
}