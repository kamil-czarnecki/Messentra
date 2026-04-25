namespace Messentra.Features.Explorer.MessageGrid.State;

public sealed record MessageGridState(
    IReadOnlyList<ColumnView> Views,
    string ActiveViewId,
    IReadOnlyList<ColumnConfig> Columns);
