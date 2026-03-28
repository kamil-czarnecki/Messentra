using Messentra.Domain;
using Messentra.Features.Explorer.Messages;
using Messentra.Features.Jobs;
using Messentra.Features.Jobs.GetJobs;
using Messentra.Features.Jobs.ImportMessages;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Jobs.GetJobs;

public sealed class GetJobsQueryHandlerShould : InMemoryDbTestBase
{
    [Fact]
    public async Task ReturnJobsOrderedByCreatedAtDescending_WhenJobsExist()
    {
        // Arrange
        var sut = new GetJobsQueryHandler(DbContext);
        var oldest = CreateJob("job-oldest", new DateTime(2026, 3, 28, 10, 0, 0, DateTimeKind.Utc));
        var newest = CreateJob("job-newest", new DateTime(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc));
        var middle = CreateJob("job-middle", new DateTime(2026, 3, 28, 11, 0, 0, DateTimeKind.Utc));

        DbContext.Set<Job>().AddRange(oldest, newest, middle);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var query = new GetJobsQuery();

        // Act
        var result = await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Select(x => x.Label).ShouldBe(["job-newest", "job-middle", "job-oldest"]);
    }

    [Fact]
    public async Task ReturnOnly100NewestJobs_WhenMoreThan100JobsExist()
    {
        // Arrange
        var sut = new GetJobsQueryHandler(DbContext);
        var baseTime = new DateTime(2026, 3, 28, 10, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < 105; i++)
        {
            DbContext.Set<Job>().Add(CreateJob($"job-{i}", baseTime.AddMinutes(i)));
        }

        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var query = new GetJobsQuery();

        // Act
        var result = await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(100);
        result.First().Label.ShouldBe("job-104");
        result.Last().Label.ShouldBe("job-5");
        result.ShouldNotContain(x => x.Label == "job-0");
        result.ShouldNotContain(x => x.Label == "job-1");
        result.ShouldNotContain(x => x.Label == "job-2");
        result.ShouldNotContain(x => x.Label == "job-3");
        result.ShouldNotContain(x => x.Label == "job-4");
    }
    
    private static ImportMessagesJob CreateJob(string label, DateTime createdAt) =>
        new()
        {
            Label = label,
            CreatedAt = createdAt,
            Input = new ImportMessagesJobRequest(
                ConnectionConfig.CreateConnectionString("Endpoint=sb://tests/"),
                new ResourceTarget.Queue("orders", SubQueue.Active))
        };
}


