using OneOf;

namespace Messentra.Features.Mcp.GetDlqSummary;

[GenerateOneOf]
public partial class GetDlqSummaryQueryResult : OneOfBase<DlqSummaryResult, McpError>;
