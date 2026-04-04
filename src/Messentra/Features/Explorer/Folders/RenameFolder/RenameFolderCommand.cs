using Mediator;

namespace Messentra.Features.Explorer.Folders.RenameFolder;

public sealed record RenameFolderCommand(long FolderId, string NewName) : ICommand;