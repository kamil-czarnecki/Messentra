using Mediator;

namespace Messentra.Features.Jobs.ImportMessages.EnqueueImportMessages;

public sealed record EnqueueImportMessagesCommand(ImportMessagesJobRequest Request) : ICommand;

