using Mediator;

namespace Messentra.Features.Jobs.ExportSelectedMessages.CreateExportSelectedMessagesJob;

public sealed record CreateExportSelectedMessagesJobCommand(ExportSelectedMessagesJobRequest Request) : ICommand<JobListItem>;
