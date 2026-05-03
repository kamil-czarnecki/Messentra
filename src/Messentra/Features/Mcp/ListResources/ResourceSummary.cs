using Messentra.Infrastructure.AzureServiceBus;

namespace Messentra.Features.Mcp.ListResources;

public sealed record ResourceSummary(
    string Name,
    string Type,
    string? TopicName,
    string Status,
    long Active,
    long DeadLetter,
    long Scheduled,
    long Transfer,
    long TransferDeadLetter,
    int MaxDeliveryCount,
    bool DeadLetteringOnMessageExpiration,
    string? ForwardDeadLetteredMessagesTo)
{
    public static ResourceSummary From(Resource resource) => resource switch
    {
        Resource.Queue q => new ResourceSummary(
            q.Name, "queue", null,
            q.Overview.Status,
            q.Overview.MessageInfo.Active,
            q.Overview.MessageInfo.DeadLetter,
            q.Overview.MessageInfo.Scheduled,
            q.Overview.MessageInfo.Transfer,
            q.Overview.MessageInfo.TransferDeadLetter,
            q.Properties.MaxDeliveryCount,
            q.Properties.DeadLetteringOnMessageExpiration,
            q.Properties.ForwardDeadLetteredMessagesTo),
        Resource.Subscription s => new ResourceSummary(
            s.Name, "subscription", s.TopicName,
            s.Overview.Status,
            s.Overview.MessageInfo.Active,
            s.Overview.MessageInfo.DeadLetter,
            s.Overview.MessageInfo.Scheduled,
            s.Overview.MessageInfo.Transfer,
            s.Overview.MessageInfo.TransferDeadLetter,
            s.Properties.MaxDeliveryCount,
            s.Properties.DeadLetteringOnMessageExpiration,
            s.Properties.ForwardDeadLetteredMessagesTo),
        _ => throw new ArgumentOutOfRangeException(nameof(resource), resource, "Unsupported resource type")
    };
}
