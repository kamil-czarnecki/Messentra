namespace Messentra.Features.Jobs.Stages.ImportMessages;

public sealed class ImportedMessage
{
    public long Id { get; init; }
    public required long JobId { get; init; }
    public long Position { get; init; }
    public required ServiceBusMessageDto Message { get; init; }
    public bool IsSent { get; set; }
    public required DateTime CreatedOn { get; init; }
    public DateTime? SentOn { get; set; }
}

