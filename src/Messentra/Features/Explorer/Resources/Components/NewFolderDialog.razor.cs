using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Messentra.Features.Explorer.Resources.Components;

public sealed partial class NewFolderDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public string InitialName { get; set; } = string.Empty;

    [Parameter]
    public IReadOnlyCollection<string> ExistingFolderNames { get; set; } = [];

    private string _folderName = string.Empty;

    private bool HasDuplicateName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_folderName))
                return false;

            var normalized = _folderName.Trim();
            var normalizedInitial = InitialName.Trim();

            return ExistingFolderNames.Any(name =>
            {
                var candidate = name.Trim();
                if (string.IsNullOrEmpty(candidate))
                    return false;

                if (!string.Equals(candidate, normalized, StringComparison.OrdinalIgnoreCase))
                    return false;

                return string.IsNullOrWhiteSpace(normalizedInitial) ||
                       !string.Equals(candidate, normalizedInitial, StringComparison.OrdinalIgnoreCase);
            });
        }
    }

    private bool IsConfirmDisabled => string.IsNullOrWhiteSpace(_folderName) || HasDuplicateName;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _folderName = InitialName;
    }

    private void Cancel() => MudDialog.Cancel();

    private void Confirm()
    {
        if (IsConfirmDisabled)
            return;

        MudDialog.Close(DialogResult.Ok(_folderName.Trim()));
    }
}
