using Mediator;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Explorer.Messages.FetchQueueMessages;
using Messentra.Features.Explorer.Messages.FetchSubscriptionMessages;
using Microsoft.AspNetCore.Components;
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

    private bool _isGridLoading;
    private bool _isFetchOngoing;
    private bool _isResendOngoing;
    private bool _isCompleteOngoing;
    private bool _isAbandonOngoing;
    private bool _isDeadLetterOngoing;
    private MudDataGrid<ServiceBusMessage> _grid = null!;
    private int _activePanelIndex;
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

    private ServiceBusMessage? _lastSelected;
    private bool _isGridFocused;
    private ElementReference _gridContainer;
    private readonly IDialogService _dialogService;
    private readonly IMediator _mediator;
    private readonly IScrollManager _scrollManager;

    private List<ServiceBusMessage> _messages = [];
    private FetchMessagesOptions? _fetchMessagesOptions;
    private string _searchTerm = string.Empty;

    private List<ServiceBusMessage> FilteredMessages =>
        string.IsNullOrWhiteSpace(_searchTerm)
            ? _messages
            : _messages.Where(m => MatchesSearch(m, _searchTerm)).ToList();

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

    public MessageGrid(IDialogService dialogService, IMediator mediator, IScrollManager scrollManager)
    {
        _dialogService = dialogService;
        _mediator = mediator;
        _scrollManager = scrollManager;
    }

    private string GetRowClass(ServiceBusMessage msg) =>
        SelectedItems.Contains(msg)
            ? $"mud-table-row-selected row-seq-{msg.Message.BrokerProperties.SequenceNumber}"
            : $"row-seq-{msg.Message.BrokerProperties.SequenceNumber}";

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
        await _scrollManager.ScrollIntoViewAsync($".row-seq-{FilteredMessages[index].Message.BrokerProperties.SequenceNumber}", ScrollBehavior.Smooth);
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
        await _scrollManager.ScrollIntoViewAsync($".row-seq-{FilteredMessages[index].Message.BrokerProperties.SequenceNumber}", ScrollBehavior.Smooth);
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

            _fetchMessagesOptions = optionsWithSubQueue;
            _isGridLoading = false;
            _isFetchOngoing = false;

            if (IsReceiveAndDeleteMode)
                await OnRefresh.InvokeAsync();
        }
    }

    private void OnRowClick(ServiceBusMessage message)
    {
        SelectedItems = [message];
    }

    private async Task OnAbandonClicked()
    {
        _isAbandonOngoing = true;
        await Parallel.ForEachAsync(
            SelectedItems,
            new ParallelOptions { MaxDegreeOfParallelism = 100 },
            async (message, ct) => await message.MessageContext.Abandon(ct));

        _messages = _messages.Except(SelectedItems).ToList();
        SelectedItems = [];
        _isAbandonOngoing = false;
        await OnRefresh.InvokeAsync();
    }

    private async Task OnCompleteClicked()
    {
        _isCompleteOngoing = true;
        await Parallel.ForEachAsync(
            SelectedItems,
            new ParallelOptions { MaxDegreeOfParallelism = 100 },
            async (message, ct) => await message.MessageContext.Complete(ct));

        _messages = _messages.Except(SelectedItems).ToList();
        SelectedItems = [];
        _isCompleteOngoing = false;
        await OnRefresh.InvokeAsync();
    }

    private async Task OnDeadLetterClicked()
    {
        _isDeadLetterOngoing = true;
        await Parallel.ForEachAsync(
            SelectedItems,
            new ParallelOptions { MaxDegreeOfParallelism = 100 },
            async (message, ct) => await message.MessageContext.DeadLetter(ct));

        _messages = _messages.Except(SelectedItems).ToList();
        SelectedItems = [];
        _isDeadLetterOngoing = false;
        await OnRefresh.InvokeAsync();
    }
    
    private async Task OnResendClicked()
    {
        _isResendOngoing = true;
        await Parallel.ForEachAsync(
            SelectedItems,
            new ParallelOptions { MaxDegreeOfParallelism = 100 },
            async (message, ct) => await message.MessageContext.Resend(ct));
        _isResendOngoing = false;
        await OnRefresh.InvokeAsync();
    }
}

