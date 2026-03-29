using Mediator;
using System.Security.Cryptography;
using Messentra.Domain;
using Messentra.Infrastructure;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Jobs.ImportMessages.EnqueueImportMessages;

public sealed class EnqueueImportMessagesCommandHandler : ICommandHandler<EnqueueImportMessagesCommand>
{
    private readonly IDbContextFactory<MessentraDbContext> _dbContextFactory;
    private readonly IBackgroundJobQueue _backgroundJobQueue;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<EnqueueImportMessagesCommandHandler> _logger;

    public EnqueueImportMessagesCommandHandler(
        IDbContextFactory<MessentraDbContext> dbContextFactory,
        IBackgroundJobQueue backgroundJobQueue,
        IFileSystem fileSystem,
        ILogger<EnqueueImportMessagesCommandHandler> logger)
    {
        _dbContextFactory = dbContextFactory;
        _backgroundJobQueue = backgroundJobQueue;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public async ValueTask<Unit> Handle(EnqueueImportMessagesCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Request.SourceFilePath))
        {
            _logger.LogWarning("Skipping import job enqueue due to missing source file path.");
            return Unit.Value;
        }

        if (!_fileSystem.FileExists(command.Request.SourceFilePath))
        {
            _logger.LogWarning(
                "Skipping import job enqueue because source file does not exist. SourceFilePath: {SourceFilePath}",
                command.Request.SourceFilePath);
            return Unit.Value;
        }

        var sourceFileHash = await CalculateSha256(command.Request.SourceFilePath, cancellationToken);
        var request = command.Request with { SourceFileHash = sourceFileHash };

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var importJob = new ImportMessagesJob
        {
            Label = GetLabel(command),
            Input = request,
            CreatedAt = now,
            MaxRetries = 3
        };

        await dbContext.Set<Job>().AddAsync(importJob, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await _backgroundJobQueue.Enqueue(importJob.Id, cancellationToken);

        return Unit.Value;
    }

    private async Task<string> CalculateSha256(string path, CancellationToken cancellationToken)
    {
        await using var stream = _fileSystem.OpenRead(path);
        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hashBytes);
    }

    private static string GetLabel(EnqueueImportMessagesCommand command)
    {
        var targetResource = command.Request.Target switch
        {
            ResourceTarget.Queue queue => $"{queue.QueueName}-{queue.SubQueue}",
            ResourceTarget.TopicSubscription topicSubscription =>
                $"{topicSubscription.TopicName}-{topicSubscription.SubscriptionName}-{topicSubscription.SubQueue}",
            _ => throw new InvalidOperationException("Unknown resource target type")
        };

        return $"ImportMessagesJob-{targetResource}";
    }
}

