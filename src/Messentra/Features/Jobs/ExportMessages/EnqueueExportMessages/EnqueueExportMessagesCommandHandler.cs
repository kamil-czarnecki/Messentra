using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.Database;

namespace Messentra.Features.Jobs.ExportMessages.EnqueueExportMessages;

public sealed class EnqueueExportMessagesCommandHandler : ICommandHandler<EnqueueExportMessagesCommand>
{
    private readonly MessentraDbContext _dbContext;
    private readonly IBackgroundJobQueue _backgroundJobQueue;

    public EnqueueExportMessagesCommandHandler(MessentraDbContext dbContext, IBackgroundJobQueue backgroundJobQueue)
    {
        _dbContext = dbContext;
        _backgroundJobQueue = backgroundJobQueue;
    }

    public async ValueTask<Unit> Handle(EnqueueExportMessagesCommand command, CancellationToken cancellationToken)
    {
        var now  = DateTime.UtcNow;
        var exportJob = new ExportMessagesJob
        {
            Label = $"ExportMessagesJob-{now:yyyyMMddHHmmss}",
            Input = command.Request,
            CreatedAt = now,
            MaxRetries = 3
        };
        
        await _dbContext.Set<Job>().AddAsync(exportJob, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        await _backgroundJobQueue.Enqueue(exportJob.Id, cancellationToken);
        
        return Unit.Value;
    }
}