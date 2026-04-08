using Messentra.Domain;
using Messentra.Features.Explorer.Folders;
using Messentra.Infrastructure.AzureServiceBus;

namespace Messentra.Features.Explorer.Resources;

public sealed record FetchResourcesAction(long ConnectionId, string ConnectionName, ConnectionConfig ConnectionConfig);
public sealed record FetchResourcesSuccessAction(long ConnectionId, string ConnectionName, ConnectionConfig ConnectionConfig, IReadOnlyCollection<Resource.Queue> Queues, IReadOnlyCollection<Resource.Topic> Topics, IReadOnlyCollection<FolderDto> Folders);
public sealed record FetchResourcesFailureAction(string ConnectionName, string Error);
public sealed record CancelFetchResourcesAction(string ConnectionName);
public sealed record FetchResourcesCanceledAction(string ConnectionName);

public sealed record SelectResourceAction(ResourceTreeNode Node);
public sealed record DisconnectResourceAction(string ConnectionName);
public sealed record ToggleExpandedAction(string NodeKey, bool Expanded);
public sealed record SetSearchPhraseAction(string? Phrase);

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

public sealed record CreateFolderAction(long ConnectionId, string ConnectionName, ConnectionConfig ConnectionConfig, string Name);
public sealed record CreateFolderSuccessAction(long ConnectionId, string ConnectionName, FolderEntry Entry);
public sealed record CreateFolderFailureAction(string ConnectionName, string Error);

public sealed record CreateFolderAndAddResourceAction(long ConnectionId, string ConnectionName, ConnectionConfig ConnectionConfig, string FolderName, string ResourceUrl);

public sealed record CreateFolderAndAddResourcesAction(long ConnectionId, string ConnectionName, ConnectionConfig ConnectionConfig, string FolderName, IReadOnlyList<string> ResourceUrls);

public sealed record RenameFolderAction(long FolderId, long ConnectionId, string ConnectionName, string NewName);
public sealed record RenameFolderSuccessAction(long FolderId, long ConnectionId, string ConnectionName, string NewName);
public sealed record RenameFolderFailureAction(string ConnectionName, string Error);

public sealed record DeleteFolderAction(long FolderId, long ConnectionId, string ConnectionName);
public sealed record DeleteFolderSuccessAction(long FolderId, long ConnectionId, string ConnectionName);
public sealed record DeleteFolderFailureAction(string ConnectionName, string Error);

public sealed record AddResourceToFolderAction(long FolderId, long ConnectionId, string ConnectionName, string ResourceUrl);
public sealed record AddResourceToFolderSuccessAction(long FolderId, long ConnectionId, string ConnectionName, string ResourceUrl);
public sealed record AddResourceToFolderFailureAction(string ConnectionName, string Error);

public sealed record RemoveResourceFromFolderAction(long FolderId, long ConnectionId, string ConnectionName, string ResourceUrl);
public sealed record RemoveResourceFromFolderSuccessAction(long FolderId, long ConnectionId, string ConnectionName, string ResourceUrl);
public sealed record RemoveResourceFromFolderFailureAction(string ConnectionName, string Error);

public sealed record ExportFoldersAction(long ConnectionId, string ConnectionName, ConnectionConfig ConnectionConfig, string DestinationPath);
public sealed record ExportFoldersSuccessAction(string ConnectionName);
public sealed record ExportFoldersFailureAction(string ConnectionName, string Error);

public sealed record ImportFoldersAction(long ConnectionId, string ConnectionName, ConnectionConfig ConnectionConfig, string JsonContent);
public sealed record ImportFoldersSuccessAction(long ConnectionId, string ConnectionName, ConnectionConfig ConnectionConfig, IReadOnlyList<FolderDto> Folders);
public sealed record ImportFoldersFailureAction(string ConnectionName, string Error);
