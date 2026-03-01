using Fluxor;
using Mediator;
using Messentra.Features.Explorer.Messages.SendMessage;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Messentra.Features.Explorer.Resources.Components.Details;

public partial class ResourceDetails
{
    [Parameter]
    public ResourceTreeItemData SelectedResource { get; init; }

    private readonly IDialogService _dialogService;
    private readonly IMediator _mediator;
    private readonly IDispatcher _dispatcher;

    public ResourceDetails(IDialogService dialogService, IMediator mediator, IDispatcher dispatcher)
    {
        _dialogService = dialogService;
        _mediator = mediator;
        _dispatcher = dispatcher;
    }

    private bool IsRefreshing => SelectedResource.Value!.IsLoading;

    private string StatusText => GetStatusText();
    private Color StatusColor => GetStatusColor();

    private string GetStatusText() =>
        SelectedResource.Value switch
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
        switch (SelectedResource.Value)
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
        if (SelectedResource.Value is null)
            return;

        var parameters = new DialogParameters
        {
            [nameof(SendMessageDialog.ResourceTreeNode)] = SelectedResource.Value
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
            await _mediator.Send(command);
        }
    }
}