using Azure.Messaging.ServiceBus;
using Messentra.Features.Explorer.Messages;
using ServiceBusMessage = Azure.Messaging.ServiceBus.ServiceBusMessage;

namespace Messentra.Infrastructure.AzureServiceBus;

internal sealed class AzureServiceBusMessagePeekLockContext : IServiceBusMessageContext
{
    private readonly ServiceBusReceiver _receiver;
    private readonly ServiceBusSender _sender;
    private readonly ServiceBusReceivedMessage _message;

    public AzureServiceBusMessagePeekLockContext(
        ServiceBusReceiver receiver,
        ServiceBusSender sender,
        ServiceBusReceivedMessage message)
    {
        _receiver = receiver;
        _sender = sender;
        _message = message;
    }

    public Task Complete(CancellationToken cancellationToken) =>
        _receiver.CompleteMessageAsync(_message, cancellationToken);

    public Task Abandon(CancellationToken cancellationToken) =>
        _receiver.AbandonMessageAsync(_message, cancellationToken: cancellationToken);

    public Task DeadLetter(CancellationToken cancellationToken) =>
        _receiver.DeadLetterMessageAsync(_message, cancellationToken: cancellationToken);

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