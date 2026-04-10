using Fluxor;
using Mediator;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Explorer.Messages.ActionProgress;
using Messentra.Features.Explorer.Messages.FetchQueueMessages;
using Messentra.Features.Explorer.Messages.FetchSubscriptionMessages;
using Messentra.Features.Explorer.Messages.SendMessage;
using Messentra.Features.Jobs;
using Messentra.Features.Jobs.ExportSelectedMessages;
using Messentra.Features.Jobs.ExportSelectedMessages.EnqueueExportSelectedMessages;
using Messentra.Features.Jobs.Stages;
using Messentra.Features.Layout.State;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System.Collections.Concurrent;

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
    private HashSet<ServiceBusMessage> _selectedMessages = [];
    private FetchMessagesOptions? _fetchMessagesOptions;
    private string _searchTerm = string.Empty;
    private int _searchFieldRenderKey;

    private string SearchTerm
    {
        get => _searchTerm;
        set
        {
            if (_searchTerm == value)
                return;

            _searchTerm = value;
            SyncHierarchyWithVisibleSelection();
        }
    }

    private bool IsReceiveAndDeleteMode =>
        _fetchMessagesOptions is { Mode: FetchMode.Receive, ReceiveMode: FetchReceiveMode.ReceiveAndDelete };

    private HashSet<ServiceBusMessage> SelectedItems
    {
        get
        {
            var filtered = FilteredMessages;
            return _selectedMessages.Count == 0 ? [] : [.._selectedMessages.Where(filtered.Contains)];
        }
        set
        {
            var addedItem = value.Except(_selectedMessages).FirstOrDefault();
            if (addedItem != null)
                _lastSelected = addedItem;

            var filteredSet = FilteredMessages.ToHashSet();
            _selectedMessages = [.._selectedMessages.Where(m => !filteredSet.Contains(m)), ..value];
            var visibleSelectedMessages = value.Where(filteredSet.Contains).ToList();

            if (visibleSelectedMessages.Count == 1)
                _grid.ToggleHierarchyVisibilityAsync(visibleSelectedMessages[0]);
            else if (value.Count == 0 && _selectedMessages.Count == 1)
            {
                // Preserve detail-row expansion while the only selected message is temporarily hidden by filter.
            }
            else
                _grid.CollapseAllHierarchy();
        }
    }

    private List<ServiceBusMessage> FilteredMessages =>
        string.IsNullOrWhiteSpace(_searchTerm)
            ? _messages
            : _messages.Where(m => MatchesSearch(m, _searchTerm)).ToList();

    private void SyncHierarchyWithVisibleSelection()
    {
        if (_grid is null)
            return;

        var filteredSet = FilteredMessages.ToHashSet();
        var visibleSelectedMessages = _selectedMessages.Where(filteredSet.Contains).ToList();

        if (visibleSelectedMessages.Count == 1)
            _grid.ToggleHierarchyVisibilityAsync(visibleSelectedMessages[0]);
        else if (_selectedMessages.Count == 1)
        {
            // Keep current expansion state while the only selected item is temporarily hidden by filter.
        }
        else
            _grid.CollapseAllHierarchy();
    }

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
        _selectedMessages = [];
        _lastSelected = null;
        _searchTerm = string.Empty;
        _searchFieldRenderKey++;
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

    private static DialogOptions ActionProgressDialogOptions() => new()
    {
        MaxWidth = MaxWidth.ExtraSmall,
        FullWidth = true,
        CloseButton = false,
        CloseOnEscapeKey = false,
        BackdropClick = false
    };

    private async Task ShowActionProgressDialog(
        string actionLabel,
        string actionIcon,
        string subLabel,
        int totalCount,
        Func<IProgress<ActionProgressUpdate>, CancellationToken, Task> onRunAction)
    {
        _actionOngoing = true;

        var parameters = new DialogParameters
        {
            [nameof(ActionProgressDialog.ActionLabel)] = actionLabel,
            [nameof(ActionProgressDialog.ActionIcon)] = actionIcon,
            [nameof(ActionProgressDialog.SubLabel)] = subLabel,
            [nameof(ActionProgressDialog.TotalCount)] = totalCount,
            [nameof(ActionProgressDialog.OnRunAction)] = onRunAction
        };

        var dialogRef = await _dialogService.ShowAsync<ActionProgressDialog>(
            string.Empty, parameters, ActionProgressDialogOptions());

        await dialogRef.Result;
        _actionOngoing = false;
    }

    private static async Task RunParallelAction(
        IReadOnlyList<ServiceBusMessage> messages,
        ConcurrentBag<ServiceBusMessage> successful,
        IProgress<ActionProgressUpdate> progress,
        CancellationToken ct,
        Func<ServiceBusMessage, CancellationToken, Task> operation)
    {
        var succeeded = 0;
        var failed = 0;
        var total = messages.Count;

        await Parallel.ForEachAsync(
            messages,
            new ParallelOptions { MaxDegreeOfParallelism = 100, CancellationToken = ct },
            async (message, innerCt) =>
            {
                string? failedId = null;
                string? failedReason = null;

                try
                {
                    await operation(message, innerCt);
                    Interlocked.Increment(ref succeeded);
                    successful.Add(message);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failed);
                    failedId = message.Message.BrokerProperties.MessageId
                        ?? message.Message.BrokerProperties.SequenceNumber.ToString();
                    failedReason = ex.Message;
                }

                var pending = Math.Max(0, total - succeeded - failed);
                progress.Report(new ActionProgressUpdate(succeeded, failed, pending, failedId, failedReason));
            });
    }

    private void RemoveSuccessful(ConcurrentBag<ServiceBusMessage> successful)
    {
        if (successful.IsEmpty) return;
        _messages = _messages.Except(successful).ToList();
        _selectedMessages = [.._selectedMessages.Except(successful)];
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
        _selectedMessages.Contains(msg)
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
            _selectedMessages = [];
            _searchTerm = string.Empty;
            _searchFieldRenderKey++;

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

    private async Task OnExportSelectedClicked()
    {
        var selectedList = _selectedMessages
            .OrderBy(x => x.Message.BrokerProperties.SequenceNumber)
            .ToList();

        var count = selectedList.Count;
        var confirm = await _dialogService.ShowMessageBoxAsync(
            "Export Selected Messages",
            $"This will export {count} selected message(s) from '{ResourceName}'. No messages will be lost. Continue?",
            yesText: "Export",
            cancelText: "Cancel");

        if (confirm != true)
            return;

        _actionOngoing = true;

        try
        {
            var dtos = selectedList
                .Select(x => ServiceBusMessageDto.From(x.Message))
                .ToList();

            var resourceLabel = $"{ResourceName}-{SubQueue}";

            await _mediator.Send(new EnqueueExportSelectedMessagesCommand(
                new ExportSelectedMessagesJobRequest(dtos, resourceLabel)));

            _dispatcher.Dispatch(new FetchJobsAction());
            LogActivity("Info", $"Export job enqueued for {count} selected message(s) from '{ResourceName}' ({SubQueue}). Go to Jobs menu to monitor progress.");
        }
        catch (Exception ex)
        {
            LogActivity("Error", $"Exporting messages from '{ResourceName}' failed: {ex.Message}");
        }
        finally
        {
            _actionOngoing = false;
        }
    }

    private async Task OnResendClicked(bool autoComplete)
    {
        var selectedList = _selectedMessages
            .OrderBy(x => x.Message.BrokerProperties.SequenceNumber)
            .ToList();

        var dialogOptions = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraLarge,
            FullWidth = true,
            CloseButton = true,
            CloseOnEscapeKey = true
        };

        var parameters = new DialogParameters
        {
            [nameof(ResendMessagesDialog.Messages)] = selectedList,
            [nameof(ResendMessagesDialog.ResourceTreeNode)] = ResourceTreeNode
        };

        var dialogRef = await _dialogService.ShowAsync<ResendMessagesDialog>("Resend Messages", parameters, dialogOptions);
        var result = await dialogRef.Result;

        if (result is not { Canceled: false, Data: IReadOnlyList<SendMessageBatchItem> batchItems })
            return;

        var successful = new ConcurrentBag<ServiceBusMessage>();
        var count = selectedList.Count;

        await ShowActionProgressDialog(
            actionLabel: "Resend",
            actionIcon: Icons.Material.Filled.MoveUp,
            subLabel: ResourceName,
            totalCount: count,
            onRunAction: async (progress, ct) =>
            {
                const int resendChunkSize = 200;

                var succeeded = 0;
                var failed = 0;
                string? failedId = null;
                string? failedReason = null;
                var selectedBySequenceNumber = selectedList
                    .ToDictionary(x => x.Message.BrokerProperties.SequenceNumber);
                var addedSuccessfulSequenceNumbers = new HashSet<long>();

                progress.Report(new ActionProgressUpdate(0, 0, count));

                foreach (var batchChunk in batchItems.Chunk(resendChunkSize))
                {
                    ct.ThrowIfCancellationRequested();

                    var resendResult = await _mediator.Send(
                        new SendMessagesCommand(ResourceTreeNode, batchChunk), ct);

                    succeeded += resendResult.SentCount;
                    failed += resendResult.FailedCount;

                    var chunkResentMessages = resendResult.SentSequenceNumbers
                        .Select(selectedBySequenceNumber.GetValueOrDefault)
                        .Where(message => message is not null)
                        .Cast<ServiceBusMessage>()
                        .ToList();

                    if (autoComplete && chunkResentMessages.Count > 0)
                    {
                        await Parallel.ForEachAsync(
                            chunkResentMessages,
                            new ParallelOptions { MaxDegreeOfParallelism = 100, CancellationToken = ct },
                            async (message, innerCt) =>
                            {
                                try
                                {
                                    await message.MessageContext.Complete(innerCt);

                                    var sequenceNumber = message.Message.BrokerProperties.SequenceNumber;
                                    lock (addedSuccessfulSequenceNumbers)
                                    {
                                        if (addedSuccessfulSequenceNumbers.Add(sequenceNumber))
                                            successful.Add(message);
                                    }
                                }
                                catch
                                {
                                    // ignored
                                }
                            });
                    }
                    else
                    {
                        foreach (var message in chunkResentMessages)
                        {
                            var sequenceNumber = message.Message.BrokerProperties.SequenceNumber;
                            if (addedSuccessfulSequenceNumbers.Add(sequenceNumber))
                                successful.Add(message);
                        }
                    }

                    if (resendResult.FailedCount > 0)
                    {
                        var firstError = resendResult.Errors.FirstOrDefault();
                        failedId = firstError?.SourceSequenceNumber.ToString() ?? "unknown";
                        failedReason = firstError?.Message ?? "Unknown error";
                    }

                    progress.Report(new ActionProgressUpdate(
                        succeeded,
                        failed,
                        Math.Max(0, count - succeeded - failed),
                        failedId,
                        failedReason));
                }


                if (failed == 0)
                    LogActivity("Info", $"Resent {succeeded}/{count} message(s) from '{ResourceName}'.");
                else
                    LogActivity("Warning", $"Resent {succeeded}/{count} message(s) from '{ResourceName}'. Failed: {failed}.");
            });

        RemoveSuccessful(successful);
        await OnRefresh.InvokeAsync();
    }

    private async Task OnCompleteClicked()
    {
        var messages = _selectedMessages.ToList();
        var successful = new ConcurrentBag<ServiceBusMessage>();

        await ShowActionProgressDialog(
            actionLabel: "Complete",
            actionIcon: Icons.Material.Filled.Check,
            subLabel: ResourceName,
            totalCount: messages.Count,
            onRunAction: (progress, ct) =>
                RunParallelAction(messages, successful, progress, ct,
                    (msg, innerCt) => msg.MessageContext.Complete(innerCt)));

        RemoveSuccessful(successful);
        LogActivity("Info", $"Completed {successful.Count}/{messages.Count} message(s) from '{ResourceName}'.");
        await OnRefresh.InvokeAsync();
    }

    private async Task OnAbandonClicked()
    {
        var messages = _selectedMessages.ToList();
        var successful = new ConcurrentBag<ServiceBusMessage>();

        await ShowActionProgressDialog(
            actionLabel: "Abandon",
            actionIcon: Icons.Material.Filled.LockOpen,
            subLabel: ResourceName,
            totalCount: messages.Count,
            onRunAction: (progress, ct) =>
                RunParallelAction(messages, successful, progress, ct,
                    (msg, innerCt) => msg.MessageContext.Abandon(innerCt)));

        RemoveSuccessful(successful);
        LogActivity("Info", $"Abandoned {successful.Count}/{messages.Count} message(s) from '{ResourceName}'.");
        await OnRefresh.InvokeAsync();
    }

    private async Task OnDeadLetterClicked()
    {
        var messages = _selectedMessages.ToList();
        var successful = new ConcurrentBag<ServiceBusMessage>();

        await ShowActionProgressDialog(
            actionLabel: "Dead-letter",
            actionIcon: Icons.Material.Filled.Delete,
            subLabel: ResourceName,
            totalCount: messages.Count,
            onRunAction: (progress, ct) =>
                RunParallelAction(messages, successful, progress, ct,
                    (msg, innerCt) => msg.MessageContext.DeadLetter(innerCt)));

        RemoveSuccessful(successful);
        LogActivity("Info", $"Dead-lettered {successful.Count}/{messages.Count} message(s) from '{ResourceName}'.");
        await OnRefresh.InvokeAsync();
    }
}
