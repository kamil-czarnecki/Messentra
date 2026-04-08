using Mediator;
using Messentra.Domain;

namespace Messentra.Features.Explorer.Folders.ExportFolders;

public sealed record ExportFoldersCommand(
    long ConnectionId,
    ConnectionConfig ConnectionConfig,
    string DestinationPath) : ICommand;
