namespace Messentra.Features.Explorer.Folders;

public sealed record FolderDto(long Id, string Name, IReadOnlySet<string> ResourceUrls);