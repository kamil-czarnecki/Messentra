using Azure;
using Moq;

namespace Messentra.UnitTests.Infrastructure.AzureServiceBus;

internal sealed class TestAsyncPageable<T>(IEnumerable<T> items) : AsyncPageable<T> where T : notnull
{
    public override async IAsyncEnumerable<Page<T>> AsPages(string? continuationToken = null, int? pageSizeHint = null)
    {
        yield return Page<T>.FromValues(items.ToList(), null, Mock.Of<Response>());
        await Task.CompletedTask;
    }
}

internal static class TestAsyncPageableHelper
{
    public static AsyncPageable<T> AsyncPageable<T>(params T[] items) where T : notnull =>
        new TestAsyncPageable<T>(items);
}
