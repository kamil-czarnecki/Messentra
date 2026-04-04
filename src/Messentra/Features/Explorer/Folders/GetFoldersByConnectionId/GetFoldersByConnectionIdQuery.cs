using Mediator;

namespace Messentra.Features.Explorer.Folders.GetFoldersByConnectionId;

public sealed record GetFoldersByConnectionIdQuery(long ConnectionId) : IQuery<IReadOnlyCollection<FolderDto>>;