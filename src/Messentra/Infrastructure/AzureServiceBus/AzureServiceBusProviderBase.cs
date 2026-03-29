using Azure.Messaging.ServiceBus;
using Messentra.Features.Explorer.Messages;
using Messentra.Infrastructure.AzureServiceBus.Factories;
using ServiceBusMessage = Messentra.Features.Explorer.Messages.ServiceBusMessage;

namespace Messentra.Infrastructure.AzureServiceBus;

public abstract class AzureServiceBusProviderBase(IAzureServiceBusClientFactory clientFactory)
{
    protected async Task ExecuteWithClientRecovery(
        ConnectionInfo info,
        Func<ServiceBusClient, Task> operation,
        CancellationToken cancellationToken)
    {
        await ExecuteWithClientRecovery(
            info,
            async client =>
            {
                await operation(client);
                return true;
            },
            cancellationToken);
    }

    protected async Task<TResult> ExecuteWithClientRecovery<TResult>(
        ConnectionInfo info,
        Func<ServiceBusClient, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        var client = await GetClient(info, cancellationToken);

        try
        {
            return await operation(client);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            await InvalidateClient(info);
            var refreshedClient = await GetClient(info, cancellationToken);

            try
            {
                return await operation(refreshedClient);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                await InvalidateClient(info);
                throw;
            }
        }
    }

    protected async Task<ServiceBusClient> GetClient(ConnectionInfo info, CancellationToken cancellationToken) =>
        info switch
        {
            ConnectionInfo.ConnectionString cs => await clientFactory.CreateClient(cs.Value, cancellationToken),
            ConnectionInfo.ManagedIdentity mi => await clientFactory.CreateClient(
                mi.FullyQualifiedNamespace,
                mi.TenantId,
                mi.ClientId,
                cancellationToken),
            _ => throw new InvalidOperationException("Invalid connection info type")
        };

    private Task InvalidateClient(ConnectionInfo info) =>
        info switch
        {
            ConnectionInfo.ConnectionString cs => clientFactory.InvalidateClient(cs.Value),
            ConnectionInfo.ManagedIdentity mi => clientFactory.InvalidateClient(
                mi.FullyQualifiedNamespace,
                mi.TenantId,
                mi.ClientId),
            _ => throw new InvalidOperationException("Invalid connection info type")
        };
    
    protected static ServiceBusReceiveMode GetReceiveMode(FetchMessagesOptions options) =>
        options switch
        {
            { Mode: FetchMode.Peek } => ServiceBusReceiveMode.PeekLock,
            { Mode: FetchMode.Receive, ReceiveMode: FetchReceiveMode.PeekLock } => ServiceBusReceiveMode.PeekLock,
            { Mode: FetchMode.Receive, ReceiveMode: FetchReceiveMode.ReceiveAndDelete } => ServiceBusReceiveMode
                .ReceiveAndDelete,
            _ => throw new InvalidOperationException("Invalid fetch messages mode")
        };

    protected static ServiceBusMessage Map(
        ServiceBusReceiver receiver,
        ServiceBusSender sender,
        ServiceBusReceivedMessage message)
    {
        var brokerProperties = new BrokerProperties(
            message.MessageId,
            message.SequenceNumber,
            message.CorrelationId,
            message.SessionId,
            message.ReplyToSessionId,
            message.EnqueuedTime.UtcDateTime,
            message.ScheduledEnqueueTime.UtcDateTime,
            message.TimeToLive,
            message.LockedUntil.UtcDateTime,
            message.ExpiresAt.UtcDateTime,
            message.DeliveryCount,
            message.Subject,
            message.To,
            message.ReplyTo,
            message.PartitionKey,
            message.ContentType,
            message.DeadLetterReason,
            message.DeadLetterErrorDescription);
        var messageDto = new MessageDto(
            message.Body.ToString(),
            brokerProperties,
            message.ApplicationProperties);

        return new ServiceBusMessage(messageDto, new AzureServiceBusMessageContext(receiver, sender, message));
    }
}

