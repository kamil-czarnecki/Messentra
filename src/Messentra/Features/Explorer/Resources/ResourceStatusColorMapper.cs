using MudBlazor;

namespace Messentra.Features.Explorer.Resources;

internal static class ResourceStatusColorMapper
{
    public static Color GetStatusColor(string? status) =>
        status switch
        {
            "Active" => Color.Success,
            "Disabled" => Color.Error,
            "SendDisabled" => Color.Warning,
            "ReceiveDisabled" => Color.Warning,
            _ => Color.Default
        };
}
