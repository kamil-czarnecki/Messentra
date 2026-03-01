using Mediator;
using Messentra.Domain;

namespace Messentra.Features.Explorer.Resources.Topics.GetTopicResource;

public sealed record GetTopicResourceQuery(string TopicName, ConnectionConfig ConnectionConfig) : IQuery<GetTopicResult>;

