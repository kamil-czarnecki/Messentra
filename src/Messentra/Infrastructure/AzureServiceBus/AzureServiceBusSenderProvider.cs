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
    Task Send(ConnectionInfo info, string entityPath, SendMessageBatchItem message, CancellationToken cancellationToken);
    Task Send(ConnectionInfo info, string entityPath, ServiceBusMessageDto message, CancellationToken cancellationToken);
    Task<int> SendBatchChunk(ConnectionInfo info, string entityPath, IReadOnlyList<SendMessageBatchItem> messages, CancellationToken cancellationToken);
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

    public async Task Send(ConnectionInfo info, string entityPath, SendMessageBatchItem message, CancellationToken cancellationToken)
    {
        await ExecuteWithClientRecovery(info, async client =>
        {
            await using var sender = client.CreateSender(entityPath);
            await sender.SendMessageAsync(FromSendMessageBatchItem(message), cancellationToken);
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

    public async Task<int> SendBatchChunk(ConnectionInfo info, string entityPath, IReadOnlyList<SendMessageBatchItem> messages, CancellationToken cancellationToken)
    {
        if (messages.Count == 0)
            return 0;

        return await ExecuteWithClientRecovery(info, async client =>
        {
            await using var sender = client.CreateSender(entityPath);
            using var batch = await sender.CreateMessageBatchAsync(cancellationToken);
            var sentCount = messages
                .Select(FromSendMessageBatchItem)
                .TakeWhile(batch.TryAddMessage)
                .Count();

            if (sentCount == 0)
                return 0;

            await sender.SendMessagesAsync(batch, cancellationToken);
            return sentCount;
        }, cancellationToken);
    }

    private static ServiceBusMessage FromSendMessageCommand(SendMessageCommand command)
        => BuildSendMessage(
            command.Body,
            command.MessageId,
            command.Label,
            command.CorrelationId,
            command.SessionId,
            command.ReplyToSessionId,
            command.PartitionKey,
            command.ScheduledEnqueueTimeUtc,
            command.TimeToLive,
            command.To,
            command.ReplyTo,
            command.ContentType,
            command.ApplicationProperties);

    private static ServiceBusMessage FromSendMessageBatchItem(SendMessageBatchItem message)
        => BuildSendMessage(
            message.Body,
            message.MessageId,
            message.Label,
            message.CorrelationId,
            message.SessionId,
            message.ReplyToSessionId,
            message.PartitionKey,
            message.ScheduledEnqueueTimeUtc,
            message.TimeToLive,
            message.To,
            message.ReplyTo,
            message.ContentType,
            message.ApplicationProperties);

    private static ServiceBusMessage BuildSendMessage(
        string body,
        string? messageId,
        string? label,
        string? correlationId,
        string? sessionId,
        string? replyToSessionId,
        string? partitionKey,
        DateTime? scheduledEnqueueTimeUtc,
        TimeSpan? timeToLive,
        string? to,
        string? replyTo,
        string? contentType,
        IReadOnlyDictionary<string, object> applicationProperties)
    {
        var message = new ServiceBusMessage(body);

        if (messageId is not null)
            message.MessageId = messageId;

        if (label is not null)
            message.Subject = label;

        if (correlationId is not null)
            message.CorrelationId = correlationId;

        if (sessionId is not null)
            message.SessionId = sessionId;

        if (replyToSessionId is not null)
            message.ReplyToSessionId = replyToSessionId;

        if (partitionKey is not null)
            message.PartitionKey = partitionKey;

        if (scheduledEnqueueTimeUtc is not null)
            message.ScheduledEnqueueTime = new DateTimeOffset(scheduledEnqueueTimeUtc.Value, TimeSpan.Zero);

        if (timeToLive is not null)
            message.TimeToLive = timeToLive.Value;

        if (to is not null)
            message.To = to;

        if (replyTo is not null)
            message.ReplyTo = replyTo;

        if (contentType is not null)
            message.ContentType = contentType;

        foreach (var (key, value) in applicationProperties)
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
            To = messageDto.Properties.To,
            ReplyTo = messageDto.Properties.ReplyTo,
            ReplyToSessionId = messageDto.Properties.ReplyToSessionId,
            SessionId = messageDto.Properties.SessionId,
            PartitionKey = messageDto.Properties.PartitionKey,
            TransactionPartitionKey = messageDto.Properties.TransactionPartitionKey
        };

        if (messageDto.Properties.MessageId is not null)
            message.MessageId = messageDto.Properties.MessageId;
        
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

