using System.Text.Json.Serialization;

namespace Messentra.Features.Explorer.Folders;

public sealed record FolderExportItem(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("resources")] IReadOnlyList<string> Resources);
