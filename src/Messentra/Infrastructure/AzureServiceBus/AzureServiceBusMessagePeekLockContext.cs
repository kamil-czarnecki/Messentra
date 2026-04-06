using Azure.Messaging.ServiceBus;
using Messentra.Features.Explorer.Messages;

namespace Messentra.Infrastructure.AzureServiceBus;

internal sealed class AzureServiceBusMessagePeekLockContext : IServiceBusMessageContext
{
    private readonly ServiceBusReceiver _receiver;
    private readonly ServiceBusReceivedMessage _message;

    public AzureServiceBusMessagePeekLockContext(
        ServiceBusReceiver receiver,
        ServiceBusReceivedMessage message)
    {
        _receiver = receiver;
        _message = message;
    }

    public Task Complete(CancellationToken cancellationToken) =>
        _receiver.CompleteMessageAsync(_message, cancellationToken);

    public Task Abandon(CancellationToken cancellationToken) =>
        _receiver.AbandonMessageAsync(_message, cancellationToken: cancellationToken);

    public Task DeadLetter(CancellationToken cancellationToken) =>
        _receiver.DeadLetterMessageAsync(_message, cancellationToken: cancellationToken);
}
