namespace Messentra.Features.Explorer.MessageGrid.State;

public sealed record SwitchMessageGridViewAction(string ViewId);
public sealed record AddMessageGridColumnAction(ColumnConfig Column);
public sealed record RemoveMessageGridColumnAction(string ColumnId);
public sealed record ReorderMessageGridColumnsAction(IReadOnlyList<ColumnConfig> Columns);
public sealed record SaveCurrentMessageGridViewAction();
public sealed record SaveMessageGridViewAsAction(string Name);
public sealed record DeleteMessageGridViewAction(string ViewId);
public sealed record MessageGridViewsSavedAction();
