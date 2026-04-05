using Messentra.Features.Explorer.Messages;
using Messentra.Features.Explorer.Messages.SendMessage;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Messentra.Features.Explorer.Resources.Components.Details;

public sealed partial class ResendMessagesDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter, EditorRequired]
    public required IReadOnlyList<ServiceBusMessage> Messages { get; init; }

    [Parameter, EditorRequired]
    public required ResourceTreeNode ResourceTreeNode { get; init; }

    private List<MessageEditModel> _models = [];
    private int _selectedIndex;
    private IReadOnlyList<ServiceBusMessage>? _sourceMessages;

    protected override void OnParametersSet()
    {
        if (_sourceMessages == Messages) return;
        _sourceMessages = Messages;
        _models = Messages.Select(m => MessageEditModel.FromMessageDto(m.Message)).ToList();
        _selectedIndex = 0;
    }

    private void Cancel() => MudDialog.Cancel();

    private void Submit()
    {
        IReadOnlyList<SendMessageCommand> commands = _models
            .Select(m => m.ToSendMessageCommand(ResourceTreeNode))
            .ToList();

        MudDialog.Close(DialogResult.Ok(commands));
    }
}
