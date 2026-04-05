using Messentra.Features.Explorer.Messages.SendMessage;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Messentra.Features.Explorer.Resources.Components.Details;

public sealed partial class SendMessageDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public required ResourceTreeNode ResourceTreeNode { get; init; }

    private readonly MessageEditModel _model = new();

    private void Cancel() => MudDialog.Cancel();

    private void Submit() =>
        MudDialog.Close(DialogResult.Ok(_model.ToSendMessageCommand(ResourceTreeNode)));
}
