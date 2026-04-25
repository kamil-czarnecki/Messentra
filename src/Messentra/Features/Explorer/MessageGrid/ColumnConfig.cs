namespace Messentra.Features.Explorer.MessageGrid;

public enum ColumnSource { BrokerProperty, AppProperty }

public sealed record ColumnConfig(
    string Id,
    string Title,
    ColumnSource Source,
    string PropertyKey,
    bool IsRemovable,
    int Order);

public sealed record ColumnView(
    string Id,
    string Name,
    bool IsBuiltIn,
    IReadOnlyList<ColumnConfig> Columns);