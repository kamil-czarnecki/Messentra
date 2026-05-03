using OneOf;

namespace Messentra.Features.Mcp.PeekMessages;

[GenerateOneOf]
public partial class PeekMessagesQueryResult : OneOfBase<PeekMessagesResult, McpError>;
