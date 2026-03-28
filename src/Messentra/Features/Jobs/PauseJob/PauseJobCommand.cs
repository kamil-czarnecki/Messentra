using Mediator;

namespace Messentra.Features.Jobs.PauseJob;

public sealed record PauseJobCommand(long JobId) : ICommand<bool>;

