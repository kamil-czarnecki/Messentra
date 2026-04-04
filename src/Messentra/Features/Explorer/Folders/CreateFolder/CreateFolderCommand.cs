using Mediator;

namespace Messentra.Features.Explorer.Folders.CreateFolder;

public sealed record CreateFolderCommand(long ConnectionId, string Name) : ICommand<long>;