using Messentra.Domain;

namespace Messentra.Features.Jobs;

public sealed record FetchJobsAction;
public sealed record FetchJobsSuccessAction(IReadOnlyCollection<Job> Jobs);
public sealed record FetchJobsFailureAction;
public sealed record JobProgressReceivedAction(JobProgressUpdate Update);

public sealed record PauseJobAction(long JobId);
public sealed record PauseJobSuccessAction;
public sealed record PauseJobFailureAction;

public sealed record ResumeJobAction(long JobId);
public sealed record ResumeJobSuccessAction;
public sealed record ResumeJobFailureAction;