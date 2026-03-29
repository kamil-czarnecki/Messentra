using Mediator;

namespace Messentra.Features.Jobs.ResumeJob;

public sealed record ResumeJobCommand(long JobId) : ICommand<bool>;

