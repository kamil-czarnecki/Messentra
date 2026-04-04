using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Explorer.Folders.GetFoldersByConnectionId;

public sealed class GetFoldersByConnectionIdQueryHandler
    : IQueryHandler<GetFoldersByConnectionIdQuery, IReadOnlyCollection<FolderDto>>
{
    private readonly MessentraDbContext _dbContext;

    public GetFoldersByConnectionIdQueryHandler(MessentraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<IReadOnlyCollection<FolderDto>> Handle(
        GetFoldersByConnectionIdQuery query, CancellationToken cancellationToken)
    {
        var rows = await _dbContext.Set<Folder>()
            .Where(f => f.ConnectionId == query.ConnectionId)
            .Select(f => new { f.Id, f.Name, ResourceUrls = f.Resources.Select(r => r.ResourceUrl) })
            .ToListAsync(cancellationToken);

        return rows
            .Select(f => new FolderDto(f.Id, f.Name, f.ResourceUrls.ToHashSet()))
            .ToList();
    }
}