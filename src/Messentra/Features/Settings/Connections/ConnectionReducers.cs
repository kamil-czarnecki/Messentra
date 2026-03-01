using Fluxor;

namespace Messentra.Features.Settings.Connections;

public static class ConnectionReducers
{
    [ReducerMethod]
    public static ConnectionState Reduce(ConnectionState state, FetchConnectionsAction _) =>
        state with { IsLoading = true, IsLoaded = false };
    [ReducerMethod]
    public static ConnectionState Reduce(ConnectionState _, FetchConnectionsSuccessAction action) =>
        new(IsLoading: false, IsLoaded: true, Connections: action.Connections);
    [ReducerMethod]
    public static ConnectionState Reduce(ConnectionState state, FetchConnectionsFailureAction _) =>
        state with { IsLoading = false, IsLoaded = false };
    
    [ReducerMethod]
    public static ConnectionState Reduce(ConnectionState state, CreateConnectionAction _) =>
        state with { IsLoading = true, IsLoaded = false };
    [ReducerMethod]
    public static ConnectionState Reduce(ConnectionState state, CreateConnectionSuccessAction _) =>
        state with { IsLoading = false, IsLoaded = false };
    [ReducerMethod]
    public static ConnectionState Reduce(ConnectionState state, CreateConnectionFailureAction _) =>
        state with { IsLoading = false, IsLoaded = false };
    
    [ReducerMethod]
    public static ConnectionState Reduce(ConnectionState state, UpdateConnectionAction _) =>
        state with { IsLoading = true, IsLoaded = false };
    [ReducerMethod]
    public static ConnectionState Reduce(ConnectionState state, UpdateConnectionSuccessAction _) =>
        state with { IsLoading = false, IsLoaded = false };
    [ReducerMethod]
    public static ConnectionState Reduce(ConnectionState state, UpdateConnectionFailureAction _) =>
        state with { IsLoading = false, IsLoaded = false };
    
    [ReducerMethod]
    public static ConnectionState Reduce(ConnectionState state, DeleteConnectionAction _) =>
        state with { IsLoading = true, IsLoaded = false };
    [ReducerMethod]
    public static ConnectionState Reduce(ConnectionState state, DeleteConnectionSuccessAction _) =>
        state with { IsLoading = false, IsLoaded = false };
    [ReducerMethod]
    public static ConnectionState Reduce(ConnectionState state, DeleteConnectionFailureAction _) =>
        state with { IsLoading = false, IsLoaded = false };
}