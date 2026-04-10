using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Jobs.ExportMessages.CreateExportMessagesJob;

public sealed class CreateExportMessagesJobCommandHandler : ICommandHandler<CreateExportMessagesJobCommand, JobListItem>
{
    private readonly IDbContextFactory<MessentraDbContext> _dbContextFactory;
    private readonly ILogger<CreateExportMessagesJobCommandHandler> _logger;

    public CreateExportMessagesJobCommandHandler(
        IDbContextFactory<MessentraDbContext> dbContextFactory,
        ILogger<CreateExportMessagesJobCommandHandler> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async ValueTask<JobListItem> Handle(CreateExportMessagesJobCommand command, CancellationToken cancellationToken)
    {
        if (command.Request.TotalNumberOfMessagesToFetch <= 0)
        {
            _logger.LogWarning(
                "Cannot create export job: non-positive requested message count. RequestedTotalNumberOfMessagesToFetch: {RequestedTotalNumberOfMessagesToFetch}, Target: {Target}",
                command.Request.TotalNumberOfMessagesToFetch,
                command.Request.Target);
            throw new InvalidOperationException("Cannot create export job with non-positive message count.");
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var exportJob = new ExportMessagesJob
        {
            Label = GetLabel(command),
            Input = command.Request,
            CreatedAt = DateTime.UtcNow,
            MaxRetries = 3
        };

        await dbContext.Set<Job>().AddAsync(exportJob, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return JobListItem.FromJob(exportJob);
    }

    private static string GetLabel(CreateExportMessagesJobCommand command)
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