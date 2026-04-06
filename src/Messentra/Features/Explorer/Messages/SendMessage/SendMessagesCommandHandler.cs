using Mediator;
using Messentra.Infrastructure.AzureServiceBus;

namespace Messentra.Features.Explorer.Messages.SendMessage;

public sealed class SendMessagesCommandHandler : ICommandHandler<SendMessagesCommand, SendMessagesResult>
{
    private const int MaxBatchCandidateSize = 200;

    private readonly IAzureServiceBusSender _sender;

    public SendMessagesCommandHandler(IAzureServiceBusSender sender)
    {
        _sender = sender;
    }

    public async ValueTask<SendMessagesResult> Handle(SendMessagesCommand command, CancellationToken cancellationToken)
    {
        var sentSequenceNumbers = new HashSet<long>();
        var errors = new List<SendMessagesError>();

        if (command.Messages.Count == 0)
            return new SendMessagesResult(0, 0, sentSequenceNumbers, errors);

        var entityPath = GetEntityPath(command.ResourceTreeNode);
        var connectionInfo = command.ResourceTreeNode.ConnectionConfig.ToConnectionInfo();

        for (var start = 0; start < command.Messages.Count; start += MaxBatchCandidateSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var candidateBatch = command.Messages
                .Skip(start)
                .Take(MaxBatchCandidateSize)
                .ToList();

            var offset = 0;
            while (offset < candidateBatch.Count)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var pending = candidateBatch.Skip(offset).ToList();
                var sentFromBatch = await _sender.SendBatchChunk(connectionInfo, entityPath, pending, cancellationToken);

                if (sentFromBatch > 0)
                {
                    foreach (var sentMessage in pending.Take(sentFromBatch))
                        sentSequenceNumbers.Add(sentMessage.SourceSequenceNumber);

                    offset += sentFromBatch;
                    continue;
                }

                var singleMessage = pending[0];
                try
                {
                    await _sender.Send(connectionInfo, entityPath, singleMessage, cancellationToken);
                    sentSequenceNumbers.Add(singleMessage.SourceSequenceNumber);
                }
                catch (Exception ex)
                {
                    errors.Add(new SendMessagesError(singleMessage.SourceSequenceNumber, ex.Message));
                }

                offset++;
            }
        }

        return new SendMessagesResult(
            TotalCount: command.Messages.Count,
            SentCount: sentSequenceNumbers.Count,
            SentSequenceNumbers: sentSequenceNumbers,
            Errors: errors);
    }

    private static string GetEntityPath(Resources.ResourceTreeNode node) =>
        node switch
        {
            Resources.QueueTreeNode queue => queue.Resource.Name,
            Resources.TopicTreeNode topic => topic.Resource.Name,
            Resources.SubscriptionTreeNode subscription => subscription.Resource.TopicName,
            _ => throw new InvalidOperationException($"Unsupported resource type: {node.GetType().Name}")
        };
}


