using Messentra.Features.Explorer.Messages;

namespace Messentra.Infrastructure.AzureServiceBus;

internal sealed class AzureServiceBusMessagePeekContext : IServiceBusMessageContext
{
    public Task Complete(CancellationToken cancellationToken) =>
        throw new NotSupportedException(
            "Cannot complete a peeked message. Please receive the message before completing.");

    public Task Abandon(CancellationToken cancellationToken) =>
        throw new NotSupportedException(
            "Cannot abandon a peeked message. Please receive the message before abandoning.");

    public Task DeadLetter(CancellationToken cancellationToken) =>
        throw new NotSupportedException(
            "Cannot deadletter a peeked message. Please receive the message before deadlettering.");
}
