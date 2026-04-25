using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Messentra.Features.Explorer.Resources.Components.Details.Tabs;

public partial class SaveViewAsDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public string InitialName { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public IReadOnlyList<string> ExistingNames { get; set; } = [];

    private string _name = string.Empty;

    protected override void OnParametersSet()
        => _name = InitialName;

    private bool IsDuplicate => ExistingNames.Contains(_name.Trim(), StringComparer.OrdinalIgnoreCase);
    private bool CanSave => !string.IsNullOrWhiteSpace(_name) && !IsDuplicate;

    private void Save()
    {
        if (!CanSave) return;
        MudDialog.Close(DialogResult.Ok(_name.Trim()));
    }

    private void Cancel() => MudDialog.Cancel();
}
