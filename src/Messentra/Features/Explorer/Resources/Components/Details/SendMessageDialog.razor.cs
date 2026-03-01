using System.Text.Json;
using Messentra.Features.Explorer.Messages.SendMessage;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Messentra.Features.Explorer.Resources.Components.Details;

public partial class SendMessageDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public required ResourceTreeNode ResourceTreeNode { get; init; }

    private string _body = string.Empty;
    private BodyFormat _bodyFormat = BodyFormat.Json;

    private string? _messageId;
    private string? _label;
    private string? _correlationId;
    private string? _sessionId;
    private string? _replyToSessionId;
    private string? _partitionKey;
    private DateTime? _scheduledDate;
    private string? _timeToLiveText;
    private string? _to;
    private string? _replyTo;
    private string? _contentType;

    private TimeSpan? ParsedTimeToLive =>
        TimeSpan.TryParse(_timeToLiveText, out var ts) ? ts : null;

    private readonly List<CustomProperty> _customProperties = [];

    private void Cancel() => MudDialog.Cancel();

    private void Submit()
    {
        var appProperties = _customProperties
            .Where(p => !string.IsNullOrWhiteSpace(p.Key))
            .ToDictionary(p => p.Key, p => p.Value);

        var command = new SendMessageCommand(
            ResourceTreeNode: ResourceTreeNode,
            Body: _body,
            MessageId: NullIfEmpty(_messageId),
            Label: NullIfEmpty(_label),
            CorrelationId: NullIfEmpty(_correlationId),
            SessionId: NullIfEmpty(_sessionId),
            ReplyToSessionId: NullIfEmpty(_replyToSessionId),
            PartitionKey: NullIfEmpty(_partitionKey),
            ScheduledEnqueueTimeUtc: _scheduledDate,
            TimeToLive: ParsedTimeToLive,
            To: NullIfEmpty(_to),
            ReplyTo: NullIfEmpty(_replyTo),
            ContentType: NullIfEmpty(_contentType),
            ApplicationProperties: appProperties);

        MudDialog.Close(DialogResult.Ok(command));
    }

    private void FormatJson()
    {
        if (_bodyFormat != BodyFormat.Json || string.IsNullOrWhiteSpace(_body))
            return;

        try
        {
            var parsed = JsonDocument.Parse(_body);
            _body = JsonSerializer.Serialize(parsed,
                new JsonSerializerOptions { WriteIndented = true, IndentSize = 4 });
        }
        catch
        {
            // Not valid JSON — leave as-is
        }
    }

    private void AddCustomProperty() =>
        _customProperties.Add(new CustomProperty());

    private void RemoveCustomProperty(CustomProperty property) =>
        _customProperties.Remove(property);

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private sealed class CustomProperty
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
