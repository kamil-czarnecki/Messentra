using Messentra.Domain;
using Messentra.Features.Jobs.Stages;
using Messentra.Features.Jobs.Stages.ImportMessagesFromJson;
using Messentra.Features.Jobs.Stages.SendImportedMessages;

namespace Messentra.Features.Jobs.ImportMessages;

public sealed class ImportMessagesJob : TypedJob<ImportMessagesJobRequest, ImportMessagesJobResponse>,
    IHasImportMessagesFile,
    IHasMessageImportSendConfiguration,
    IStageCompletionHandler<SendImportedMessagesStageResult>
{
    public override IReadOnlyList<Type> Stages { get; } =
    [
        typeof(PrepareMessagesFromJsonStage<ImportMessagesJob>),
        typeof(SendImportedMessagesStage<ImportMessagesJob>)
    ];

    public ImportMessagesFile GetImportMessagesFilePath()
    {
        ArgumentNullException.ThrowIfNull(Input);
        return new ImportMessagesFile(Input.SourceFilePath, Input.SourceFileHash);
    }

    public MessageImportSendConfiguration GetMessageImportSendConfiguration()
    {
        ArgumentNullException.ThrowIfNull(Input);
        return new MessageImportSendConfiguration(Input.ConnectionConfig, Input.Target, Input.GenerateNewMessageId);
    }

    public void StageCompleted(SendImportedMessagesStageResult result)
    {
        Output = new ImportMessagesJobResponse(result.SentMessagesCount);
    }
}

public sealed record ImportMessagesJobRequest(
    ConnectionConfig ConnectionConfig,
    ResourceTarget Target,
    string SourceFilePath,
    string SourceFileHash,
    bool GenerateNewMessageId = false);

public sealed record ImportMessagesJobResponse(long SentMessagesCount);
