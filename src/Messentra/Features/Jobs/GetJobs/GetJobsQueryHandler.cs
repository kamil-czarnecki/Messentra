using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Jobs.GetJobs;

public sealed class GetJobsQueryHandler : IQueryHandler<GetJobsQuery, IReadOnlyList<Job>>
{
    private readonly IDbContextFactory<MessentraDbContext> _dbContextFactory;

    public GetJobsQueryHandler(IDbContextFactory<MessentraDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async ValueTask<IReadOnlyList<Job>> Handle(GetJobsQuery query, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var jobs = await dbContext.Set<Job>()
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
        
        return jobs;
    }
}