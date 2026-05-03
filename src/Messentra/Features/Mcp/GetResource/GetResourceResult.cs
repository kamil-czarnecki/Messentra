using Messentra.Features.Mcp.ListResources;
using OneOf;

namespace Messentra.Features.Mcp.GetResource;

[GenerateOneOf]
public partial class GetResourceResult : OneOfBase<ResourceSummary, McpError>;
