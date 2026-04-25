namespace Messentra.Features.Explorer.MessageGrid.State;

public sealed record MessageGridState(
    IReadOnlyList<ColumnView> Views,
    string ActiveViewId,
    IReadOnlyList<ColumnConfig> Columns)
{
    private MessageGridState() : this(
        Views: [DefaultColumns.DefaultView],
        ActiveViewId: DefaultColumns.DefaultView.Id,
        Columns: DefaultColumns.DefaultView.Columns)
    { }
}
