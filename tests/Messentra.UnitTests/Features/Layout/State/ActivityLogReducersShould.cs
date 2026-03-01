using AutoFixture;
using Messentra.Features.Layout.State;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Layout.State;

public sealed class ActivityLogReducersShould
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void AddLogEntryToEmptyState()
    {
        // Arrange
        var initialState = new ActivityLogState([]);
        var logEntry = _fixture.Create<ActivityLogEntry>();
        var action = new LogActivityAction(logEntry);

        // Act
        var newState = ActivityLogReducers.ReduceIncrementCounterAction(initialState, action);

        // Assert
        newState.Logs.Count().ShouldBe(1);
        newState.Logs.First().ShouldBe(logEntry);
    }

    [Fact]
    public void AddLogEntryToExistingLogs()
    {
        // Arrange
        var existingLogs = _fixture.CreateMany<ActivityLogEntry>(3).ToList();
        var initialState = new ActivityLogState(existingLogs);
        var newLogEntry = _fixture.Create<ActivityLogEntry>();
        var action = new LogActivityAction(newLogEntry);

        // Act
        var newState = ActivityLogReducers.ReduceIncrementCounterAction(initialState, action);

        // Assert
        newState.Logs.Count().ShouldBe(4);
        newState.Logs.Last().ShouldBe(newLogEntry);
        newState.Logs.Take(3).ShouldBe(existingLogs);
    }

    [Fact]
    public void PreserveExistingLogsWhenAddingNew()
    {
        // Arrange
        var log1 = new ActivityLogEntry("Connection 1", "Info", "Message 1", DateTime.Now);
        var log2 = new ActivityLogEntry("Connection 2", "Error", "Message 2", DateTime.Now);
        var initialState = new ActivityLogState([log1]);
        var action = new LogActivityAction(log2);

        // Act
        var newState = ActivityLogReducers.ReduceIncrementCounterAction(initialState, action);

        // Assert
        newState.Logs.Count().ShouldBe(2);
        newState.Logs.First().ShouldBe(log1);
        newState.Logs.Last().ShouldBe(log2);
    }

    [Fact]
    public void AddMultipleLogEntriesSequentially()
    {
        // Arrange
        var state = new ActivityLogState([]);
        var log1 = _fixture.Create<ActivityLogEntry>();
        var log2 = _fixture.Create<ActivityLogEntry>();
        var log3 = _fixture.Create<ActivityLogEntry>();

        // Act
        var state1 = ActivityLogReducers.ReduceIncrementCounterAction(state, new LogActivityAction(log1));
        var state2 = ActivityLogReducers.ReduceIncrementCounterAction(state1, new LogActivityAction(log2));
        var state3 = ActivityLogReducers.ReduceIncrementCounterAction(state2, new LogActivityAction(log3));

        // Assert
        state3.Logs.Count().ShouldBe(3);
        state3.Logs.ElementAt(0).ShouldBe(log1);
        state3.Logs.ElementAt(1).ShouldBe(log2);
        state3.Logs.ElementAt(2).ShouldBe(log3);
    }

    [Fact]
    public void MaintainImmutabilityOfOriginalState()
    {
        // Arrange
        var originalLogs = _fixture.CreateMany<ActivityLogEntry>(2).ToList();
        var initialState = new ActivityLogState(originalLogs);
        var newLogEntry = _fixture.Create<ActivityLogEntry>();
        var action = new LogActivityAction(newLogEntry);

        // Act
        var newState = ActivityLogReducers.ReduceIncrementCounterAction(initialState, action);

        // Assert
        initialState.Logs.Count().ShouldBe(2);
        newState.Logs.Count().ShouldBe(3);
        initialState.Logs.ShouldBe(originalLogs);
    }

    [Fact]
    public void ClearLogsFromPopulatedState()
    {
        // Arrange
        var logs = _fixture.CreateMany<ActivityLogEntry>(3).ToList();
        var initialState = new ActivityLogState(logs);
        var action = new ClearActivityLogAction();

        // Act
        var newState = ActivityLogReducers.ReduceClearActivityLogAction(initialState, action);

        // Assert
        newState.Logs.ShouldBeEmpty();
    }

    [Fact]
    public void ClearLogsFromEmptyState()
    {
        // Arrange
        var initialState = new ActivityLogState([]);
        var action = new ClearActivityLogAction();

        // Act
        var newState = ActivityLogReducers.ReduceClearActivityLogAction(initialState, action);

        // Assert
        newState.Logs.ShouldBeEmpty();
    }
}

