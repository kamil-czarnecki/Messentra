using Messentra.Infrastructure.AzureServiceBus;

namespace Messentra.Features.Mcp.ListResources;

public sealed record ResourceSummary(
    string Name,
    string Type,
    string? TopicName,
    long Active,
    long DeadLetter,
    long Scheduled)
{
    public static ResourceSummary From(Resource resource) => resource switch
    {
        Resource.Queue q => new ResourceSummary(
            q.Name, "queue", null,
            q.Overview.MessageInfo.Active,
            q.Overview.MessageInfo.DeadLetter,
            q.Overview.MessageInfo.Scheduled),
        Resource.Subscription s => new ResourceSummary(
            s.Name, "subscription", s.TopicName,
            s.Overview.MessageInfo.Active,
            s.Overview.MessageInfo.DeadLetter,
            s.Overview.MessageInfo.Scheduled),
        _ => throw new ArgumentOutOfRangeException(nameof(resource), resource, "Unsupported resource type")
    };
}
