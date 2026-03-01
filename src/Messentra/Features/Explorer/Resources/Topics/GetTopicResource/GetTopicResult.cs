using Messentra.Infrastructure.AzureServiceBus;
using OneOf;

namespace Messentra.Features.Explorer.Resources.Topics.GetTopicResource;

[GenerateOneOf]
public partial class GetTopicResult : OneOfBase<Resource.Topic, TopicNotFound>;

