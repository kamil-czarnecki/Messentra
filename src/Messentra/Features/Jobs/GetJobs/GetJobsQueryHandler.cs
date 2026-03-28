using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Jobs.GetJobs;

public sealed class GetJobsQueryHandler : IQueryHandler<GetJobsQuery, IReadOnlyList<Job>>
{
    private readonly MessentraDbContext _dbContext;

    public GetJobsQueryHandler(MessentraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<IReadOnlyList<Job>> Handle(GetJobsQuery query, CancellationToken cancellationToken)
    {
        var jobs = await _dbContext.Set<Job>()
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
        
        return jobs;
    }
}