using Fluxor;
using Mediator;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Explorer.Messages.SendMessage;
using Messentra.Features.Jobs;
using Messentra.Features.Jobs.ExportMessages;
using Messentra.Features.Jobs.ExportMessages.EnqueueExportMessages;
using Messentra.Features.Jobs.ImportMessages;
using Messentra.Features.Jobs.ImportMessages.EnqueueImportMessages;
using Messentra.Features.Layout.State;
using Messentra.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;

namespace Messentra.Features.Explorer.Resources.Components.Details;

public partial class ResourceDetails
{
    [Parameter]
    public ResourceTreeNode? SelectedResource { get; init; }

    private readonly IDialogService _dialogService;
    private readonly IMediator _mediator;
    private readonly IDispatcher _dispatcher;
    private readonly IFileSystem _fileSystem;
    private readonly IJSRuntime _jsRuntime;
    private int _activeDetailsTabIndex;
    private bool _copiedResourceName;

    public ResourceDetails(IDialogService dialogService, IMediator mediator, IDispatcher dispatcher, IFileSystem fileSystem, IJSRuntime jsRuntime)
    {
        _dialogService = dialogService;
        _mediator = mediator;
        _dispatcher = dispatcher;
        _fileSystem = fileSystem;
        _jsRuntime = jsRuntime;
    }

    private bool IsRefreshing => SelectedResource is { IsLoading: true };
    private bool ShowMessageTabs => SelectedResource is not TopicTreeNode;
    private bool IsMessagesOrDeadLetterTab => _activeDetailsTabIndex is 2 or 3;
    private bool IsMessagesTab => _activeDetailsTabIndex == 2;
    private SubQueue ActiveSubQueue => _activeDetailsTabIndex == 3 ? SubQueue.DeadLetter : SubQueue.Active;
    private bool CanCopyResourceName => SelectedResource is QueueTreeNode or TopicTreeNode or SubscriptionTreeNode;
    private bool CanExportMessages =>
        !IsRefreshing &&
        IsMessagesOrDeadLetterTab &&
        SelectedResource is QueueTreeNode or SubscriptionTreeNode &&
        GetTotalMessagesInSelectedSubQueue() > 0;
    private bool CanImportMessages =>
        !IsRefreshing &&
        IsMessagesTab &&
        SelectedResource is QueueTreeNode or SubscriptionTreeNode;

    private string ResourceName => SelectedResource switch
    {
        QueueTreeNode q => q.Resource.Name,
        TopicTreeNode t => t.Resource.Name,
        SubscriptionTreeNode s => s.Resource.Name,
        NamespaceTreeNode n => n.ConnectionName,
        _ => string.Empty
    };
    
    private string ConnectionName => SelectedResource switch
    {
        QueueTreeNode q => q.ConnectionName,
        TopicTreeNode t => t.ConnectionName,
        SubscriptionTreeNode s => s.ConnectionName,
        NamespaceTreeNode n => n.ConnectionName,
        _ => string.Empty
    };

    private string StatusText => GetStatusText();
    private Color StatusColor => GetStatusColor();

    protected override void OnParametersSet()
    {
        if (!ShowMessageTabs && _activeDetailsTabIndex > 1)
        {
            _activeDetailsTabIndex = 0;
        }
    }

    private async Task CopyResourceName()
    {
        if (!CanCopyResourceName || string.IsNullOrWhiteSpace(ResourceName))
            return;

        await _jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", ResourceName);
        _copiedResourceName = true;
        StateHasChanged();

        await Task.Delay(2000);
        _copiedResourceName = false;
    }

    private string GetStatusText() =>
        SelectedResource switch
        {
            QueueTreeNode q => q.Resource.Overview.Status,
            TopicTreeNode t => t.Resource.Overview.Status,
            SubscriptionTreeNode s => s.Resource.Overview.Status,
            _ => "Unknown"
        };

    private Color GetStatusColor() =>
        GetStatusText() switch
        {
            "Active" => Color.Success,
            "Disabled" => Color.Error,
            "SendDisabled" => Color.Warning,
            "ReceiveDisabled" => Color.Warning,
            _ => Color.Default
        };

    private void RefreshResource()
    {
        switch (SelectedResource)
        {
            case QueueTreeNode queueNode:
                _dispatcher.Dispatch(new RefreshQueueAction(queueNode));
                break;
            case TopicTreeNode topicNode:
                _dispatcher.Dispatch(new RefreshTopicAction(topicNode));
                break;
            case SubscriptionTreeNode subscriptionNode:
                _dispatcher.Dispatch(new RefreshSubscriptionAction(subscriptionNode));
                break;
        }
    }

    private async Task OpenSendMessageDialog()
    {
        if (SelectedResource is null)
            return;

        var parameters = new DialogParameters
        {
            [nameof(SendMessageDialog.ResourceTreeNode)] = SelectedResource
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraLarge,
            FullWidth = true,
            CloseButton = true,
            CloseOnEscapeKey = true
        };

        var dialog = await _dialogService.ShowAsync<SendMessageDialog>("Send Message", parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: SendMessageCommand command })
        {
            _dispatcher.Dispatch(new LogActivityAction(
                new ActivityLogEntry(
                    ConnectionName,
                    "Debug",
                    $"Sending message to {ResourceName}.",
                    DateTime.Now)));
            
            await _mediator.Send(command);
            
            _dispatcher.Dispatch(new LogActivityAction(
                new ActivityLogEntry(
                    ConnectionName,
                    "Info",
                    $"Message sent to {ResourceName}.",
                    DateTime.Now)));
            
            RefreshResource();
        }
    }

    private async Task OpenExportDialog()
    {
        if (!CanExportMessages || SelectedResource is null)
            return;

        var queueLabel = ActiveSubQueue == SubQueue.DeadLetter ? "DLQ " : string.Empty;
        var confirm = await _dialogService.ShowMessageBoxAsync(
            "Export Messages",
            $"This will export {queueLabel}messages from '{ResourceName}'. No messages will be lost. Continue?",
            yesText: "Export",
            cancelText: "Cancel");

        if (confirm != true)
            return;

        var target = CreateExportTarget();
        if (target is null)
            return;

        var totalMessages = GetTotalMessagesInSelectedSubQueue();

        await _mediator.Send(new EnqueueExportMessagesCommand(new ExportMessagesJobRequest(
            SelectedResource.ConnectionConfig,
            target,
            totalMessages)));
        
        _dispatcher.Dispatch(new FetchJobsAction());
        _dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
            ConnectionName,
            "Info",
            $"Export job enqueued for '{ResourceName}' ({ActiveSubQueue}). Go to Jobs menu to monitor progress.",
            DateTime.Now)));
    }

    private async Task OpenImportDialog()
    {
        if (!CanImportMessages || SelectedResource is null)
            return;

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseButton = true,
            CloseOnEscapeKey = true
        };

        var dialog = await _dialogService.ShowAsync<ImportMessagesDialog>("Import Messages", options);
        var result = await dialog.Result;

        if (result is null || result.Canceled)
            return;

        if (result.Data is not ImportMessagesDialogResult importDialogResult)
            return;

        var target = CreateExportTarget();
        if (target is null)
            return;

        var sourceFilePath = await SaveImportFile(importDialogResult.File);

        await _mediator.Send(new EnqueueImportMessagesCommand(new ImportMessagesJobRequest(
            SelectedResource.ConnectionConfig,
            target,
            sourceFilePath,
            string.Empty,
            importDialogResult.GenerateNewMessageId)));

        _dispatcher.Dispatch(new FetchJobsAction());
        _dispatcher.Dispatch(new LogActivityAction(new ActivityLogEntry(
            ConnectionName,
            "Info",
            $"Import job enqueued for '{ResourceName}' ({ActiveSubQueue}). Go to Jobs menu to monitor progress.",
            DateTime.Now)));
    }

    private ResourceTarget? CreateExportTarget() =>
        SelectedResource switch
        {
            QueueTreeNode queueNode => new ResourceTarget.Queue(queueNode.Resource.Name, ActiveSubQueue),
            SubscriptionTreeNode subscriptionNode => new ResourceTarget.TopicSubscription(
                subscriptionNode.Resource.TopicName,
                subscriptionNode.Resource.Name,
                ActiveSubQueue),
            _ => null
        };

    private long GetTotalMessagesInSelectedSubQueue() =>
        SelectedResource switch
        {
            QueueTreeNode queueNode => ActiveSubQueue == SubQueue.DeadLetter
                ? queueNode.Resource.Overview.MessageInfo.DeadLetter
                : queueNode.Resource.Overview.MessageInfo.Active,
            SubscriptionTreeNode subscriptionNode => ActiveSubQueue == SubQueue.DeadLetter
                ? subscriptionNode.Resource.Overview.MessageInfo.DeadLetter
                : subscriptionNode.Resource.Overview.MessageInfo.Active,
            _ => 0
        };

    private async Task<string> SaveImportFile(IBrowserFile file)
    {
        var root = Path.Combine(_fileSystem.GetRootPath(), "Jobs", "Imports");
        _fileSystem.CreateDirectory(root);

        var fileName = string.IsNullOrWhiteSpace(file.Name)
            ? $"import-{Guid.NewGuid():N}.json"
            : $"{Path.GetFileNameWithoutExtension(file.Name)}-{Guid.NewGuid():N}.json";
        var destinationPath = Path.Combine(root, fileName);

        await using var source = file.OpenReadStream(1024L * 1024L * 1024L);
        await using var destination = _fileSystem.OpenWrite(destinationPath, useAsync: true);
        await source.CopyToAsync(destination);
        await destination.FlushAsync();

        return destinationPath;
    }
}

