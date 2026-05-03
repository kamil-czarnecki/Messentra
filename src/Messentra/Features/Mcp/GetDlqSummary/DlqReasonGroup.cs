namespace Messentra.Features.Mcp.GetDlqSummary;

public sealed record DlqReasonGroup(
    string? DeadLetterReason,
    string? DeadLetterErrorDescription,
    int Count,
    string SampleBody);
