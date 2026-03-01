using OneOf;

namespace Messentra.Features.Explorer.Messages.SendMessage;

[GenerateOneOf]
public partial class SendMessageResult : OneOfBase<Success, SendMessageError>;

public sealed record Success;

public sealed record SendMessageError(string Message);

