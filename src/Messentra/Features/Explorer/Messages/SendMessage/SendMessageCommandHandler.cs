using Mediator;
using Messentra.Domain;
using Messentra.Features.Explorer.Resources;
using Messentra.Infrastructure.AzureServiceBus;
using ConnectionInfo = Messentra.Infrastructure.AzureServiceBus.ConnectionInfo;

namespace Messentra.Features.Explorer.Messages.SendMessage;

public sealed class SendMessageCommandHandler : ICommandHandler<SendMessageCommand, SendMessageResult>
{
    private readonly IAzureServiceBusSender _sender;

    public SendMessageCommandHandler(IAzureServiceBusSender sender)
    {
        _sender = sender;
    }

    public async ValueTask<SendMessageResult> Handle(SendMessageCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var connectionConfig = command.ResourceTreeNode.ConnectionConfig;
            var connectionInfo = ToConnectionInfo(connectionConfig);
            var entityPath = GetEntityPath(command.ResourceTreeNode);

            await _sender.Send(connectionInfo, entityPath, command, cancellationToken);

            return new Success();
        }
        catch (Exception ex)
        {
            return new SendMessageError(ex.Message);
        }
    }

    private static string GetEntityPath(ResourceTreeNode node) =>
        node switch
        {
            QueueTreeNode queue => queue.Resource.Name,
            TopicTreeNode topic => topic.Resource.Name,
            SubscriptionTreeNode subscription => subscription.Resource.TopicName,
            _ => throw new InvalidOperationException($"Unsupported resource type: {node.GetType().Name}")
        };

    private static ConnectionInfo ToConnectionInfo(ConnectionConfig config) =>
        config.ConnectionType switch
        {
            ConnectionType.ConnectionString => new ConnectionInfo.ConnectionString(
                config.ConnectionStringConfig!.ConnectionString),
            ConnectionType.EntraId => new ConnectionInfo.ManagedIdentity(
                FullyQualifiedNamespace: config.EntraIdConfig!.Namespace,
                TenantId: config.EntraIdConfig.TenantId,
                ClientId: config.EntraIdConfig.ClientId),
            _ => throw new InvalidOperationException($"Unsupported connection type: {config.ConnectionType}")
        };
}

