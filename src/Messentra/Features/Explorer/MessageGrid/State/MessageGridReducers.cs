using Fluxor;

namespace Messentra.Features.Explorer.MessageGrid.State;

public static class MessageGridReducers
{
    [ReducerMethod]
    public static MessageGridState OnSwitchView(MessageGridState state, SwitchMessageGridViewAction action)
    {
        var view = state.Views.FirstOrDefault(v => v.Id == action.ViewId);
        if (view is null) return state;
        return state with { ActiveViewId = view.Id, Columns = view.Columns };
    }

    [ReducerMethod]
    public static MessageGridState OnAddColumn(MessageGridState state, AddMessageGridColumnAction action)
    {
        var newColumns = state.Columns
            .Append(action.Column with { Order = state.Columns.Count })
            .ToList();
        return state with { Columns = newColumns };
    }

    [ReducerMethod]
    public static MessageGridState OnRemoveColumn(MessageGridState state, RemoveMessageGridColumnAction action)
    {
        var newColumns = state.Columns
            .Where(c => c.Id != action.ColumnId)
            .Select((c, i) => c with { Order = i })
            .ToList();
        return state with { Columns = newColumns };
    }

    [ReducerMethod]
    public static MessageGridState OnReorderColumns(MessageGridState state, ReorderMessageGridColumnsAction action)
        => state with { Columns = action.Columns };

    [ReducerMethod]
    public static MessageGridState OnSaveCurrentView(MessageGridState state, SaveCurrentMessageGridViewAction _)
    {
        var updated = state.Views
            .Select(v => v.Id == state.ActiveViewId ? v with { Columns = state.Columns } : v)
            .ToList();
        return state with { Views = updated };
    }

    [ReducerMethod]
    public static MessageGridState OnSaveViewAs(MessageGridState state, SaveMessageGridViewAsAction action)
    {
        var newView = new ColumnView(
            Id: Guid.NewGuid().ToString(),
            Name: action.Name,
            IsBuiltIn: false,
            Columns: state.Columns);
        var newViews = state.Views.Append(newView).ToList();
        return state with { Views = newViews, ActiveViewId = newView.Id };
    }

    [ReducerMethod]
    public static MessageGridState OnDeleteView(MessageGridState state, DeleteMessageGridViewAction action)
    {
        var newViews = state.Views.Where(v => v.Id != action.ViewId).ToList();
        if (newViews.Count == 0) return state;

        if (state.ActiveViewId != action.ViewId)
            return state with { Views = newViews };

        var fallback = newViews[0];
        return state with { Views = newViews, ActiveViewId = fallback.Id, Columns = fallback.Columns };
    }
}
