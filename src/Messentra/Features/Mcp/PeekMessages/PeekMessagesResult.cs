namespace Messentra.Features.Mcp.PeekMessages;

public sealed record PeekMessagesResult(
    IReadOnlyList<PeekedMessage> Messages,
    long? NextSequenceNumber);
