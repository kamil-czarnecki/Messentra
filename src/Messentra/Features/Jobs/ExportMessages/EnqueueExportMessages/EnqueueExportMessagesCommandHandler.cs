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
            Label = GetLabel(command),
            Input = command.Request,
            CreatedAt = now,
            MaxRetries = 3
        };
        
        await _dbContext.Set<Job>().AddAsync(exportJob, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        
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