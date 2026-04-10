using Mediator;
using System.Security.Cryptography;
using Messentra.Domain;
using Messentra.Infrastructure;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Jobs.ImportMessages.CreateImportMessagesJob;

public sealed class CreateImportMessagesJobCommandHandler : ICommandHandler<CreateImportMessagesJobCommand, JobListItem>
{
    private readonly IDbContextFactory<MessentraDbContext> _dbContextFactory;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<CreateImportMessagesJobCommandHandler> _logger;

    public CreateImportMessagesJobCommandHandler(
        IDbContextFactory<MessentraDbContext> dbContextFactory,
        IFileSystem fileSystem,
        ILogger<CreateImportMessagesJobCommandHandler> logger)
    {
        _dbContextFactory = dbContextFactory;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public async ValueTask<JobListItem> Handle(CreateImportMessagesJobCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Request.SourceFilePath))
        {
            _logger.LogWarning("Cannot create import job: missing source file path.");
            throw new InvalidOperationException("Cannot create import job: missing source file path.");
        }

        if (!_fileSystem.FileExists(command.Request.SourceFilePath))
        {
            _logger.LogWarning(
                "Cannot create import job: source file does not exist. SourceFilePath: {SourceFilePath}",
                command.Request.SourceFilePath);
            throw new InvalidOperationException($"Cannot create import job: source file does not exist at '{command.Request.SourceFilePath}'.");
        }

        var sourceFileHash = await CalculateSha256(command.Request.SourceFilePath, cancellationToken);
        var request = command.Request with { SourceFileHash = sourceFileHash };

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var importJob = new ImportMessagesJob
        {
            Label = GetLabel(command),
            Input = request,
            CreatedAt = DateTime.UtcNow,
            MaxRetries = 3
        };

        await dbContext.Set<Job>().AddAsync(importJob, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return JobListItem.FromJob(importJob);
    }

    private async Task<string> CalculateSha256(string path, CancellationToken cancellationToken)
    {
        await using var stream = _fileSystem.OpenRead(path);
        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hashBytes);
    }

    private static string GetLabel(CreateImportMessagesJobCommand command)
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
