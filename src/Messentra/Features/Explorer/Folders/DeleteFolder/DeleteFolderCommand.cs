using Mediator;

namespace Messentra.Features.Explorer.Folders.DeleteFolder;

public sealed record DeleteFolderCommand(long FolderId) : ICommand;
