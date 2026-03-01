using Messentra.Domain;

namespace Messentra.Features.Explorer.Resources;

public sealed record FetchResourcesAction(string ConnectionName, ConnectionConfig ConnectionConfig);
public sealed record FetchResourcesSuccessAction(ResourceTreeItemData Resource);
public sealed record FetchResourcesFailureAction(string Error);

public sealed record SelectResourceAction(ResourceTreeItemData Resource);
public sealed record DisconnectResourceAction(ResourceTreeItemData Resource);

public sealed record RefreshQueueAction(QueueTreeNode Node);
public sealed record RefreshQueueSuccessAction(QueueTreeNode UpdatedNode);
public sealed record RefreshQueueFailureAction(QueueTreeNode Node, string Error);

public sealed record RefreshTopicAction(TopicTreeNode Node);
public sealed record RefreshTopicSuccessAction(TopicTreeNode UpdatedNode);
public sealed record RefreshTopicFailureAction(TopicTreeNode Node, string Error);

public sealed record RefreshSubscriptionAction(SubscriptionTreeNode Node);
public sealed record RefreshSubscriptionSuccessAction(SubscriptionTreeNode UpdatedNode);
public sealed record RefreshSubscriptionFailureAction(SubscriptionTreeNode Node, string Error);

public sealed record RefreshQueuesAction(QueuesTreeNode Node);
public sealed record RefreshQueuesSuccessAction(QueuesTreeNode Node, IReadOnlyCollection<QueueTreeNode> UpdatedNodes);
public sealed record RefreshQueuesFailureAction(QueuesTreeNode Node, string Error);

public sealed record RefreshTopicsAction(TopicsTreeNode Node);
public sealed record RefreshTopicsSuccessAction(TopicsTreeNode Node, IReadOnlyCollection<TopicTreeNode> UpdatedNodes);
public sealed record RefreshTopicsFailureAction(TopicsTreeNode Node, string Error);
