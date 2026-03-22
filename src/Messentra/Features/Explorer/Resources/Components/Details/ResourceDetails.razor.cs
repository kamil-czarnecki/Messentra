using Fluxor;
using Mediator;
using Messentra.Features.Explorer.Messages.SendMessage;
using Messentra.Features.Layout.State;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Messentra.Features.Explorer.Resources.Components.Details;

public partial class ResourceDetails
{
    [Parameter]
    public ResourceTreeNode? SelectedResource { get; init; }

    private readonly IDialogService _dialogService;
    private readonly IMediator _mediator;
    private readonly IDispatcher _dispatcher;

    public ResourceDetails(IDialogService dialogService, IMediator mediator, IDispatcher dispatcher)
    {
        _dialogService = dialogService;
        _mediator = mediator;
        _dispatcher = dispatcher;
    }

    private bool IsRefreshing => SelectedResource is { IsLoading: true };

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
            MaxWidth = MaxWidth.Large,
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
}