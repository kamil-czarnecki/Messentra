using Mediator;
using Messentra.Domain;

namespace Messentra.Features.Mcp.GetResource;

public sealed record GetResourceQuery(
    long ConnectionId,
    ConnectionConfig Config,
    string ResourceName,
    string? TopicName) : IQuery<GetResourceResult>;
