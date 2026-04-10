using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Messentra.Features.Explorer.Messages.ActionProgress;

public sealed partial class ActionProgressDialog : IDisposable
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter, EditorRequired]
    public required string ActionLabel { get; init; }

    [Parameter, EditorRequired]
    public required string ActionIcon { get; init; }

    [Parameter, EditorRequired]
    public required string SubLabel { get; init; }

    [Parameter, EditorRequired]
    public required int TotalCount { get; init; }

    [Parameter, EditorRequired]
    public required Func<IProgress<ActionProgressUpdate>, CancellationToken, Task> OnRunAction { get; init; }

    private enum DialogState { Running, ConfirmingCancel, Completed, Cancelled }

    private DialogState _state = DialogState.Running;
    private int _succeeded;
    private int _failed;
    private int _pending;
    private readonly ConcurrentQueue<(string Id, string Reason)> _failedMessages = new();
    private readonly CancellationTokenSource _cts = new();
    private bool _started;

    private double ProgressPercent =>
        TotalCount == 0 ? 100 : (double)(_succeeded + _failed) / TotalCount * 100;

    private bool IsRunning => _state is DialogState.Running or DialogState.ConfirmingCancel;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _started) return;
        _started = true;

        _pending = TotalCount;

        var progress = new Progress<ActionProgressUpdate>(update =>
        {
            if (update.Succeeded + update.Failed < _succeeded + _failed) return;

            _succeeded = update.Succeeded;
            _failed = update.Failed;
            _pending = update.Pending;

            if (update.FailedMessageId is not null && _failedMessages.Count < 50)
                _failedMessages.Enqueue((update.FailedMessageId, update.FailedReason ?? "Unknown error"));

            InvokeAsync(StateHasChanged);
        });

        try
        {
            await OnRunAction(progress, _cts.Token);
        }
        finally
        {
            _state = _cts.IsCancellationRequested ? DialogState.Cancelled : DialogState.Completed;
            _pending = 0;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void OnCancelClicked() => _state = DialogState.ConfirmingCancel;

    private void OnKeepGoingClicked() => _state = DialogState.Running;

    private void OnConfirmCancelClicked()
    {
        _cts.Cancel();
        _state = DialogState.Running;
    }

    private void OnOkClicked() => MudDialog.Close();

    public void Dispose() => _cts.Dispose();
}
