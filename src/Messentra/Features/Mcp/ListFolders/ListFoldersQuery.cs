using Mediator;

namespace Messentra.Features.Mcp.ListFolders;

public sealed record ListFoldersQuery(long ConnectionId) : IQuery<IEnumerable<FolderSummary>>;
