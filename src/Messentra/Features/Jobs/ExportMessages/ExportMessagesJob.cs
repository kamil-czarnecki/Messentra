using Messentra.Domain;
using Messentra.Features.Jobs.Stages;

namespace Messentra.Features.Jobs.ExportMessages;

public sealed class ExportMessagesJob : TypedJob<ExportMessagesJobRequest, ExportMessagesJobResponse>,
    IHasMessageFetchConfiguration, IStageCompletionHandler<CreateJsonStageResult>
{
    public override IReadOnlyList<Type> Stages { get; } =
    [
        typeof(FetchMessagesStage<ExportMessagesJob>),
        typeof(CreateJsonStage<ExportMessagesJob>)
    ];

    public MessageFetchConfiguration GetMessageFetchConfiguration()
    {
        ArgumentNullException.ThrowIfNull(Input);
        
        return new MessageFetchConfiguration(Input.ConnectionConfig, Input.Target);
    }

    public void StageCompleted(CreateJsonStageResult result)
    {
        Output = new ExportMessagesJobResponse(result.FilePath);
    }
}

public sealed record ExportMessagesJobRequest(
    ConnectionConfig ConnectionConfig,
    ResourceTarget Target);

public sealed record ExportMessagesJobResponse(string PathToJson);