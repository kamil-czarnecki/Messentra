using Messentra.Features.Explorer.Messages;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Messentra.Features.Explorer.Resources.Components.Details.Tabs;

public sealed partial class FetchMessagesOptionsDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public int DefaultMessageCount { get; set; } = 100;

    private FetchMode _mode = FetchMode.Peek;
    private FetchReceiveMode _receiveMode = FetchReceiveMode.PeekLock;
    private int _messageCount;
    private long? _startSequence;
    private int _waitTimeInSeconds = 10;

    protected override void OnParametersSet()
    {
        _messageCount = DefaultMessageCount;
    }

    private void Cancel() => MudDialog.Cancel();

    private void Submit() =>
        MudDialog.Close(DialogResult.Ok(new FetchMessagesOptions(_mode, _receiveMode, _messageCount, _startSequence, TimeSpan.FromSeconds(_waitTimeInSeconds))));
}
