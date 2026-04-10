using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Jobs.ExportSelectedMessages.CreateExportSelectedMessagesJob;

public sealed class CreateExportSelectedMessagesJobCommandHandler : ICommandHandler<CreateExportSelectedMessagesJobCommand, JobListItem>
{
    private readonly IDbContextFactory<MessentraDbContext> _dbContextFactory;
    private readonly ILogger<CreateExportSelectedMessagesJobCommandHandler> _logger;

    public CreateExportSelectedMessagesJobCommandHandler(
        IDbContextFactory<MessentraDbContext> dbContextFactory,
        ILogger<CreateExportSelectedMessagesJobCommandHandler> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async ValueTask<JobListItem> Handle(CreateExportSelectedMessagesJobCommand command, CancellationToken cancellationToken)
    {
        if (command.Request.Messages.Count == 0)
        {
            _logger.LogWarning(
                "Cannot create export selected messages job: empty message list. ResourceLabel: {ResourceLabel}",
                command.Request.ResourceLabel);
            throw new InvalidOperationException("Cannot create export selected messages job with empty message list.");
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var count = command.Request.Messages.Count;
        var exportJob = new ExportSelectedMessagesJob
        {
            Label = $"ExportSelectedMessages-{command.Request.ResourceLabel}-{count}msgs",
            Input = command.Request,
            CreatedAt = DateTime.UtcNow,
            MaxRetries = 3
        };

        await dbContext.Set<Job>().AddAsync(exportJob, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return JobListItem.FromJob(exportJob);
    }
}
