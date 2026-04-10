using Mediator;

namespace Messentra.Features.Jobs.EnqueueJob;

public sealed class EnqueueJobCommandHandler : ICommandHandler<EnqueueJobCommand>
{
    private readonly IBackgroundJobQueue _backgroundJobQueue;

    public EnqueueJobCommandHandler(IBackgroundJobQueue backgroundJobQueue)
    {
        _backgroundJobQueue = backgroundJobQueue;
    }

    public async ValueTask<Unit> Handle(EnqueueJobCommand command, CancellationToken cancellationToken)
    {
        await _backgroundJobQueue.Enqueue(command.JobId, cancellationToken);
        return Unit.Value;
    }
}
