using MudBlazor;

namespace Messentra.Features.Layout.Components;

public partial class SideBar
{
    private readonly IReadOnlyCollection<Link> _links = new List<Link>
    {
        new("Explorer", "/explorer", Icons.Material.Filled.Layers),
        new("Jobs", "/jobs", Icons.Material.Filled.Sync),
        new("Options", "/options", Icons.Material.Filled.Cable)
    };
    
    private record Link(string Title, string Href, string Icon, bool IsDisabled = false);
}