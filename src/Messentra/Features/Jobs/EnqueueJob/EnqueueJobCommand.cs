using Mediator;

namespace Messentra.Features.Jobs.EnqueueJob;

public sealed record EnqueueJobCommand(long JobId) : ICommand;
