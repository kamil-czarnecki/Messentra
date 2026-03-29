using System.Text.Json;
using System.Security.Cryptography;
using Messentra.Domain;
using Messentra.Features.Jobs.Stages.ImportMessages;
using Messentra.Infrastructure;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using DatabaseJsonSerializerOptions = Messentra.Infrastructure.Database.JsonSerializerOptions;

namespace Messentra.Features.Jobs.Stages.ImportMessagesFromJson;

public interface IHasImportMessagesFile
{
    ImportMessagesFile GetImportMessagesFilePath();
}

public sealed record ImportMessagesFile(string Path, string Sha256);

public sealed class PrepareMessagesFromJsonStage<TJob> : IStage<TJob>
    where TJob : Job, IHasImportMessagesFile
{
    private const string Stage = "Preparing messages";
    private const int SaveBatchSize = 250;

    private readonly MessentraDbContext _dbContext;
    private readonly IFileSystem _fileSystem;

    public PrepareMessagesFromJsonStage(MessentraDbContext dbContext, IFileSystem fileSystem)
    {
        _dbContext = dbContext;
        _fileSystem = fileSystem;
    }

    public async Task Run(TJob job, CancellationToken ct)
    {
        var currentProgress = job.StageProgress.Stage == Stage ? job.StageProgress.Progress : 0;
        job.UpdateProgress(Stage, currentProgress);

        var source = job.GetImportMessagesFilePath();
        if (!_fileSystem.FileExists(source.Path))
            throw new FileNotFoundException($"Import file '{source.Path}' was not found.", source.Path);

        var currentHash = await CalculateSha256(source.Path, ct);
        if (!string.Equals(currentHash, source.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Import file hash does not match the queued job source.");

        var lastPersistedPosition = await _dbContext.Set<ImportedMessage>()
            .Where(x => x.JobId == job.Id)
            .Select(x => (long?)x.Position)
            .MaxAsync(ct) ?? 0;

        await using var stream = _fileSystem.OpenRead(source.Path);
        var fileLength = stream.CanSeek ? stream.Length : 0;
        var persisted = new List<ImportedMessage>(SaveBatchSize);
        var messagePosition = 0L;
        var lastProgress = -1;

        await foreach (var dto in JsonSerializer.DeserializeAsyncEnumerable<ServiceBusMessageDto>(
                           stream,
                           DatabaseJsonSerializerOptions.Default,
                           cancellationToken: ct))
        {
            ct.ThrowIfCancellationRequested();

            if (dto is null)
                throw new JsonException("Invalid import message payload.");

            messagePosition++;

            if (messagePosition <= lastPersistedPosition)
                continue;

            persisted.Add(new ImportedMessage
            {
                JobId = job.Id,
                Position = messagePosition,
                Message = dto,
                IsSent = false,
                CreatedOn = DateTime.UtcNow
            });

            if (persisted.Count < SaveBatchSize)
                continue;

            await FlushPersisted(persisted, ct);
            UpdateProgress(job, ref lastProgress, stream, fileLength);
        }

        if (persisted.Count > 0)
        {
            await FlushPersisted(persisted, ct);
        }

        if (messagePosition < lastPersistedPosition)
            throw new InvalidOperationException("Import source has fewer messages than already persisted state.");

        job.UpdateProgress(Stage, 100);
    }

    private async Task<string> CalculateSha256(string path, CancellationToken ct)
    {
        await using var stream = _fileSystem.OpenRead(path);
        var hashBytes = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(hashBytes);
    }

    private async Task FlushPersisted(List<ImportedMessage> persisted, CancellationToken ct)
    {
        if (persisted.Count == 0)
            return;

        await _dbContext.Set<ImportedMessage>().AddRangeAsync(persisted, ct);
        await _dbContext.SaveChangesAsync(ct);
        persisted.Clear();
    }

    private static void UpdateProgress(TJob job, ref int lastProgress, Stream stream, long fileLength)
    {
        if (!stream.CanSeek || fileLength <= 0)
            return;

        var progress = (int)Math.Clamp(stream.Position * 100 / fileLength, 0, 99);
        if (progress == lastProgress)
            return;

        job.UpdateProgress(Stage, progress);
        lastProgress = progress;
    }
}




