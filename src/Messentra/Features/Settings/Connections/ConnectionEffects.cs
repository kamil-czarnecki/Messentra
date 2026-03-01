using Fluxor;
using Mediator;
using Messentra.Features.Settings.Connections.CreateConnection;
using Messentra.Features.Settings.Connections.DeleteConnection;
using Messentra.Features.Settings.Connections.GetConnections;
using Messentra.Features.Settings.Connections.UpdateConnection;
using ConnectionConfigDto = Messentra.Features.Settings.Connections.CreateConnection.ConnectionConfigDto;

namespace Messentra.Features.Settings.Connections;

public sealed class ConnectionEffects
{
    private readonly IMediator _mediator;

    public ConnectionEffects(IMediator mediator)
    {
        _mediator = mediator;
    }

    [EffectMethod(typeof(FetchConnectionsAction))]
    public async Task HandleFetchConnectionsAction(IDispatcher dispatcher)
    {
        try
        {
            var connections = await _mediator.Send(new GetConnectionsQuery(), CancellationToken.None);
            
            dispatcher.Dispatch(new FetchConnectionsSuccessAction(connections));
        }
        catch
        {
            dispatcher.Dispatch(new FetchConnectionsFailureAction());
        }
    }
    
    [EffectMethod]
    public async Task HandleCreateConnectionAction(CreateConnectionAction action, IDispatcher dispatcher)
    {
        try
        {
            var command = new CreateConnectionCommand(
                Name: action.Connection.Name,
                ConnectionConfig: new ConnectionConfigDto(
                    ConnectionType: action.Connection.ConnectionConfig.ConnectionType,
                    ConnectionString: action.Connection.ConnectionConfig.ConnectionString,
                    Namespace: action.Connection.ConnectionConfig.Namespace,
                    TenantId: action.Connection.ConnectionConfig.TenantId,
                    ClientId: action.Connection.ConnectionConfig.ClientId));
            
            await _mediator.Send(command, CancellationToken.None);
            
            dispatcher.Dispatch(new CreateConnectionSuccessAction());
            dispatcher.Dispatch(new FetchConnectionsAction());
        }
        catch
        {
            dispatcher.Dispatch(new CreateConnectionFailureAction());
        }
    }
    
    [EffectMethod]
    public async Task HandleUpdateConnectionAction(UpdateConnectionAction action, IDispatcher dispatcher)
    {
        try
        {
            var command = new UpdateConnectionCommand(
                Id: action.Connection.Id,
                Name: action.Connection.Name,
                ConnectionConfig: new Connections.UpdateConnection.ConnectionConfigDto(
                    ConnectionType: action.Connection.ConnectionConfig.ConnectionType,
                    ConnectionString: action.Connection.ConnectionConfig.ConnectionString,
                    Namespace: action.Connection.ConnectionConfig.Namespace,
                    TenantId: action.Connection.ConnectionConfig.TenantId,
                    ClientId: action.Connection.ConnectionConfig.ClientId));
            
            await _mediator.Send(command, CancellationToken.None);
            
            dispatcher.Dispatch(new UpdateConnectionSuccessAction());
            dispatcher.Dispatch(new FetchConnectionsAction());
        }
        catch
        {
            dispatcher.Dispatch(new UpdateConnectionFailureAction());
        }
    }
    
    [EffectMethod]
    public async Task HandleDeleteConnectionAction(DeleteConnectionAction action, IDispatcher dispatcher)
    {
        try
        {
            var command = new DeleteConnectionCommand(action.Id);
            
            await _mediator.Send(command, CancellationToken.None);
            
            dispatcher.Dispatch(new DeleteConnectionSuccessAction());
            dispatcher.Dispatch(new FetchConnectionsAction());
        }
        catch
        {
            dispatcher.Dispatch(new DeleteConnectionFailureAction());
        }
    }
}