using Messentra.Features.Explorer.MessageGrid;
using Messentra.Features.Explorer.Messages;
using Moq;
using Shouldly;
using Xunit;
using MessageGridComponent = Messentra.Features.Explorer.Resources.Components.Details.Tabs.MessageGrid;

namespace Messentra.UnitTests.Features.Explorer.MessageGrid;

public sealed class MessageGridCellFormattingShould
{
    private static ServiceBusMessage BuildMessage(BrokerProperties brokerProps,
        IReadOnlyDictionary<string, object>? appProps = null)
    {
        var dto = new MessageDto(
            Body: string.Empty,
            BrokerProperties: brokerProps,
            ApplicationProperties: appProps ?? new Dictionary<string, object>());
        return new ServiceBusMessage(dto, Mock.Of<IServiceBusMessageContext>());
    }

    private static BrokerProperties DefaultBroker(
        long sequenceNumber = 1,
        string? messageId = "msg-1",
        string? label = "test-label",
        int deliveryCount = 0,
        string? correlationId = null,
        string? contentType = "application/json",
        string? deadLetterReason = null,
        string? deadLetterErrorDescription = null) =>
        new(
            MessageId: messageId,
            SequenceNumber: sequenceNumber,
            CorrelationId: correlationId,
            SessionId: null,
            ReplyToSessionId: null,
            EnqueuedTimeUtc: new DateTime(2026, 4, 25, 9, 0, 0, DateTimeKind.Utc),
            ScheduledEnqueueTimeUtc: new DateTime(2026, 4, 25, 8, 0, 0, DateTimeKind.Utc),
            TimeToLive: TimeSpan.FromMinutes(30),
            LockedUntilUtc: new DateTime(2026, 4, 25, 9, 5, 0, DateTimeKind.Utc),
            ExpiresAtUtc: new DateTime(2026, 4, 25, 9, 30, 0, DateTimeKind.Utc),
            DeliveryCount: deliveryCount,
            Label: label,
            To: null,
            ReplyTo: null,
            PartitionKey: null,
            ContentType: contentType,
            DeadLetterReason: deadLetterReason,
            DeadLetterErrorDescription: deadLetterErrorDescription);

    [Fact]
    public void ReturnSequenceNumberForSeqColumn()
    {
        var msg = BuildMessage(DefaultBroker(sequenceNumber: 42));
        var col = new ColumnConfig("seq", "SEQ #", ColumnSource.BrokerProperty, "SequenceNumber", false, 0);

        MessageGridComponent.GetCellValue(msg, col).ShouldBe("42");
    }

    [Fact]
    public void ReturnMessageIdForMessageIdColumn()
    {
        var msg = BuildMessage(DefaultBroker(messageId: "abc-123"));
        var col = new ColumnConfig("msgid", "MESSAGE ID", ColumnSource.BrokerProperty, "MessageId", true, 1);

        MessageGridComponent.GetCellValue(msg, col).ShouldBe("abc-123");
    }

    [Fact]
    public void ReturnEmptyStringForNullBrokerField()
    {
        var msg = BuildMessage(DefaultBroker(correlationId: null));
        var col = new ColumnConfig("corr", "CORR", ColumnSource.BrokerProperty, "CorrelationId", true, 5);

        MessageGridComponent.GetCellValue(msg, col).ShouldBe(string.Empty);
    }

    [Fact]
    public void ReturnAppPropertyValueWhenPresent()
    {
        var appProps = new Dictionary<string, object> { ["x-tenant-id"] = "tenant-42" };
        var msg = BuildMessage(DefaultBroker(), appProps);
        var col = new ColumnConfig("tenant", "TENANT", ColumnSource.AppProperty, "x-tenant-id", true, 10);

        MessageGridComponent.GetCellValue(msg, col).ShouldBe("tenant-42");
    }

    [Fact]
    public void ReturnEmptyStringForAbsentAppProperty()
    {
        var msg = BuildMessage(DefaultBroker());
        var col = new ColumnConfig("tenant", "TENANT", ColumnSource.AppProperty, "x-tenant-id", true, 10);

        MessageGridComponent.GetCellValue(msg, col).ShouldBe(string.Empty);
    }

    [Fact]
    public void ReturnEmptyStringForUnknownBrokerPropertyKey()
    {
        var msg = BuildMessage(DefaultBroker());
        var col = new ColumnConfig("unk", "UNK", ColumnSource.BrokerProperty, "UnknownField", true, 99);

        MessageGridComponent.GetCellValue(msg, col).ShouldBe(string.Empty);
    }

    [Fact]
    public void ReturnTypedLongForSequenceNumberSortValue()
    {
        var msg = BuildMessage(DefaultBroker(sequenceNumber: 7));
        var col = new ColumnConfig("seq", "SEQ #", ColumnSource.BrokerProperty, "SequenceNumber", false, 0);

        var result = MessageGridComponent.GetSortValue(msg, col);

        result.ShouldBeOfType<long>();
        ((long)result).ShouldBe(7L);
    }

    [Fact]
    public void ReturnTypedIntForDeliveryCountSortValue()
    {
        var msg = BuildMessage(DefaultBroker(deliveryCount: 3));
        var col = new ColumnConfig("delivery", "DELIVERY COUNT", ColumnSource.BrokerProperty, "DeliveryCount", true, 4);

        var result = MessageGridComponent.GetSortValue(msg, col);

        result.ShouldBeOfType<int>();
        ((int)result).ShouldBe(3);
    }

    [Fact]
    public void ReturnStringForAppPropertySortValueWhenNotParseable()
    {
        var appProps = new Dictionary<string, object> { ["priority"] = "high" };
        var msg = BuildMessage(DefaultBroker(), appProps);
        var col = new ColumnConfig("pri", "PRIORITY", ColumnSource.AppProperty, "priority", true, 10);

        var result = MessageGridComponent.GetSortValue(msg, col);

        result.ShouldBeOfType<string>();
        result.ShouldBe("high");
    }

    [Fact]
    public void ReturnDateTimeForAppPropertySortValueWhenDateString()
    {
        var date = new DateTime(2026, 4, 25, 9, 0, 0);
        var appProps = new Dictionary<string, object> { ["created-at"] = date.ToString("O") };
        var msg = BuildMessage(DefaultBroker(), appProps);
        var col = new ColumnConfig("created", "CREATED AT", ColumnSource.AppProperty, "created-at", true, 10);

        var result = MessageGridComponent.GetSortValue(msg, col);

        result.ShouldBeOfType<DateTime>();
    }

    [Fact]
    public void ReturnTypedDateTimeDirectlyForAppPropertyWhenStoredAsDateTime()
    {
        var date = new DateTime(2026, 4, 25, 9, 0, 0);
        var appProps = new Dictionary<string, object> { ["created-at"] = date };
        var msg = BuildMessage(DefaultBroker(), appProps);
        var col = new ColumnConfig("created", "CREATED AT", ColumnSource.AppProperty, "created-at", true, 10);

        var result = MessageGridComponent.GetSortValue(msg, col);

        result.ShouldBeOfType<DateTime>();
        ((DateTime)result).ShouldBe(date);
    }

    [Fact]
    public void ReturnLongForAppPropertySortValueWhenNumericString()
    {
        var appProps = new Dictionary<string, object> { ["retry-count"] = "42" };
        var msg = BuildMessage(DefaultBroker(), appProps);
        var col = new ColumnConfig("retry", "RETRY COUNT", ColumnSource.AppProperty, "retry-count", true, 10);

        var result = MessageGridComponent.GetSortValue(msg, col);

        result.ShouldBeOfType<long>();
        ((long)result).ShouldBe(42L);
    }

    [Fact]
    public void ReturnDeadLetterReasonForDlqReasonColumn()
    {
        var msg = BuildMessage(DefaultBroker(deadLetterReason: "MaxDeliveryCountExceeded"));
        var col = DefaultColumns.DlqColumns[0]; // dlq-reason

        MessageGridComponent.GetCellValue(msg, col).ShouldBe("MaxDeliveryCountExceeded");
    }
}
