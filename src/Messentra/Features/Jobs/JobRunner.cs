using System.Reflection;
using Messentra.Domain;
using Messentra.Features.Jobs.Stages;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Jobs;

public interface IJobRunner
{
    Task Run(long jobId, CancellationToken cancellationToken);
}

public sealed class JobRunner : IJobRunner
{
    private static readonly MethodInfo ExecuteStageWithRetryMethod = typeof(JobRunner)
        .GetMethod(nameof(ExecuteStageWithRetryGeneric), BindingFlags.Static | BindingFlags.NonPublic)!;

    private readonly IServiceScopeFactory _serviceScopeFactory;

    public JobRunner(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task Run(long jobId, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MessentraDbContext>();
        var registry = scope.ServiceProvider.GetRequiredService<IJobCancellationRegistry>();

        var job = await dbContext.Set<Job>().SingleOrDefaultAsync(x => x.Id == jobId, cancellationToken);
        if (job is null)
        {
            throw new InvalidOperationException($"Job '{jobId}' was not found.");
        }

        var jobCts = registry.Register(job.Id);
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(jobCts.Token, cancellationToken);

        try
        {
            job.UpdateStatus(JobStatus.Running);
            await dbContext.SaveChangesAsync(cancellationToken);

            for (var i = job.CurrentStageIndex; i < job.Stages.Count; i++)
            {
                var stageType = job.Stages[i];
                var stage = scope.ServiceProvider.GetRequiredService(stageType);

                job.CurrentStageIndex = i;
                
                await ExecuteStageWithRetry(job, stage, async () =>
                {
                    await dbContext.SaveChangesAsync(cancellationToken);
                }, linkedCts.Token);
            }

            job.UpdateStatus(JobStatus.Completed);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            job.UpdateStatus(JobStatus.Paused);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            job.UpdateStatus(JobStatus.Failed, ex.Message);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            linkedCts.Dispose();
            registry.Unregister(job.Id);
        }
    }

    private static Task ExecuteStageWithRetry(Job job, object stage, Func<Task> persistAsync, CancellationToken ct)
    {
        var typedMethod = ExecuteStageWithRetryMethod.MakeGenericMethod(job.GetType());
        
        return (Task)typedMethod.Invoke(null, [job, stage, persistAsync, ct])!;
    }

    private static async Task ExecuteStageWithRetryGeneric<TJob>(TJob job, object stageObject, Func<Task> persistAsync, CancellationToken ct)
        where TJob : Job
    {
        if (stageObject is not IStage<TJob> stage)
        {
            throw new InvalidOperationException($"Resolved stage '{stageObject.GetType().Name}' does not implement IStage<{typeof(TJob).Name}>.");
        }

        var maxStageRetries = job.MaxRetries;

        while (true)
        {
            try
            {
                await stage.Run(job, ct);
                job.UpdateRetryCount(0);
                await persistAsync();

                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                job.UpdateRetryCount(job.RetryCount + 1);
                await persistAsync();

                if (job.RetryCount >= maxStageRetries)
                {
                    throw;
                }

                var delayMs = (int)Math.Pow(2, job.RetryCount) * 1000;
                await Task.Delay(delayMs, ct);
            }
        }
    }
}
