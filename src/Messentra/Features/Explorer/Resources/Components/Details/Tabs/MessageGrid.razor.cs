using Fluxor;
using Mediator;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Explorer.Messages.FetchQueueMessages;
using Messentra.Features.Explorer.Messages.FetchSubscriptionMessages;
using Messentra.Features.Layout.State;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Messentra.Features.Explorer.Resources.Components.Details.Tabs;

public partial class MessageGrid
{
    [Parameter, EditorRequired]
    public ResourceTreeNode ResourceTreeNode { get; init; } = null!;

    [Parameter]
    public SubQueue SubQueue { get; init; } = SubQueue.Active;

    [Parameter, EditorRequired]
    public EventCallback OnRefresh { get; set; }

    private readonly IDialogService _dialogService;
    private readonly IMediator _mediator;
    private readonly IDispatcher _dispatcher;
    private readonly IJSRuntime _jsRuntime;

    private bool _isGridLoading;
    private bool _isFetchOngoing;
    private bool _actionOngoing;
    private bool _isGridFocused;
    private int _activePanelIndex;
    private ElementReference _gridContainer;
    private MudDataGrid<ServiceBusMessage> _grid = null!;
    private ServiceBusMessage? _lastSelected;
    private ResourceTreeNode? _previousResourceTreeNode;
    private List<ServiceBusMessage> _messages = [];
    private FetchMessagesOptions? _fetchMessagesOptions;
    private string _searchTerm = string.Empty;

    private bool IsReceiveAndDeleteMode =>
        _fetchMessagesOptions is { Mode: FetchMode.Receive, ReceiveMode: FetchReceiveMode.ReceiveAndDelete };

    private HashSet<ServiceBusMessage> SelectedItems
    {
        get;
        set
        {
            var addedItem = value.Except(field).FirstOrDefault();

            if (addedItem != null)
                _lastSelected = addedItem;

            field = value;

            if (field.Count == 1)
            {
                _grid.ToggleHierarchyVisibilityAsync(field.First());
            }
            else
            {
                _grid.CollapseAllHierarchy();
            }
        }
    } = [];

    private List<ServiceBusMessage> FilteredMessages =>
        string.IsNullOrWhiteSpace(_searchTerm)
            ? _messages
            : _messages.Where(m => MatchesSearch(m, _searchTerm)).ToList();

    private string ConnectionName => ResourceTreeNode switch
    {
        QueueTreeNode q => q.ConnectionName,
        SubscriptionTreeNode s => s.ConnectionName,
        _ => string.Empty
    };

    private string ResourceName => ResourceTreeNode switch
    {
        QueueTreeNode q => q.Resource.Name,
        SubscriptionTreeNode s => $"{s.Resource.TopicName}/{s.Resource.Name}",
        _ => ResourceTreeNode.GetType().Name
    };

    public MessageGrid(IDialogService dialogService, IMediator mediator, IDispatcher dispatcher, IJSRuntime jsRuntime)
    {
        _dialogService = dialogService;
        _mediator = mediator;
        _dispatcher = dispatcher;
        _jsRuntime = jsRuntime;
    }

    private string? ResourceKey(ResourceTreeNode? node) => node switch
    {
        QueueTreeNode q => $"queue:{q.ConnectionName}:{q.Resource.Name}:{SubQueue}",
        SubscriptionTreeNode s =>
            $"subscription:{s.ConnectionName}:{s.Resource.TopicName}:{s.Resource.Name}:{SubQueue}",
        _ => null
    };

    protected override void OnParametersSet()
    {
        var currentKey  = ResourceKey(ResourceTreeNode);
        var previousKey = ResourceKey(_previousResourceTreeNode);
        
        _previousResourceTreeNode = ResourceTreeNode;
        
        if (currentKey != null && currentKey == previousKey)
            return;
        
        _messages = [];
        _lastSelected = null;
        _searchTerm = string.Empty;
        _fetchMessagesOptions = null;
    }

    private string GetRowClass(ServiceBusMessage msg) =>
        SelectedItems.Contains(msg)
            ? $"mud-table-row-selected row-seq-{msg.Message.BrokerProperties.SequenceNumber}"
            : $"row-seq-{msg.Message.BrokerProperties.SequenceNumber}";

    private static bool MatchesSearch(ServiceBusMessage msg, string term)
    {
        var t = term.Trim();
        var b = msg.Message.BrokerProperties;

        if (msg.Message.Body.Contains(t, StringComparison.OrdinalIgnoreCase))
            return true;

        if (b.SequenceNumber.ToString().Contains(t, StringComparison.OrdinalIgnoreCase)) return true;
        if (b.DeliveryCount.ToString().Contains(t, StringComparison.OrdinalIgnoreCase)) return true;
        if (b.EnqueuedTimeUtc.ToString(System.Globalization.CultureInfo.InvariantCulture).Contains(t, StringComparison.OrdinalIgnoreCase)) return true;

        var strings = new[]
        {
            b.MessageId, b.CorrelationId, b.SessionId, b.ReplyToSessionId,
            b.Label, b.To, b.ReplyTo, b.PartitionKey, b.ContentType,
            b.DeadLetterReason, b.DeadLetterErrorDescription
        };

        if (strings.Any(s => s?.Contains(t, StringComparison.OrdinalIgnoreCase) == true))
            return true;

        return msg.Message.ApplicationProperties
            .Any(kv =>
                kv.Key.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                (kv.Value.ToString() ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase));
    }

    private void OnRowClick(ServiceBusMessage message)
    {
        SelectedItems = [message];
    }

    private async Task OnKeyUpPressed()
    {
        if (!_isGridFocused || _messages.Count == 0)
            return;

        await _gridContainer.FocusAsync();

        if (_lastSelected is null)
        {
            SelectedItems = [_messages[0]];
            return;
        }

        var index = Math.Max(0, FilteredMessages.IndexOf(_lastSelected) - 1);
        SelectedItems = [FilteredMessages[index]];
        await _jsRuntime.InvokeVoidAsync("messentra.scrollRowNearTop",
            ".message-grid",
            $".row-seq-{FilteredMessages[index].Message.BrokerProperties.SequenceNumber}",
            index,
            36,
            3);
    }

    private async Task OnKeyDownPressed()
    {
        if (!_isGridFocused || _messages.Count == 0)
            return;

        await _gridContainer.FocusAsync();

        if (_lastSelected is null)
        {
            SelectedItems = [_messages[0]];
            return;
        }

        var index = Math.Min(FilteredMessages.Count - 1, FilteredMessages.IndexOf(_lastSelected) + 1);
        SelectedItems = [FilteredMessages[index]];
        await _jsRuntime.InvokeVoidAsync("messentra.scrollRowNearTop",
            ".message-grid",
            $".row-seq-{FilteredMessages[index].Message.BrokerProperties.SequenceNumber}",
            index,
            36,
            3);
    }

    private async Task OnFetchMessagesClicked()
    {
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseButton = true,
            CloseOnEscapeKey = true
        };

        var dialogReference = await _dialogService.ShowAsync<FetchMessagesOptionsDialog>("Fetch Messages", options);
        var result = await dialogReference.Result;

        if (result is { Canceled: false, Data: FetchMessagesOptions optionsData })
        {
            _isGridLoading = true;
            _isFetchOngoing = true;

            var optionsWithSubQueue = optionsData with { SubQueue = SubQueue };

            try
            {
                _dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                    ConnectionName, "Debug",
                    $"Fetching messages from '{ResourceName}'...",
                    DateTime.Now)));

                _messages = SubQueue switch
                {
                    _ when ResourceTreeNode is QueueTreeNode queue =>
                        (await _mediator.Send(new FetchQueueMessagesQuery(
                            QueueName: queue.Resource.Name,
                            ConnectionConfig: queue.ConnectionConfig,
                            Options: optionsWithSubQueue))).ToList(),

                    _ when ResourceTreeNode is SubscriptionTreeNode subscription =>
                        (await _mediator.Send(new FetchSubscriptionMessagesQuery(
                            TopicName: subscription.Resource.TopicName,
                            SubscriptionName: subscription.Resource.Name,
                            ConnectionConfig: subscription.ConnectionConfig,
                            Options: optionsWithSubQueue))).ToList(),

                    _ => throw new InvalidOperationException(
                        $"Unsupported resource type: {ResourceTreeNode.GetType().Name}")
                };

                _dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                    ConnectionName, "Info",
                    $"Fetched {_messages.Count} message(s) from '{ResourceName}'.",
                    DateTime.Now)));
            }
            catch (Exception ex)
            {
                _dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                    ConnectionName, "Error",
                    $"Fetching messages from '{ResourceName}' failed: {ex.Message}",
                    DateTime.Now)));
            }

            _fetchMessagesOptions = optionsWithSubQueue;
            _isGridLoading = false;
            _isFetchOngoing = false;

            if (IsReceiveAndDeleteMode)
                await OnRefresh.InvokeAsync();
        }
    }

    private async Task OnResendClicked()
    {
        _actionOngoing = true;
        var count = SelectedItems.Count;
        _dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
            ConnectionName, "Debug",
            $"Resending {count} message(s) from '{ResourceName}'...",
            DateTime.Now)));
        try
        {
            await Parallel.ForEachAsync(
                SelectedItems,
                new ParallelOptions { MaxDegreeOfParallelism = 100 },
                async (message, ct) => await message.MessageContext.Resend(ct));

            _dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                ConnectionName, "Info",
                $"Resent {count} message(s) from '{ResourceName}'.",
                DateTime.Now)));
        }
        catch (Exception ex)
        {
            _dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                ConnectionName, "Error",
                $"Resending messages from '{ResourceName}' failed: {ex.Message}",
                DateTime.Now)));
        }
        _actionOngoing = false;
        await OnRefresh.InvokeAsync();
    }

    private async Task OnCompleteClicked()
    {
        _actionOngoing = true;
        var count = SelectedItems.Count;
        _dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
            ConnectionName, "Debug",
            $"Completing {count} message(s) from '{ResourceName}'...",
            DateTime.Now)));
        try
        {
            await Parallel.ForEachAsync(
                SelectedItems,
                new ParallelOptions { MaxDegreeOfParallelism = 100 },
                async (message, ct) => await message.MessageContext.Complete(ct));

            _messages = _messages.Except(SelectedItems).ToList();
            SelectedItems = [];
            _dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                ConnectionName, "Info",
                $"Completed {count} message(s) from '{ResourceName}'.",
                DateTime.Now)));
        }
        catch (Exception ex)
        {
            _dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                ConnectionName, "Error",
                $"Completing messages from '{ResourceName}' failed: {ex.Message}",
                DateTime.Now)));
        }
        _actionOngoing = false;
        await OnRefresh.InvokeAsync();
    }

    private async Task OnAbandonClicked()
    {
        _actionOngoing = true;
        var count = SelectedItems.Count;
        _dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
            ConnectionName, "Debug",
            $"Abandoning {count} message(s) from '{ResourceName}'...",
            DateTime.Now)));
        try
        {
            await Parallel.ForEachAsync(
                SelectedItems,
                new ParallelOptions { MaxDegreeOfParallelism = 100 },
                async (message, ct) => await message.MessageContext.Abandon(ct));

            _messages = _messages.Except(SelectedItems).ToList();
            SelectedItems = [];
            _dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                ConnectionName, "Info",
                $"Abandoned {count} message(s) from '{ResourceName}'.",
                DateTime.Now)));
        }
        catch (Exception ex)
        {
            _dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                ConnectionName, "Error",
                $"Abandoning messages from '{ResourceName}' failed: {ex.Message}",
                DateTime.Now)));
        }
        _actionOngoing = false;
        await OnRefresh.InvokeAsync();
    }

    private async Task OnDeadLetterClicked()
    {
        _actionOngoing = true;
        var count = SelectedItems.Count;
        _dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
            ConnectionName, "Debug",
            $"Dead-lettering {count} message(s) from '{ResourceName}'...",
            DateTime.Now)));
        try
        {
            await Parallel.ForEachAsync(
                SelectedItems,
                new ParallelOptions { MaxDegreeOfParallelism = 100 },
                async (message, ct) => await message.MessageContext.DeadLetter(ct));

            _messages = _messages.Except(SelectedItems).ToList();
            SelectedItems = [];
            _dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                ConnectionName, "Info",
                $"Dead-lettered {count} message(s) from '{ResourceName}'.",
                DateTime.Now)));
        }
        catch (Exception ex)
        {
            _dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
                ConnectionName, "Error",
                $"Dead-lettering messages from '{ResourceName}' failed: {ex.Message}",
                DateTime.Now)));
        }
        _actionOngoing = false;
        await OnRefresh.InvokeAsync();
    }
}
