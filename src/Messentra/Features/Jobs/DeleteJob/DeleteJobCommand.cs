using Mediator;

namespace Messentra.Features.Jobs.DeleteJob;

public sealed record DeleteJobCommand(long JobId) : ICommand<bool>;

