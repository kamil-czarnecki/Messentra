namespace Messentra.Features.Jobs.Stages.FetchMessages;

public sealed class FetchedMessagesBatch
{
    public long Id { get; init; }
    public required long JobId { get; init; }
    public required IReadOnlyCollection<ServiceBusMessageDto> Messages { get; init; }
    public int MessagesCount { get; init; }
    public required long LastSequence { get; init; }
    public required DateTime CreatedOn { get; init; }
}