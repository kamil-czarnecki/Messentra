namespace Messentra.Features.Mcp.GetDlqSummary;

public sealed record DlqReasonGroup(
    IReadOnlyDictionary<string, string?> GroupKey,
    int Count,
    string SampleBody);
