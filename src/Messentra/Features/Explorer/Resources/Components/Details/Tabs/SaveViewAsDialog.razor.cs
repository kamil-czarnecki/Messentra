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

    private bool CanSave => !string.IsNullOrWhiteSpace(_name);

    private void Save()
    {
        var finalName = _name.Trim();
        
        if (ExistingNames.Contains(finalName, StringComparer.OrdinalIgnoreCase))
        {
            var suffix = 2;
            while (ExistingNames.Contains($"{finalName} ({suffix})", StringComparer.OrdinalIgnoreCase))
                suffix++;
            finalName = $"{finalName} ({suffix})";
        }
        
        MudDialog.Close(DialogResult.Ok(finalName));
    }

    private void Cancel() => MudDialog.Cancel();
}
