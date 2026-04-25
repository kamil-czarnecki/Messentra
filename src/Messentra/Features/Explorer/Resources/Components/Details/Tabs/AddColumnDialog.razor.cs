using Messentra.Features.Explorer.MessageGrid;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Messentra.Features.Explorer.Resources.Components.Details.Tabs;

public partial class AddColumnDialog
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter, EditorRequired]
    public IReadOnlyList<ColumnConfig> ExistingColumns { get; set; } = [];

    private static readonly IReadOnlyList<string> AllBrokerKeys =
    [
        "SequenceNumber", "MessageId", "CorrelationId", "SessionId", "ReplyToSessionId",
        "EnqueuedTimeUtc", "ScheduledEnqueueTimeUtc", "TimeToLive", "LockedUntilUtc", "ExpiresAtUtc",
        "DeliveryCount", "Label", "To", "ReplyTo", "PartitionKey", "ContentType",
        "DeadLetterReason", "DeadLetterErrorDescription"
    ];

    private ColumnSource _source = ColumnSource.BrokerProperty;
    private string _selectedBrokerKey = string.Empty;
    private string _appPropertyKey = string.Empty;
    private string _title = string.Empty;

    private IReadOnlyList<string> AvailableBrokerKeys =>
        AllBrokerKeys
            .Where(k => ExistingColumns.All(c => c.PropertyKey != k))
            .ToList();

    private bool CanConfirm => _source == ColumnSource.BrokerProperty
        ? !string.IsNullOrWhiteSpace(_selectedBrokerKey)
        : !string.IsNullOrWhiteSpace(_appPropertyKey);

    private void OnSourceChanged(ColumnSource source)
    {
        _source = source;
        _title = string.Empty;
        _selectedBrokerKey = string.Empty;
        _appPropertyKey = string.Empty;
    }

    private void OnBrokerKeyChanged(string key)
    {
        _selectedBrokerKey = key;
        if (string.IsNullOrWhiteSpace(_title))
            _title = key;
    }

    private void OnAppKeyChanged(string key)
    {
        _appPropertyKey = key;
        if (string.IsNullOrWhiteSpace(_title))
            _title = key;
    }

    private void Confirm()
    {
        var key = _source == ColumnSource.BrokerProperty ? _selectedBrokerKey : _appPropertyKey;
        var title = string.IsNullOrWhiteSpace(_title) ? key : _title;

        var config = new ColumnConfig(
            Id: $"custom-{Guid.NewGuid():N}",
            Title: title,
            Source: _source,
            PropertyKey: key,
            IsRemovable: true,
            Order: 0);

        MudDialog.Close(DialogResult.Ok(config));
    }

    private void Cancel() => MudDialog.Cancel();
}
