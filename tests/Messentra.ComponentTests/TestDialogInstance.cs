using MudBlazor;

namespace Messentra.ComponentTests;
//
// public sealed class TestDialogInstance : IMudDialogInstance
// {
//     public Guid Id { get; } = null!
//     public string ElementId { get;  private set; }
//     public DialogOptions Options { get; private set; }
//     public string? Title { get; private set; }
//     public DialogResult ClosedResult { get; private set; }
//
//     public Task SetOptionsAsync(DialogOptions options)
//     {
//         Options = options;
//         
//         return Task.CompletedTask;
//     }
//
//     public Task SetTitleAsync(string? title)
//     {
//         Title = title;
//         
//         return Task.CompletedTask;
//     }
//
//     public void Close()
//     {
//     }
//
//     public void Close(DialogResult result)
//     {
//         ClosedResult = result;
//     }
//
//     public void Close<T>(T returnValue)
//     {
//         ClosedResult = DialogResult.Ok(returnValue);
//     }
//
//     public void Cancel() => Close(DialogResult.Cancel());
//     
//     public void CancelAll()
//     {
//         Close(DialogResult.Cancel);
//     }
//
//     public void StateHasChanged()
//     {
//     }
// }