using Messentra.Domain;
using Messentra.Features.Jobs.ExportMessages;
using Messentra.Features.Jobs.ExportSelectedMessages;
using Messentra.Features.Jobs.ImportMessages;

namespace Messentra.Features.Jobs;

public sealed record FetchJobsAction;
public sealed record FetchJobsSuccessAction(IReadOnlyCollection<Job> Jobs);
public sealed record FetchJobsFailureAction;
public sealed record JobProgressReceivedAction(JobProgressUpdate Update);

public sealed record PauseJobAction(long JobId);
public sealed record PauseJobSuccessAction(long JobId);
public sealed record PauseJobFailureAction(long JobId);

public sealed record ResumeJobAction(long JobId);
public sealed record ResumeJobSuccessAction(long JobId);
public sealed record ResumeJobFailureAction(long JobId);

public sealed record DeleteJobAction(long JobId);
public sealed record DeleteJobSuccessAction(long JobId);
public sealed record DeleteJobFailureAction(long JobId);

public sealed record JobCreatedAction(JobListItem Job);

public sealed record EnqueueExportMessagesAction(ExportMessagesJobRequest Request);
public sealed record EnqueueExportMessagesFailureAction;

public sealed record EnqueueImportMessagesAction(ImportMessagesJobRequest Request);
public sealed record EnqueueImportMessagesFailureAction;

public sealed record EnqueueExportSelectedMessagesAction(ExportSelectedMessagesJobRequest Request);
public sealed record EnqueueExportSelectedMessagesFailureAction;

