using Mediator;
using Messentra.Domain;

namespace Messentra.Features.Explorer.Folders.ImportFolders;

public sealed record ImportFoldersCommand(
    long ConnectionId,
    ConnectionConfig ConnectionConfig,
    string JsonContent) : ICommand;
