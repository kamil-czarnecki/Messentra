using Mediator;

namespace Messentra.Features.Jobs.ExportSelectedMessages.EnqueueExportSelectedMessages;

public sealed record EnqueueExportSelectedMessagesCommand(ExportSelectedMessagesJobRequest Request) : ICommand;
