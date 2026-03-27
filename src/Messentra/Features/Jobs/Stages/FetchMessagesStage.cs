using Messentra.Domain;

namespace Messentra.Features.Jobs.Stages;

public sealed class FetchMessagesStage<TJob> : IStage<TJob> where TJob : Job, IHasMessageFetchConfiguration
{
    public Task Run(TJob job, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}

public interface IHasMessageFetchConfiguration
{
    MessageFetchConfiguration GetMessageFetchConfiguration();
}

public sealed record MessageFetchConfiguration(ConnectionConfig ConnectionConfig, ResourceTarget Target);
