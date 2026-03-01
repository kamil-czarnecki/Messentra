namespace Messentra.Features.Explorer.Messages;

public sealed record FetchMessagesOptions(
    FetchMode Mode,
    FetchReceiveMode ReceiveMode,
    int MessageCount,
    long? StartSequence,
    TimeSpan WaitTime,
    SubQueue SubQueue = SubQueue.Active);
