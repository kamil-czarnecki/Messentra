using Messentra.Domain;
using Messentra.Features.Jobs.Stages.FetchMessages;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Jobs.Stages.PersistSelectedMessages;

public sealed class PersistSelectedMessagesStage<TJob> : IStage<TJob>
    where TJob : Job, IHasSelectedMessages
{
    private const string Stage = "Persisting messages";
    private const int BatchSize = 1000;
    private readonly MessentraDbContext _dbContext;

    public PersistSelectedMessagesStage(MessentraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Run(TJob job, CancellationToken ct)
    {
        var currentProgress = job.StageProgress.Stage == Stage
            ? job.StageProgress.Progress
            : 0;
        job.UpdateProgress(Stage, currentProgress);

        var allMessages = job.GetSelectedMessages();
        var total = allMessages.Count;

        if (total == 0)
        {
            job.UpdateProgress(Stage, 100);
            return;
        }

        var alreadySaved = await GetTotalCount(job.Id, ct);

        if (alreadySaved >= total)
        {
            job.UpdateProgress(Stage, 100);
            return;
        }

        var remaining = allMessages.Skip(alreadySaved).ToList();
        var persisted = alreadySaved;
        var lastProgress = -1;

        foreach (var chunk in remaining.Chunk(BatchSize))
        {
            ct.ThrowIfCancellationRequested();

            var lastIndex = persisted + chunk.Length - 1;
            var batch = new FetchedMessagesBatch
            {
                JobId = job.Id,
                Messages = chunk.ToList(),
                MessagesCount = chunk.Length,
                LastSequence = lastIndex,
                CreatedOn = DateTime.UtcNow
            };

            await _dbContext.Set<FetchedMessagesBatch>().AddAsync(batch, ct);
            await _dbContext.SaveChangesAsync(ct);
            _dbContext.Entry(batch).State = EntityState.Detached;

            persisted += chunk.Length;
            var progress = (int)Math.Clamp((long)persisted * 100 / total, 0, 100);
            if (progress != lastProgress)
            {
                job.UpdateProgress(Stage, progress);
                lastProgress = progress;
            }
        }

        if (lastProgress < 100)
            job.UpdateProgress(Stage, 100);
    }

    private async Task<int> GetTotalCount(long jobId, CancellationToken ct) =>
        await _dbContext.Set<FetchedMessagesBatch>()
            .Where(x => x.JobId == jobId)
            .Select(x => x.MessagesCount)
            .SumAsync(ct);
}

public interface IHasSelectedMessages
{
    IReadOnlyList<ServiceBusMessageDto> GetSelectedMessages();
}
