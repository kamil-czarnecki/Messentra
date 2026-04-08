using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Messentra.Features.Explorer.Resources.Components;

public sealed partial class ImportFoldersDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    private IBrowserFile? _selectedFile;

    private void Cancel() => MudDialog.Cancel();

    private void Submit()
    {
        if (_selectedFile is null)
            return;

        MudDialog.Close(DialogResult.Ok(_selectedFile));
    }
}
