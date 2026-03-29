using Azure.Messaging.ServiceBus;
using System.Text.Json;
using Messentra.Features.Explorer.Messages.SendMessage;
using Messentra.Features.Jobs.Stages;
using Messentra.Infrastructure.AzureServiceBus.Factories;
using DatabaseJsonSerializerOptions = Messentra.Infrastructure.Database.JsonSerializerOptions;

namespace Messentra.Infrastructure.AzureServiceBus;

public interface IAzureServiceBusSender
{
    Task Send(ConnectionInfo info, string entityPath, SendMessageCommand command, CancellationToken cancellationToken);
    Task Send(ConnectionInfo info, string entityPath, ServiceBusMessageDto message, CancellationToken cancellationToken);
    Task<int> SendBatchChunk(ConnectionInfo info, string entityPath, IReadOnlyList<ServiceBusMessageDto> messages, CancellationToken cancellationToken);
}

public sealed class AzureServiceBusSender : AzureServiceBusProviderBase, IAzureServiceBusSender
{
    public AzureServiceBusSender(IAzureServiceBusClientFactory clientFactory) : base(clientFactory)
    {
    }

    public async Task Send(ConnectionInfo info, string entityPath, SendMessageCommand command, CancellationToken cancellationToken)
    {
        await ExecuteWithClientRecovery(info, async client =>
        {
            var sender = client.CreateSender(entityPath);

            var message = FromSendMessageCommand(command);

            await sender.SendMessageAsync(message, cancellationToken);
        }, cancellationToken);
    }

    public async Task Send(ConnectionInfo info, string entityPath, ServiceBusMessageDto message, CancellationToken cancellationToken)
    {
        await ExecuteWithClientRecovery(info, async client =>
        {
            var sender = client.CreateSender(entityPath);
            await sender.SendMessageAsync(FromMessageDto(message), cancellationToken);
        }, cancellationToken);
    }

    public async Task<int> SendBatchChunk(ConnectionInfo info, string entityPath, IReadOnlyList<ServiceBusMessageDto> messages, CancellationToken cancellationToken)
    {
        if (messages.Count == 0)
            return 0;

        return await ExecuteWithClientRecovery(info, async client =>
        {
            var sender = client.CreateSender(entityPath);
            using var batch = await sender.CreateMessageBatchAsync(cancellationToken);
            var sentCount = messages
                .Select(FromMessageDto)
                .TakeWhile(serviceBusMessage => batch.TryAddMessage(serviceBusMessage))
                .Count();

            if (sentCount == 0)
                return 0;

            await sender.SendMessagesAsync(batch, cancellationToken);
            
            return sentCount;
        }, cancellationToken);
    }

    private static ServiceBusMessage FromSendMessageCommand(SendMessageCommand command)
    {
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

        return message;
    }

    private static ServiceBusMessage FromMessageDto(ServiceBusMessageDto messageDto)
    {
        var body = messageDto.Message switch
        {
            JsonElement { ValueKind: JsonValueKind.String } jsonString => jsonString.GetString() ?? string.Empty,
            JsonElement json => json.GetRawText(),
            string text => text,
            _ => JsonSerializer.Serialize(messageDto.Message, DatabaseJsonSerializerOptions.Default)
        };

        var message = new ServiceBusMessage(body)
        {
            ContentType = messageDto.Properties.ContentType,
            CorrelationId = messageDto.Properties.CorrelationId,
            Subject = messageDto.Properties.Subject,
            MessageId = messageDto.Properties.MessageId,
            To = messageDto.Properties.To,
            ReplyTo = messageDto.Properties.ReplyTo,
            ReplyToSessionId = messageDto.Properties.ReplyToSessionId,
            SessionId = messageDto.Properties.SessionId,
            PartitionKey = messageDto.Properties.PartitionKey,
            TransactionPartitionKey = messageDto.Properties.TransactionPartitionKey
        };
        
        if (messageDto.Properties.ScheduledEnqueueTime is not null)
            message.ScheduledEnqueueTime = messageDto.Properties.ScheduledEnqueueTime.Value;

        if (messageDto.Properties.TimeToLive is not null)
            message.TimeToLive = messageDto.Properties.TimeToLive.Value;

        foreach (var (key, value) in messageDto.ApplicationProperties)
            message.ApplicationProperties[key] = NormalizeApplicationPropertyValue(value);

        return message;
    }

    private static object? NormalizeApplicationPropertyValue(object value)
    {
        if (value is not JsonElement jsonElement)
            return value;

        return jsonElement.ValueKind switch
        {
            JsonValueKind.String => jsonElement.TryGetDateTimeOffset(out var dto)
                ? dto
                : jsonElement.TryGetDateTime(out var dateTime)
                    ? dateTime
                    : jsonElement.GetString() ?? string.Empty,
            JsonValueKind.Number => jsonElement.TryGetInt64(out var longValue)
                ? longValue
                : jsonElement.TryGetDouble(out var doubleValue)
                    ? doubleValue
                    : jsonElement.GetRawText(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => jsonElement.GetRawText()
        };
    }
}

