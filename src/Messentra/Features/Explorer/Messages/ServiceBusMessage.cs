namespace Messentra.Features.Explorer.Messages;

public sealed record ServiceBusMessage
{
    public MessageDto Message { get; }
    public IServiceBusMessageContext MessageContext { get; }

    public ServiceBusMessage(MessageDto message, IServiceBusMessageContext messageContext)
    {
        Message = message;
        MessageContext = messageContext;
    }
}