namespace Messentra.Features.Explorer.Messages.ActionProgress;

public sealed record ActionProgressUpdate(
    int Succeeded,
    int Failed,
    int Pending,
    string? FailedMessageId = null,
    string? FailedReason = null);
