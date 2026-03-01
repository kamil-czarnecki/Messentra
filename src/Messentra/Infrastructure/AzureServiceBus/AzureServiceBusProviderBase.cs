using Azure.Messaging.ServiceBus;
using Messentra.Features.Explorer.Messages;
using Messentra.Infrastructure.AzureServiceBus.Factories;
using ServiceBusMessage = Messentra.Features.Explorer.Messages.ServiceBusMessage;

namespace Messentra.Infrastructure.AzureServiceBus;

public abstract class AzureServiceBusProviderBase(IAzureServiceBusClientFactory clientFactory)
{
    protected ServiceBusClient GetClient(ConnectionInfo info) =>
        info switch
        {
            ConnectionInfo.ConnectionString cs => clientFactory.CreateClient(cs.Value),
            ConnectionInfo.ManagedIdentity mi => clientFactory.CreateClient(
                mi.FullyQualifiedNamespace,
                mi.TenantId,
                mi.ClientId),
            _ => throw new InvalidOperationException("Invalid connection info type")
        };
    
    protected static ServiceBusReceiveMode GetReceiveMode(FetchMessagesOptions options) =>
        options switch
        {
            { Mode: FetchMode.Peek } => ServiceBusReceiveMode.PeekLock,
            { Mode: FetchMode.Receive, ReceiveMode: FetchReceiveMode.PeekLock } => ServiceBusReceiveMode.PeekLock,
            { Mode: FetchMode.Receive, ReceiveMode: FetchReceiveMode.ReceiveAndDelete } => ServiceBusReceiveMode
                .ReceiveAndDelete,
            _ => throw new InvalidOperationException("Invalid fetch messages mode")
        };

    protected static ServiceBusMessage Map(
        ServiceBusReceiver receiver,
        ServiceBusSender sender,
        ServiceBusReceivedMessage message)
    {
        var brokerProperties = new BrokerProperties(
            message.MessageId,
            message.SequenceNumber,
            message.CorrelationId,
            message.SessionId,
            message.ReplyToSessionId,
            message.EnqueuedTime.UtcDateTime,
            message.ScheduledEnqueueTime.UtcDateTime,
            message.TimeToLive,
            message.LockedUntil.UtcDateTime,
            message.ExpiresAt.UtcDateTime,
            message.DeliveryCount,
            message.Subject,
            message.To,
            message.ReplyTo,
            message.PartitionKey,
            message.ContentType,
            message.DeadLetterReason,
            message.DeadLetterErrorDescription);
        var messageDto = new MessageDto(
            message.Body.ToString(),
            brokerProperties,
            message.ApplicationProperties);

        return new ServiceBusMessage(messageDto, new AzureServiceBusMessageContext(receiver, sender, message));
    }
}

