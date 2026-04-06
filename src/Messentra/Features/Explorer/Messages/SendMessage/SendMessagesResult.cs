namespace Messentra.Features.Explorer.Messages.SendMessage;

public sealed record SendMessagesError(long SourceSequenceNumber, string Message);

public sealed record SendMessagesResult(
    int TotalCount,
    int SentCount,
    IReadOnlySet<long> SentSequenceNumbers,
    IReadOnlyList<SendMessagesError> Errors)
{
    public int FailedCount => TotalCount - SentCount;
}

