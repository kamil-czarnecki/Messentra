using Messentra.Features.Explorer.Messages;

namespace Messentra.Infrastructure.AzureServiceBus;

internal sealed class AzureServiceBusMessageReceiveAndDeleteContext : IServiceBusMessageContext
{
    public Task Complete(CancellationToken cancellationToken) =>
        throw new NotSupportedException(
            "Cannot complete a deleted message. Please receive the message before completing.");

    public Task Abandon(CancellationToken cancellationToken) =>
        throw new NotSupportedException(
            "Cannot abandon a deleted message. Please receive the message before abandoning.");

    public Task DeadLetter(CancellationToken cancellationToken) =>
        throw new NotSupportedException(
            "Cannot deadletter a deleted message. Please receive the message before deadlettering.");
}
