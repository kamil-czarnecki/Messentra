using Mediator;
using Messentra.Domain;

namespace Messentra.Features.Jobs.GetJobs;

public sealed record GetJobsQuery : IQuery<IReadOnlyList<Job>>;
