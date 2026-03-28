using System.Threading.Channels;
using Messentra.Features.Jobs;

namespace Messentra.Infrastructure.Jobs;

public sealed class BackgroundJobQueue : IBackgroundJobQueue
{
    private readonly Channel<long> _channel = Channel.CreateUnbounded<long>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public async Task Enqueue(long jobId, CancellationToken cancellationToken) =>
        await _channel.Writer.WriteAsync(jobId, cancellationToken);

    public async ValueTask<long> Dequeue(CancellationToken cancellationToken) =>
        await _channel.Reader.ReadAsync(cancellationToken);
}