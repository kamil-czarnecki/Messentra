using System.Text.Json;
using Messentra.Domain;
using Messentra.Features.Jobs.Stages.FetchMessages;
using Messentra.Infrastructure;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using JsonSerializerOptions = Messentra.Infrastructure.Database.JsonSerializerOptions;

namespace Messentra.Features.Jobs.Stages.CreateJsonFromMessages;

public sealed record CreateJsonStageResult(string FilePath);

public sealed class CreateJsonFromMessagesStage<TJob> : IStage<TJob, CreateJsonStageResult>
    where TJob : Job, IStageCompletionHandler<CreateJsonStageResult>
{
    private const string Stage = "Creating JSON";
    private readonly MessentraDbContext _dbContext;
    private readonly IFileSystem _fileSystem;

    public CreateJsonFromMessagesStage(MessentraDbContext dbContext, IFileSystem fileSystem)
    {
        _dbContext = dbContext;
        _fileSystem = fileSystem;
    }

    public async Task Run(TJob job, CancellationToken ct = default)
    {
        job.UpdateProgress(Stage, 0);
        
        var root = Path.Combine(
            _fileSystem.GetRootPath(),
            "Jobs",
            job.Id.ToString());
        var path = Path.Combine(root, $"{job.Label}.json");
        
        _fileSystem.CreateDirectory(root);
        await using var stream = _fileSystem.OpenWrite(path, bufferSize: 65536, useAsync: true);
        await using var jsonWriter = new Utf8JsonWriter(stream);
        var baseQuery = _dbContext.Set<FetchedMessagesBatch>()
            .Where(x => x.JobId == job.Id)
            .OrderBy(x => x.Id)
            .AsNoTracking();
        var totalMessages = await baseQuery
            .Select(x => x.MessagesCount)
            .SumAsync(ct);

        jsonWriter.WriteStartArray();

        if (totalMessages == 0)
        {
            jsonWriter.WriteEndArray();
            await jsonWriter.FlushAsync(ct);
            job.UpdateProgress(Stage, 100);
            job.StageCompleted(new CreateJsonStageResult(path));
            
            return;
        }
        
        long processedMessages = 0;
        var lastProgress = -1;
        
        await foreach (var batch in baseQuery.AsAsyncEnumerable().WithCancellation(ct))
        {
            ct.ThrowIfCancellationRequested();

            foreach (var message in batch.Messages)
            {
                JsonSerializer.Serialize(jsonWriter, message, JsonSerializerOptions.Default);
            }

            processedMessages += batch.MessagesCount;
            var progress = (int)Math.Clamp(processedMessages * 100 / totalMessages, 0, 100);

            if (progress != lastProgress)
            {
                job.UpdateProgress(Stage, progress);
                lastProgress = progress;
            }
        }

        if (lastProgress < 100)
            job.UpdateProgress(Stage, 100);

        jsonWriter.WriteEndArray();
        await jsonWriter.FlushAsync(ct);
        job.StageCompleted(new CreateJsonStageResult(path));
    }
}