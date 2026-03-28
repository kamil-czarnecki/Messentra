using Mediator;

namespace Messentra.Features.Jobs.ExportMessages.EnqueueExportMessages;

public sealed record EnqueueExportMessagesCommand(ExportMessagesJobRequest Request) : ICommand;