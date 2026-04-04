namespace Messentra.Domain;

public class Folder
{
    public long Id { get; init; }
    public long ConnectionId { get; init; }
    public required string Name { get; set; }
    public List<FolderResource> Resources { get; init; } = [];
}

public class FolderResource
{
    public long FolderId { get; init; }
    public required string ResourceUrl { get; set; }
}
