using Messentra.Domain;
using Messentra.Features.Explorer.Messages;
using Messentra.Infrastructure.AzureServiceBus.Queues;
using Messentra.Infrastructure.AzureServiceBus.Subscriptions;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Jobs.Stages.FetchMessages;

public sealed class FetchMessagesStage<TJob> : IStage<TJob> where TJob : Job, IHasMessageFetchConfiguration
{
    private const string Stage = "Fetching messages";
    private const int BatchSize = 1000;
    private readonly MessentraDbContext _dbContext;
    private readonly IAzureServiceBusQueueMessagesProvider _queueMessagesProvider;
    private readonly IAzureServiceBusSubscriptionMessagesProvider _subscriptionMessagesProvider;

    public FetchMessagesStage(
        MessentraDbContext dbContext,
        IAzureServiceBusQueueMessagesProvider queueMessagesProvider,
        IAzureServiceBusSubscriptionMessagesProvider subscriptionMessagesProvider)
    {
        _dbContext = dbContext;
        _queueMessagesProvider = queueMessagesProvider;
        _subscriptionMessagesProvider = subscriptionMessagesProvider;
    }

    public async Task Run(TJob job, CancellationToken ct)
    {
        var currentProgress = job.StageProgress.Stage == Stage
            ? job.StageProgress.Progress
            : 0;
        job.UpdateProgress(Stage, currentProgress);
        
        var config = job.GetMessageFetchConfiguration();
        var connectionInfo = config.ConnectionConfig.ToConnectionInfo();
        var lastPersistedSequence = await GetLastPersistedSequence(job.Id, ct);
        var totalCount = await GetTotalCount(job.Id, ct);
        var nextSequence = lastPersistedSequence + 1;

        while (!ct.IsCancellationRequested)
        {
            var options = new FetchMessagesOptions(
                FetchMode.Peek,
                FetchReceiveMode.PeekLock,
                BatchSize,
                nextSequence,
                TimeSpan.FromSeconds(2),
                config.Target.SubQueue);
            var messages = await FetchMessages(config.Target, connectionInfo, options, ct);

            if (messages.Count == 0)
            {
                job.UpdateProgress(Stage, 100);
                break;
            }

            var dtoMessages = messages
                .Select(x => ServiceBusMessageDto.From(x.Message))
                .ToList();
            var lastSequence = messages.Max(x => x.Message.BrokerProperties.SequenceNumber);

            await AddBatch(job.Id, lastSequence, dtoMessages, ct);
            
            totalCount += messages.Count;
            var progress = config.TotalNumberOfMessagesToFetch <= 0
                ? 100
                : (int)Math.Min(100, totalCount * 100 / config.TotalNumberOfMessagesToFetch);
            job.UpdateProgress(Stage, progress);
            nextSequence = lastSequence + 1;

            if (messages.Count >= BatchSize)
                continue;
            
            job.UpdateProgress(Stage, 100);
            break;
        }
    }

    private async Task<long?> GetLastPersistedSequence(long jobId, CancellationToken ct) =>
        await _dbContext.Set<FetchedMessagesBatch>()
            .Where(x => x.JobId == jobId)
            .Select(x => (long?)x.LastSequence)
            .OrderByDescending(x => x)
            .FirstOrDefaultAsync(ct);

    private async Task<long> GetTotalCount(long jobId, CancellationToken ct) =>
        await _dbContext.Set<FetchedMessagesBatch>()
            .Where(x => x.JobId == jobId)
            .Select(x => x.MessagesCount)
            .SumAsync(ct);
    
    private async Task<IReadOnlyCollection<ServiceBusMessage>> FetchMessages(
        ResourceTarget target,
        Infrastructure.AzureServiceBus.ConnectionInfo connectionInfo,
        FetchMessagesOptions options,
        CancellationToken ct)
    {
        return target switch
        {
            ResourceTarget.Queue queue => await _queueMessagesProvider.Get(
                connectionInfo,
                queue.QueueName,
                options,
                ct),

            ResourceTarget.TopicSubscription topicSubscription => await _subscriptionMessagesProvider.Get(
                connectionInfo,
                topicSubscription.TopicName,
                topicSubscription.SubscriptionName,
                options,
                ct),

            _ => throw new InvalidOperationException($"Unsupported resource target: {target.GetType().Name}")
        };
    }

    private async Task AddBatch(
        long jobId,
        long lastSequence,
        IReadOnlyCollection<ServiceBusMessageDto> messages,
        CancellationToken ct)
    {
        var batch = new FetchedMessagesBatch
        {
            JobId = jobId,
            Messages = messages,
            MessagesCount = messages.Count,
            LastSequence = lastSequence,
            CreatedOn = DateTime.UtcNow
        };
        await _dbContext.Set<FetchedMessagesBatch>().AddAsync(batch, ct);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.Entry(batch).State = EntityState.Detached;
    }
}

public interface IHasMessageFetchConfiguration
{
    MessageFetchConfiguration GetMessageFetchConfiguration();
}

public sealed record MessageFetchConfiguration(
    ConnectionConfig ConnectionConfig,
    ResourceTarget Target,
    long TotalNumberOfMessagesToFetch);
