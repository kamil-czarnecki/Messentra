using Messentra.Domain;

namespace Messentra.Features.Jobs.ImportMessages;

public sealed class ImportMessagesJob : TypedJob<ImportMessagesJobRequest, ImportMessagesJobResponse>
{
    public override IReadOnlyList<Type> Stages { get; } = [];
}

public sealed record ImportMessagesJobRequest(
    ConnectionConfig ConnectionConfig,
    ResourceTarget Target);
public sealed record ImportMessagesJobResponse;