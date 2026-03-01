using Azure.Messaging.ServiceBus;
using Messentra.Features.Explorer.Messages.SendMessage;
using Messentra.Infrastructure.AzureServiceBus.Factories;

namespace Messentra.Infrastructure.AzureServiceBus;

public interface IAzureServiceBusSender
{
    Task Send(ConnectionInfo info, string entityPath, SendMessageCommand command, CancellationToken cancellationToken);
}

public sealed class AzureServiceBusSender : AzureServiceBusProviderBase, IAzureServiceBusSender
{
    public AzureServiceBusSender(IAzureServiceBusClientFactory clientFactory) : base(clientFactory)
    {
    }

    public async Task Send(ConnectionInfo info, string entityPath, SendMessageCommand command, CancellationToken cancellationToken)
    {
        var client = GetClient(info);
        var sender = client.CreateSender(entityPath);

        var message = new ServiceBusMessage(command.Body);

        if (command.MessageId is not null)
            message.MessageId = command.MessageId;

        if (command.Label is not null)
            message.Subject = command.Label;

        if (command.CorrelationId is not null)
            message.CorrelationId = command.CorrelationId;

        if (command.SessionId is not null)
            message.SessionId = command.SessionId;

        if (command.ReplyToSessionId is not null)
            message.ReplyToSessionId = command.ReplyToSessionId;

        if (command.PartitionKey is not null)
            message.PartitionKey = command.PartitionKey;

        if (command.ScheduledEnqueueTimeUtc is not null)
            message.ScheduledEnqueueTime = new DateTimeOffset(command.ScheduledEnqueueTimeUtc.Value, TimeSpan.Zero);

        if (command.TimeToLive is not null)
            message.TimeToLive = command.TimeToLive.Value;

        if (command.To is not null)
            message.To = command.To;

        if (command.ReplyTo is not null)
            message.ReplyTo = command.ReplyTo;

        if (command.ContentType is not null)
            message.ContentType = command.ContentType;

        foreach (var (key, value) in command.ApplicationProperties)
            message.ApplicationProperties[key] = value;

        await sender.SendMessageAsync(message, cancellationToken);
    }
}

