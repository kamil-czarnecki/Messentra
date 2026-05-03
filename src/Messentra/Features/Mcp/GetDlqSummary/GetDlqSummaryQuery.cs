using Mediator;
using Messentra.Domain;

namespace Messentra.Features.Mcp.GetDlqSummary;

public sealed record GetDlqSummaryQuery(
    ConnectionConfig Config,
    string ResourceName,
    string? TopicName,
    int SampleSize,
    long? FromSequenceNumber,
    IReadOnlyList<string>? GroupBy = null) : IQuery<GetDlqSummaryQueryResult>;
