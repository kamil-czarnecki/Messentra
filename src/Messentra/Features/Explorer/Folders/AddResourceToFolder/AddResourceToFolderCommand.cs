using Mediator;

namespace Messentra.Features.Explorer.Folders.AddResourceToFolder;

public sealed record AddResourceToFolderCommand(long FolderId, string ResourceUrl) : ICommand;
