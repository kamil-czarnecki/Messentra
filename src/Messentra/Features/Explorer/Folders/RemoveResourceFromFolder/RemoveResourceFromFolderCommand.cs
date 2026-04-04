using Mediator;

namespace Messentra.Features.Explorer.Folders.RemoveResourceFromFolder;

public sealed record RemoveResourceFromFolderCommand(long FolderId, string ResourceUrl) : ICommand;
