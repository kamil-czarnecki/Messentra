using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Jobs.ExportSelectedMessages.EnqueueExportSelectedMessages;

public sealed class EnqueueExportSelectedMessagesCommandHandler : ICommandHandler<EnqueueExportSelectedMessagesCommand>
{
    private readonly IDbContextFactory<MessentraDbContext> _dbContextFactory;
    private readonly IBackgroundJobQueue _backgroundJobQueue;
    private readonly ILogger<EnqueueExportSelectedMessagesCommandHandler> _logger;

    public EnqueueExportSelectedMessagesCommandHandler(
        IDbContextFactory<MessentraDbContext> dbContextFactory,
        IBackgroundJobQueue backgroundJobQueue,
        ILogger<EnqueueExportSelectedMessagesCommandHandler> logger)
    {
        _dbContextFactory = dbContextFactory;
        _backgroundJobQueue = backgroundJobQueue;
        _logger = logger;
    }

    public async ValueTask<Unit> Handle(EnqueueExportSelectedMessagesCommand command, CancellationToken cancellationToken)
    {
        if (command.Request.Messages.Count == 0)
        {
            _logger.LogWarning(
                "Skipping export selected messages job enqueue due to empty message list. ResourceLabel: {ResourceLabel}",
                command.Request.ResourceLabel);
            return Unit.Value;
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var count = command.Request.Messages.Count;
        var exportJob = new ExportSelectedMessagesJob
        {
            Label = $"ExportSelectedMessages-{command.Request.ResourceLabel}-{count}msgs",
            Input = command.Request,
            CreatedAt = now,
            MaxRetries = 3
        };

        await dbContext.Set<Job>().AddAsync(exportJob, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await _backgroundJobQueue.Enqueue(exportJob.Id, cancellationToken);

        return Unit.Value;
    }
}
