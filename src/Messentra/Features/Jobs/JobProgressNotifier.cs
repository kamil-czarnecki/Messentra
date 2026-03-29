using Messentra.Domain;
using System.Collections.Concurrent;

namespace Messentra.Features.Jobs;

public sealed class JobProgressNotifier : IJobProgressNotifier
{
    private readonly ConcurrentDictionary<long, Action<JobProgressUpdate>> _subscribers = new();
    private readonly ILogger<JobProgressNotifier> _logger;
    private long _nextSubscriptionId;

    public JobProgressNotifier(ILogger<JobProgressNotifier> logger)
    {
        _logger = logger;
    }

    public IDisposable Subscribe(Action<JobProgressUpdate> callback)
    {
        var subscriptionId = Interlocked.Increment(ref _nextSubscriptionId);
        _subscribers[subscriptionId] = callback;

        return new Subscription(_subscribers, subscriptionId);
    }

    public void Publish(JobProgressUpdate update)
    {
        foreach (var (subscriptionId, subscriber) in _subscribers)
        {
            try
            {
                subscriber(update);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Subscriber callback failed while publishing job progress update. SubscriptionId: {SubscriptionId}",
                    subscriptionId);
            }
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly ConcurrentDictionary<long, Action<JobProgressUpdate>> _subscribers;
        private readonly long _subscriptionId;
        private int _isDisposed;

        public Subscription(ConcurrentDictionary<long, Action<JobProgressUpdate>> subscribers, long subscriptionId)
        {
            _subscribers = subscribers;
            _subscriptionId = subscriptionId;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) == 1)
                return;

            _subscribers.TryRemove(_subscriptionId, out _);
        }
    }
}
