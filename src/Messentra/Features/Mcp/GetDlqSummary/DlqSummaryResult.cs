namespace Messentra.Features.Mcp.GetDlqSummary;

public sealed record DlqSummaryResult(
    int SampledCount,
    long? NextSequenceNumber,
    IReadOnlyList<DlqReasonGroup> Groups);
