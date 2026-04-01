using Azure.Messaging.ServiceBus;
using Messentra.Features.Explorer.Messages;
using ServiceBusMessage = Azure.Messaging.ServiceBus.ServiceBusMessage;

namespace Messentra.Infrastructure.AzureServiceBus;

internal sealed class AzureServiceBusMessageReceiveAndDeleteContext : IServiceBusMessageContext
{
    private readonly ServiceBusReceivedMessage _message;
    private readonly ServiceBusSender _sender;

    public AzureServiceBusMessageReceiveAndDeleteContext(ServiceBusReceivedMessage message, ServiceBusSender sender)
    {
        _message = message;
        _sender = sender;
    }

    public Task Complete(CancellationToken cancellationToken) =>
        throw new NotSupportedException(
            "Cannot complete a deleted message. Please receive the message before completing.");

    public Task Abandon(CancellationToken cancellationToken) =>
        throw new NotSupportedException(
            "Cannot abandon a deleted message. Please receive the message before abandoning.");

    public Task DeadLetter(CancellationToken cancellationToken) =>
        throw new NotSupportedException(
            "Cannot deadletter a deleted message. Please receive the message before deadlettering.");

    public Task Resend(CancellationToken cancellationToken) =>
        _sender.SendMessageAsync(ToServiceBusMessage(_message), cancellationToken);
    
    private static ServiceBusMessage ToServiceBusMessage(ServiceBusReceivedMessage received)
    {
        var message = new ServiceBusMessage(received.Body)
        {
            ContentType = received.ContentType,
            CorrelationId = received.CorrelationId,
            Subject = received.Subject,
            MessageId = received.MessageId,
            To = received.To,
            ReplyTo = received.ReplyTo,
            TimeToLive = received.TimeToLive,
            ReplyToSessionId = received.ReplyToSessionId,
            SessionId = received.SessionId,
            PartitionKey = received.PartitionKey,
            ScheduledEnqueueTime = received.ScheduledEnqueueTime,
            TransactionPartitionKey = received.TransactionPartitionKey,
            Body = received.Body
        };

        foreach (var prop in received.ApplicationProperties)
            message.ApplicationProperties[prop.Key] = prop.Value;

        return message;
    }
}