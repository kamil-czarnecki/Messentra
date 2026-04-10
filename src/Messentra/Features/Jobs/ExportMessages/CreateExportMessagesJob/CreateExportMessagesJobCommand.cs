using Mediator;

namespace Messentra.Features.Jobs.ExportMessages.CreateExportMessagesJob;

public sealed record CreateExportMessagesJobCommand(ExportMessagesJobRequest Request) : ICommand<JobListItem>;