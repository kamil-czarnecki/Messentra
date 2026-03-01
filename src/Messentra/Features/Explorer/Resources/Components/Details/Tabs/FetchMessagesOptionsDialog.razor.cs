using Messentra.Features.Explorer.Messages;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Messentra.Features.Explorer.Resources.Components.Details.Tabs;

public partial class FetchMessagesOptionsDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;
    
    private FetchMode _mode = FetchMode.Peek;
    private FetchReceiveMode _receiveMode = FetchReceiveMode.PeekLock;
    private int _messageCount = 100;
    private long? _startSequence;
    private int _waitTimeInSeconds = 10;
    
    private void Cancel() => MudDialog.Cancel();

    private void Submit() =>
        MudDialog.Close(DialogResult.Ok(new FetchMessagesOptions(_mode, _receiveMode, _messageCount, _startSequence, TimeSpan.FromSeconds(_waitTimeInSeconds))));
}