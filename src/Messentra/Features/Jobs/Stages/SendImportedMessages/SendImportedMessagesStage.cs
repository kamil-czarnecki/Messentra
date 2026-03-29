using Messentra.Domain;
using Messentra.Features.Jobs.Stages.ImportMessages;
using Messentra.Infrastructure.AzureServiceBus;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Jobs.Stages.SendImportedMessages;

public interface IHasMessageImportSendConfiguration
{
    MessageImportSendConfiguration GetMessageImportSendConfiguration();
}

public sealed record MessageImportSendConfiguration(
    ConnectionConfig ConnectionConfig,
    ResourceTarget Target);

public sealed record SendImportedMessagesStageResult(long SentMessagesCount);

public sealed class SendImportedMessagesStage<TJob> : IStage<TJob, SendImportedMessagesStageResult>
    where TJob : Job, IHasMessageImportSendConfiguration, IStageCompletionHandler<SendImportedMessagesStageResult>
{
    private const string Stage = "Sending messages";
    private const int MaxBatchCandidateSize = 200;

    private readonly MessentraDbContext _dbContext;
    private readonly IAzureServiceBusSender _sender;

    public SendImportedMessagesStage(MessentraDbContext dbContext, IAzureServiceBusSender sender)
    {
        _dbContext = dbContext;
        _sender = sender;
    }

    public async Task Run(TJob job, CancellationToken ct)
    {
        var currentProgress = job.StageProgress.Stage == Stage ? job.StageProgress.Progress : 0;
        job.UpdateProgress(Stage, currentProgress);

        var config = job.GetMessageImportSendConfiguration();
        var connectionInfo = config.ConnectionConfig.ToConnectionInfo();
        var entityPath = GetEntityPath(config.Target);
        var totalMessages = await _dbContext.Set<ImportedMessage>()
            .AsNoTracking()
            .Where(x => x.JobId == job.Id)
            .CountAsync(ct);

        if (totalMessages == 0)
        {
            job.UpdateProgress(Stage, 100);
            job.StageCompleted(new SendImportedMessagesStageResult(0));
            return;
        }

        var sentMessages = await _dbContext.Set<ImportedMessage>()
            .AsNoTracking()
            .Where(x => x.JobId == job.Id && x.IsSent)
            .CountAsync(ct);

        var lastProgress = -1;

        while (!ct.IsCancellationRequested)
        {
            var unsentBatch = await _dbContext.Set<ImportedMessage>()
                .AsNoTracking()
                .Where(x => x.JobId == job.Id && !x.IsSent)
                .OrderBy(x => x.Position)
                .Take(MaxBatchCandidateSize)
                .ToListAsync(ct);

            if (unsentBatch.Count == 0)
                break;

            ct.ThrowIfCancellationRequested();
            var sentFromBatch = await _sender.SendBatchChunk(
                connectionInfo,
                entityPath,
                unsentBatch.Select(x => x.Message).ToList(),
                ct);

            if (sentFromBatch > 0)
            {
                var sentIds = unsentBatch
                    .Take(sentFromBatch)
                    .Select(x => x.Id)
                    .ToHashSet();

                await UpdateAsSent(sentIds, ct);
                
                sentMessages += sentFromBatch;
                UpdateProgress(job, totalMessages, sentMessages, ref lastProgress);
                continue;
            }

            var first = unsentBatch[0];
            ct.ThrowIfCancellationRequested();
            await _sender.Send(connectionInfo, entityPath, first.Message, ct);
            await UpdateAsSent([first.Id], ct);
            sentMessages++;
            UpdateProgress(job, totalMessages, sentMessages, ref lastProgress);
        }

        job.UpdateProgress(Stage, 100);
        job.StageCompleted(new SendImportedMessagesStageResult(sentMessages));
    }

    private static string GetEntityPath(ResourceTarget target) =>
        target switch
        {
            ResourceTarget.Queue { SubQueue: Explorer.Messages.SubQueue.Active } queue => queue.QueueName,
            ResourceTarget.TopicSubscription { SubQueue: Explorer.Messages.SubQueue.Active } topicSubscription => topicSubscription.TopicName,
            ResourceTarget.Queue queue => throw new InvalidOperationException($"Import target sub-queue '{queue.SubQueue}' is not supported."),
            ResourceTarget.TopicSubscription topicSubscription => throw new InvalidOperationException($"Import target sub-queue '{topicSubscription.SubQueue}' is not supported."),
            _ => throw new InvalidOperationException($"Unsupported import target type: {target.GetType().Name}")
        };

    private static void UpdateProgress(TJob job, int totalMessages, int sentMessages, ref int lastProgress)
    {
        var progress = Math.Clamp(sentMessages * 100 / totalMessages, 0, 100);
        if (progress == lastProgress)
            return;

        job.UpdateProgress(Stage, progress);
        lastProgress = progress;
    }

    private async Task UpdateAsSent(HashSet<long> ids, CancellationToken ct) =>
        await _dbContext.Set<ImportedMessage>()
            .Where(x => ids.Contains(x.Id))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.IsSent, true)
                    .SetProperty(x => x.SentOn, DateTime.UtcNow),
                ct);
}




