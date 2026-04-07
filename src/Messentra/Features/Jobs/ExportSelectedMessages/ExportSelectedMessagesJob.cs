using Messentra.Domain;
using Messentra.Features.Jobs.Stages;
using Messentra.Features.Jobs.Stages.CreateJsonFromMessages;
using Messentra.Features.Jobs.Stages.PersistSelectedMessages;

namespace Messentra.Features.Jobs.ExportSelectedMessages;

public sealed class ExportSelectedMessagesJob
    : TypedJob<ExportSelectedMessagesJobRequest, ExportSelectedMessagesJobResponse>,
      IHasSelectedMessages,
      IStageCompletionHandler<CreateJsonStageResult>
{
    public override IReadOnlyList<Type> Stages { get; } =
    [
        typeof(PersistSelectedMessagesStage<ExportSelectedMessagesJob>),
        typeof(CreateJsonFromMessagesStage<ExportSelectedMessagesJob>)
    ];

    public IReadOnlyList<ServiceBusMessageDto> GetSelectedMessages()
    {
        ArgumentNullException.ThrowIfNull(Input);
        return Input.Messages;
    }

    public void StageCompleted(CreateJsonStageResult result)
    {
        Output = new ExportSelectedMessagesJobResponse(result.FilePath);
    }
}

public sealed record ExportSelectedMessagesJobRequest(
    IReadOnlyList<ServiceBusMessageDto> Messages,
    string ResourceLabel);

public sealed record ExportSelectedMessagesJobResponse(string PathToJson);
