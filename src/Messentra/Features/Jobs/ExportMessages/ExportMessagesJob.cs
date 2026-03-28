using Messentra.Domain;
using Messentra.Features.Jobs.Stages;
using Messentra.Features.Jobs.Stages.CreateJsonFromMessages;
using Messentra.Features.Jobs.Stages.FetchMessages;

namespace Messentra.Features.Jobs.ExportMessages;

public sealed class ExportMessagesJob : TypedJob<ExportMessagesJobRequest, ExportMessagesJobResponse>,
    IHasMessageFetchConfiguration, IStageCompletionHandler<CreateJsonStageResult>
{
    public override IReadOnlyList<Type> Stages { get; } =
    [
        typeof(FetchMessagesStage<ExportMessagesJob>),
        typeof(CreateJsonFromMessagesStage<ExportMessagesJob>)
    ];

    public MessageFetchConfiguration GetMessageFetchConfiguration()
    {
        ArgumentNullException.ThrowIfNull(Input);
        
        return new MessageFetchConfiguration(Input.ConnectionConfig, Input.Target, Input.TotalNumberOfMessagesToFetch);
    }

    public void StageCompleted(CreateJsonStageResult result)
    {
        Output = new ExportMessagesJobResponse(result.FilePath);
    }
}

public sealed record ExportMessagesJobRequest(
    ConnectionConfig ConnectionConfig,
    ResourceTarget Target,
    long TotalNumberOfMessagesToFetch);

public sealed record ExportMessagesJobResponse(string PathToJson);