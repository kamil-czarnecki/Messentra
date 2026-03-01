using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Settings.Connections.GetConnections;

public sealed class GetConnectionsQueryHandler : IQueryHandler<GetConnectionsQuery, IReadOnlyCollection<ConnectionDto>>
{
    private readonly MessentraDbContext _dbContext;

    public GetConnectionsQueryHandler(MessentraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<IReadOnlyCollection<ConnectionDto>> Handle(
        GetConnectionsQuery query,
        CancellationToken cancellationToken) =>
        await _dbContext
            .Set<Connection>()
            .AsNoTracking()
            .Select(x => ConnectionDto.From(x))
            .ToListAsync(cancellationToken);
}