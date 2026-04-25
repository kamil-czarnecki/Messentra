namespace Messentra.Features.Explorer.MessageGrid;

public static class DefaultColumns
{
    public static readonly ColumnView DefaultView = new(
        Id: "default",
        Name: "Default",
        IsBuiltIn: true,
        Columns:
        [
            new ColumnConfig("seq", "SEQ #", ColumnSource.BrokerProperty, "SequenceNumber", IsRemovable: false, Order: 0),
            new ColumnConfig("msgid", "MESSAGE ID", ColumnSource.BrokerProperty, "MessageId", IsRemovable: true, Order: 1),
            new ColumnConfig("label", "LABEL", ColumnSource.BrokerProperty, "Label", IsRemovable: true, Order: 2),
            new ColumnConfig("enqueued", "ENQUEUED TIME", ColumnSource.BrokerProperty, "EnqueuedTimeUtc", IsRemovable: true,
                Order: 3),
            new ColumnConfig("delivery", "DELIVERY COUNT", ColumnSource.BrokerProperty, "DeliveryCount", IsRemovable: true,
                Order: 4)
        ]);

    public static readonly IReadOnlyList<ColumnConfig> DlqColumns =
    [
        new("dlq-reason", "REASON", ColumnSource.BrokerProperty, "DeadLetterReason", IsRemovable: false, Order: 100),
        new("dlq-desc", "ERROR DESCRIPTION", ColumnSource.BrokerProperty, "DeadLetterErrorDescription",
            IsRemovable: false, Order: 101)
    ];
}