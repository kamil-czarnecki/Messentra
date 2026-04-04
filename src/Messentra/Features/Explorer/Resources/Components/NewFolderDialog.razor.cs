using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Messentra.Features.Explorer.Resources.Components;

public sealed partial class NewFolderDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public string InitialName { get; set; } = string.Empty;

    private string _folderName = string.Empty;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _folderName = InitialName;
    }

    private void Cancel() => MudDialog.Cancel();

    private void Confirm()
    {
        if (string.IsNullOrWhiteSpace(_folderName))
            return;

        MudDialog.Close(DialogResult.Ok(_folderName.Trim()));
    }
}
