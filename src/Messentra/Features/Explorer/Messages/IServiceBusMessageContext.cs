namespace Messentra.Features.Explorer.Messages;

public interface IServiceBusMessageContext
{
    Task Complete(CancellationToken cancellationToken);
    Task Abandon(CancellationToken cancellationToken);
    Task DeadLetter(CancellationToken cancellationToken);
}
