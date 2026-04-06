using System.Text.Json;
using Messentra.Features.Explorer.Resources;

namespace Messentra.Features.Explorer.Messages.SendMessage;

public sealed class MessageEditModel
{
    public string Body { get; set; } = string.Empty;
    public BodyFormat BodyFormat { get; set; } = BodyFormat.Json;
    public string? MessageId { get; set; }
    public string? Label { get; set; }
    public string? CorrelationId { get; set; }
    public string? SessionId { get; set; }
    public string? ReplyToSessionId { get; set; }
    public string? PartitionKey { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string? TimeToLiveText { get; set; }
    public string? To { get; set; }
    public string? ReplyTo { get; set; }
    public string? ContentType { get; set; }
    public List<CustomProperty> CustomProperties { get; init; } = [];

    public static MessageEditModel FromMessageDto(MessageDto dto)
    {
        var model = new MessageEditModel
        {
            Body = dto.Body,
            MessageId = null,
            Label = dto.BrokerProperties.Label,
            CorrelationId = dto.BrokerProperties.CorrelationId,
            SessionId = dto.BrokerProperties.SessionId,
            ReplyToSessionId = dto.BrokerProperties.ReplyToSessionId,
            PartitionKey = dto.BrokerProperties.PartitionKey,
            ScheduledDate = null,
            ContentType = dto.BrokerProperties.ContentType,
            To = dto.BrokerProperties.To,
            ReplyTo = dto.BrokerProperties.ReplyTo,
            TimeToLiveText = dto.BrokerProperties.TimeToLive == TimeSpan.MaxValue
                ? null
                : dto.BrokerProperties.TimeToLive.ToString()
        };

        foreach (var kvp in dto.ApplicationProperties)
            model.CustomProperties.Add(CustomProperty.FromApplicationProperty(kvp.Key, kvp.Value));

        return model;
    }

    public SendMessageCommand ToSendMessageCommand(ResourceTreeNode resourceTreeNode) =>
        new(
            ResourceTreeNode: resourceTreeNode,
            Body: Body,
            MessageId: NullIfEmpty(MessageId),
            Label: NullIfEmpty(Label),
            CorrelationId: NullIfEmpty(CorrelationId),
            SessionId: NullIfEmpty(SessionId),
            ReplyToSessionId: NullIfEmpty(ReplyToSessionId),
            PartitionKey: NullIfEmpty(PartitionKey),
            ScheduledEnqueueTimeUtc: ScheduledDate,
            TimeToLive: TimeSpan.TryParse(TimeToLiveText, out var ts) ? ts : null,
            To: NullIfEmpty(To),
            ReplyTo: NullIfEmpty(ReplyTo),
            ContentType: NullIfEmpty(ContentType),
            ApplicationProperties: BuildApplicationProperties());

    public SendMessageBatchItem ToSendMessageBatchItem(long sourceSequenceNumber) =>
        new(
            SourceSequenceNumber: sourceSequenceNumber,
            Body: Body,
            MessageId: NullIfEmpty(MessageId),
            Label: NullIfEmpty(Label),
            CorrelationId: NullIfEmpty(CorrelationId),
            SessionId: NullIfEmpty(SessionId),
            ReplyToSessionId: NullIfEmpty(ReplyToSessionId),
            PartitionKey: NullIfEmpty(PartitionKey),
            ScheduledEnqueueTimeUtc: ScheduledDate,
            TimeToLive: TimeSpan.TryParse(TimeToLiveText, out var ts) ? ts : null,
            To: NullIfEmpty(To),
            ReplyTo: NullIfEmpty(ReplyTo),
            ContentType: NullIfEmpty(ContentType),
            ApplicationProperties: BuildApplicationProperties());

    public void FormatBodyAsJson()
    {
        if (BodyFormat != BodyFormat.Json || string.IsNullOrWhiteSpace(Body))
            return;

        try
        {
            var parsed = JsonDocument.Parse(Body);
            Body = JsonSerializer.Serialize(parsed, new JsonSerializerOptions { WriteIndented = true, IndentSize = 4 });
        }
        catch (JsonException)
        {
        }
    }

    private Dictionary<string, object> BuildApplicationProperties()
    {
        var result = new Dictionary<string, object>();
        foreach (var prop in CustomProperties.Where(p => !string.IsNullOrWhiteSpace(p.Key)))
            result[prop.Key.Trim()] = GetCustomPropertyValue(prop);
        return result;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static object GetCustomPropertyValue(CustomProperty property) =>
        property.Type switch
        {
            CustomPropertyType.String => property.Value.Trim(),
            CustomPropertyType.Number => property.NumberValue ?? 0,
            CustomPropertyType.Boolean => property.BooleanValue,
            CustomPropertyType.Date => property.DateValue is { } date
                ? DateTime.SpecifyKind(date.Date + (property.TimeValue ?? TimeSpan.Zero), DateTimeKind.Utc)
                : DateTime.UtcNow,
            _ => property.Value.Trim()
        };
}

public sealed class CustomProperty
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public decimal? NumberValue { get; set; }
    public bool BooleanValue { get; set; }
    public DateTime? DateValue { get; set; }
    public TimeSpan? TimeValue { get; set; }

    public CustomPropertyType Type
    {
        get;
        set
        {
            field = value;
            if (field != CustomPropertyType.Date || DateValue is not null || TimeValue is not null)
                return;
            var now = DateTime.UtcNow;
            DateValue = now.Date;
            TimeValue = now.TimeOfDay;
        }
    } = CustomPropertyType.String;

    public static CustomProperty FromApplicationProperty(string key, object value) =>
        value switch
        {
            bool b => new CustomProperty { Key = key, Type = CustomPropertyType.Boolean, BooleanValue = b },
            int or long or float or double or decimal =>
                new CustomProperty { Key = key, Type = CustomPropertyType.Number, NumberValue = Convert.ToDecimal(value) },
            DateTime dt => new CustomProperty
            {
                Key = key,
                Type = CustomPropertyType.Date,
                DateValue = dt.Date,
                TimeValue = dt.TimeOfDay
            },
            DateTimeOffset dto => new CustomProperty
            {
                Key = key,
                Type = CustomPropertyType.Date,
                DateValue = dto.UtcDateTime.Date,
                TimeValue = dto.UtcDateTime.TimeOfDay
            },
            _ => new CustomProperty { Key = key, Value = value?.ToString() ?? string.Empty }
        };
}

public enum CustomPropertyType
{
    String,
    Number,
    Boolean,
    Date
}
