using Messentra.Domain;

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

