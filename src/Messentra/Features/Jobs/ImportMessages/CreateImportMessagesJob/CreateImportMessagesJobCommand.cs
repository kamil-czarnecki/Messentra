using Mediator;

namespace Messentra.Features.Jobs.ImportMessages.CreateImportMessagesJob;

public sealed record CreateImportMessagesJobCommand(ImportMessagesJobRequest Request) : ICommand<JobListItem>;

