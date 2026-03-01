using Fluxor;

namespace Messentra.Features.Layout.State;

public static class ActivityLogReducers
{
	[ReducerMethod]
	public static ActivityLogState ReduceIncrementCounterAction(
		ActivityLogState state,
		LogActivityAction actions) =>
		new(Logs: [..state.Logs, actions.Log]);
	
	[ReducerMethod]
	public static ActivityLogState ReduceClearActivityLogAction(
		ActivityLogState state,
		ClearActivityLogAction actions) =>
		new(Logs: []);
}