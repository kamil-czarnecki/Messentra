using Messentra.Features.Explorer.MessageGrid;
using Messentra.Features.Explorer.MessageGrid.State;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Explorer.MessageGrid;

public sealed class MessageGridReducersShould
{
    private static MessageGridState InitialState()
    {
        var defaultView = DefaultColumns.DefaultView;
        return new MessageGridState(
            Views: [defaultView],
            ActiveViewId: defaultView.Id,
            Columns: defaultView.Columns);
    }

    [Fact]
    public void SwitchToExistingView()
    {
        // Arrange
        var customView = new ColumnView("v1", "Custom", false, [
            new("seq", "SEQ #", ColumnSource.BrokerProperty, "SequenceNumber", false, 0)
        ]);
        var state = InitialState() with { Views = [DefaultColumns.DefaultView, customView] };

        // Act
        var result = MessageGridReducers.OnSwitchView(state, new SwitchMessageGridViewAction("v1"));

        // Assert
        result.ActiveViewId.ShouldBe("v1");
        result.Columns.ShouldBe(customView.Columns);
    }

    [Fact]
    public void IgnoreSwitchToNonExistentView()
    {
        // Arrange
        var state = InitialState();

        // Act
        var result = MessageGridReducers.OnSwitchView(state, new SwitchMessageGridViewAction("nonexistent"));

        // Assert
        result.ShouldBe(state);
    }

    [Fact]
    public void AddColumn()
    {
        // Arrange
        var state = InitialState();
        var newCol = new ColumnConfig("corr", "CORRELATION ID", ColumnSource.BrokerProperty, "CorrelationId", true, 99);

        // Act
        var result = MessageGridReducers.OnAddColumn(state, new AddMessageGridColumnAction(newCol));

        // Assert
        result.Columns.Count.ShouldBe(state.Columns.Count + 1);
        result.Columns.Last().Id.ShouldBe("corr");
        result.Columns.Last().Order.ShouldBe(state.Columns.Count);
    }

    [Fact]
    public void RemoveRemovableColumn()
    {
        // Arrange
        var state = InitialState();
        var initialCount = state.Columns.Count;

        // Act
        var result = MessageGridReducers.OnRemoveColumn(state, new RemoveMessageGridColumnAction("msgid"));

        // Assert
        result.Columns.Count.ShouldBe(initialCount - 1);
        result.Columns.ShouldAllBe(c => c.Id != "msgid");
        result.Columns.Select(c => c.Order).ShouldBe(Enumerable.Range(0, initialCount - 1));
    }

    [Fact]
    public void ReorderColumns()
    {
        // Arrange
        var state = InitialState();
        var reversed = state.Columns.Reverse().ToList();

        // Act
        var result = MessageGridReducers.OnReorderColumns(state, new ReorderMessageGridColumnsAction(reversed));

        // Assert
        result.Columns.ShouldBe(reversed);
    }

    [Fact]
    public void SaveCurrentViewOverwritesActiveView()
    {
        // Arrange
        var customCol = new ColumnConfig("corr", "CORR", ColumnSource.BrokerProperty, "CorrelationId", true, 99);
        var state = InitialState() with
        {
            Columns = [..DefaultColumns.DefaultView.Columns, customCol]
        };

        // Act
        var result = MessageGridReducers.OnSaveCurrentView(state, new SaveCurrentMessageGridViewAction());

        // Assert
        var savedView = result.Views.First(v => v.Id == state.ActiveViewId);
        savedView.Columns.ShouldContain(c => c.Id == "corr");
    }

    [Fact]
    public void SaveViewAsCreatesNewViewAndMakesItActive()
    {
        // Arrange
        var state = InitialState();

        // Act
        var result = MessageGridReducers.OnSaveViewAs(state, new SaveMessageGridViewAsAction("My View"));

        // Assert
        result.Views.Count.ShouldBe(2);
        var newView = result.Views.Last();
        newView.Name.ShouldBe("My View");
        newView.IsBuiltIn.ShouldBeFalse();
        result.ActiveViewId.ShouldBe(newView.Id);
    }

    [Fact]
    public void DeleteViewAndFallBackToFirstView()
    {
        // Arrange
        var custom = new ColumnView("v1", "Custom", false, DefaultColumns.DefaultView.Columns);
        var state = InitialState() with
        {
            Views = [DefaultColumns.DefaultView, custom],
            ActiveViewId = "v1"
        };

        // Act
        var result = MessageGridReducers.OnDeleteView(state, new DeleteMessageGridViewAction("v1"));

        // Assert
        result.Views.Count.ShouldBe(1);
        result.ActiveViewId.ShouldBe(DefaultColumns.DefaultView.Id);
        result.Columns.ShouldBe(DefaultColumns.DefaultView.Columns);
    }
}
