using Fluxor;
using Mediator;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Explorer.Messages.FetchQueueMessages;
using Messentra.Features.Explorer.Messages.FetchSubscriptionMessages;
using Messentra.Features.Explorer.Messages.SendMessage;
using Messentra.Features.Explorer.Resources.Components.Details;
using Messentra.Features.Layout.State;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Messentra.Features.Explorer.Resources.Components.Details.Tabs;

public partial class MessageGrid : IDisposable
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
    private CancellationTokenSource _resourceOperationCts = new();
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

        var previousCts = _resourceOperationCts;
        _resourceOperationCts = new CancellationTokenSource();
        previousCts.Cancel();
        previousCts.Dispose();
        
        _messages = [];
        _lastSelected = null;
        _searchTerm = string.Empty;
        _fetchMessagesOptions = null;
        _isGridLoading = false;
        _isFetchOngoing = false;
        _actionOngoing = false;
    }

    private void LogCanceledOperation(string operation)
    {
        LogActivity("Warning", $"{operation} canceled for '{ResourceName}'.");
    }

    private void LogActivity(string level, string message)
    {
        _dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
            ConnectionName,
            level,
            message,
            DateTime.Now)));
    }

    private async Task ExecuteMessageAction(
        string operationName,
        string successVerb,
        Func<ServiceBusMessage, CancellationToken, Task> operation,
        bool removeProcessedMessages)
    {
        _actionOngoing = true;
        var count = SelectedItems.Count;
        var cancellationToken = _resourceOperationCts.Token;

        LogActivity("Debug", $"{operationName} {count} message(s) from '{ResourceName}'...");

        try
        {
            await Parallel.ForEachAsync(
                SelectedItems,
                new ParallelOptions { MaxDegreeOfParallelism = 100, CancellationToken = cancellationToken },
                async (message, ct) => await operation(message, ct));

            if (cancellationToken.IsCancellationRequested)
            {
                LogCanceledOperation(operationName);
                return;
            }

            if (removeProcessedMessages)
            {
                _messages = _messages.Except(SelectedItems).ToList();
                SelectedItems = [];
            }

            LogActivity("Info", $"{successVerb} {count} message(s) from '{ResourceName}'.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LogCanceledOperation(operationName);
        }
        catch (Exception ex)
        {
            LogActivity("Error", $"{operationName} messages from '{ResourceName}' failed: {ex.Message}");
        }
        finally
        {
            _actionOngoing = false;
            await OnRefresh.InvokeAsync();
        }
    }

    private static DialogOptions CreateFetchDialogOptions() => new()
    {
        MaxWidth = MaxWidth.Small,
        FullWidth = true,
        CloseButton = true,
        CloseOnEscapeKey = true
    };

    private void SetFetchState(bool isLoading)
    {
        _isGridLoading = isLoading;
        _isFetchOngoing = isLoading;
    }

    private async Task<List<ServiceBusMessage>> FetchMessagesForCurrentResource(
        FetchMessagesOptions options,
        CancellationToken cancellationToken)
    {
        return ResourceTreeNode switch
        {
            QueueTreeNode queue =>
                (await _mediator.Send(new FetchQueueMessagesQuery(
                    QueueName: queue.Resource.Name,
                    ConnectionConfig: queue.ConnectionConfig,
                    Options: options), cancellationToken)).ToList(),

            SubscriptionTreeNode subscription =>
                (await _mediator.Send(new FetchSubscriptionMessagesQuery(
                    TopicName: subscription.Resource.TopicName,
                    SubscriptionName: subscription.Resource.Name,
                    ConnectionConfig: subscription.ConnectionConfig,
                    Options: options), cancellationToken)).ToList(),

            _ => throw new InvalidOperationException(
                $"Unsupported resource type: {ResourceTreeNode.GetType().Name}")
        };
    }

    public void Dispose()
    {
        _resourceOperationCts.Cancel();
        _resourceOperationCts.Dispose();
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
        var options = CreateFetchDialogOptions();

        var dialogReference = await _dialogService.ShowAsync<FetchMessagesOptionsDialog>("Fetch Messages", options);
        var result = await dialogReference.Result;

        if (result is { Canceled: false, Data: FetchMessagesOptions optionsData })
        {
            SetFetchState(true);

            var optionsWithSubQueue = optionsData with { SubQueue = SubQueue };
            var cancellationToken = _resourceOperationCts.Token;

            try
            {
                LogActivity("Debug", $"Fetching messages from '{ResourceName}'...");

                _messages = await FetchMessagesForCurrentResource(optionsWithSubQueue, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    LogCanceledOperation("Fetching messages");
                    return;
                }

                LogActivity("Info", $"Fetched {_messages.Count} message(s) from '{ResourceName}'.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                LogCanceledOperation("Fetching messages");
            }
            catch (Exception ex)
            {
                LogActivity("Error", $"Fetching messages from '{ResourceName}' failed: {ex.Message}");
            }
            finally
            {
                _fetchMessagesOptions = optionsWithSubQueue;
                SetFetchState(false);

                if (IsReceiveAndDeleteMode)
                    await OnRefresh.InvokeAsync();
            }
        }
    }

    private async Task OnResendClicked(bool autoComplete)
    {
        var dialogOptions = new DialogOptions
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseButton = true,
            CloseOnEscapeKey = true
        };

        var parameters = new DialogParameters
        {
            [nameof(ResendMessagesDialog.Messages)] = SelectedItems.ToList(),
            [nameof(ResendMessagesDialog.ResourceTreeNode)] = ResourceTreeNode
        };

        var dialogRef = await _dialogService.ShowAsync<ResendMessagesDialog>("Resend Messages", parameters, dialogOptions);
        var result = await dialogRef.Result;

        if (result is not { Canceled: false, Data: IReadOnlyList<SendMessageCommand> commands })
            return;

        _actionOngoing = true;
        var selectedList = SelectedItems.ToList();
        var count = selectedList.Count;
        var cancellationToken = _resourceOperationCts.Token;

        LogActivity("Debug", $"Resending {count} message(s) from '{ResourceName}'...");

        try
        {
            await Parallel.ForEachAsync(
                Enumerable.Range(0, count),
                new ParallelOptions { MaxDegreeOfParallelism = 100, CancellationToken = cancellationToken },
                async (i, ct) =>
                {
                    await _mediator.Send(commands[i], ct);
                    if (autoComplete)
                        await selectedList[i].MessageContext.Complete(ct);
                });

            if (cancellationToken.IsCancellationRequested)
            {
                LogCanceledOperation("Resending");
                return;
            }

            if (autoComplete)
            {
                _messages = _messages.Except(SelectedItems).ToList();
                SelectedItems = [];
            }

            LogActivity("Info", $"Resent {count} message(s) from '{ResourceName}'.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LogCanceledOperation("Resending");
        }
        catch (Exception ex)
        {
            LogActivity("Error", $"Resending messages from '{ResourceName}' failed: {ex.Message}");
        }
        finally
        {
            _actionOngoing = false;
            await OnRefresh.InvokeAsync();
        }
    }

    private async Task OnCompleteClicked()
    {
        await ExecuteMessageAction(
            operationName: "Completing",
            successVerb: "Completed",
            operation: async (message, ct) => await message.MessageContext.Complete(ct),
            removeProcessedMessages: true);
    }

    private async Task OnAbandonClicked()
    {
        await ExecuteMessageAction(
            operationName: "Abandoning",
            successVerb: "Abandoned",
            operation: async (message, ct) => await message.MessageContext.Abandon(ct),
            removeProcessedMessages: true);
    }

    private async Task OnDeadLetterClicked()
    {
        await ExecuteMessageAction(
            operationName: "Dead-lettering",
            successVerb: "Dead-lettered",
            operation: async (message, ct) => await message.MessageContext.DeadLetter(ct),
            removeProcessedMessages: true);
    }
}
