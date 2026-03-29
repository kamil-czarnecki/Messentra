using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Jobs.ExportMessages.EnqueueExportMessages;

public sealed class EnqueueExportMessagesCommandHandler : ICommandHandler<EnqueueExportMessagesCommand>
{
    private readonly IDbContextFactory<MessentraDbContext> _dbContextFactory;
    private readonly IBackgroundJobQueue _backgroundJobQueue;
    private readonly ILogger<EnqueueExportMessagesCommandHandler> _logger;

    public EnqueueExportMessagesCommandHandler(
        IDbContextFactory<MessentraDbContext> dbContextFactory,
        IBackgroundJobQueue backgroundJobQueue,
        ILogger<EnqueueExportMessagesCommandHandler> logger)
    {
        _dbContextFactory = dbContextFactory;
        _backgroundJobQueue = backgroundJobQueue;
        _logger = logger;
    }

    public async ValueTask<Unit> Handle(EnqueueExportMessagesCommand command, CancellationToken cancellationToken)
    {
        if (command.Request.TotalNumberOfMessagesToFetch <= 0)
        {
            _logger.LogWarning(
                "Skipping export job enqueue due to non-positive requested message count. RequestedTotalNumberOfMessagesToFetch: {RequestedTotalNumberOfMessagesToFetch}, Target: {Target}",
                command.Request.TotalNumberOfMessagesToFetch,
                command.Request.Target);
            return Unit.Value;
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var now  = DateTime.UtcNow;
        var exportJob = new ExportMessagesJob
        {
            Label = GetLabel(command),
            Input = command.Request,
            CreatedAt = now,
            MaxRetries = 3
        };
        
        await dbContext.Set<Job>().AddAsync(exportJob, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        await _backgroundJobQueue.Enqueue(exportJob.Id, cancellationToken);
        
        return Unit.Value;
    }
    
    private static string GetLabel(EnqueueExportMessagesCommand command)
    {
        var targetResource = command.Request.Target switch
        {
            ResourceTarget.Queue queue => $"{queue.QueueName}-{queue.SubQueue}",
            ResourceTarget.TopicSubscription topicSubscription =>
                $"{topicSubscription.TopicName}-{topicSubscription.SubscriptionName}-{topicSubscription.SubQueue}",
            _ => throw new InvalidOperationException("Unknown resource target type")
        };
        
        return $"ExportMessagesJob-{targetResource}";
    }
}