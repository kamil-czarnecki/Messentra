namespace Messentra.Features.Explorer.Resources.Components;

internal static class ResourceTreeItemKeys
{
    internal static string GetKey(ResourceTreeNode? node) => node switch
    {
        NamespaceTreeNode n => $"ns:{n.ConnectionName}",
        FoldersTreeNode n => $"folders:{n.ConnectionName}",
        FolderTreeNode n => $"folder:{n.FolderId}",
        QueuesTreeNode n => $"queues:{n.ConnectionName}",
        TopicsTreeNode n => $"topics:{n.ConnectionName}",
        QueueTreeNode n => $"queue:{n.Resource.Url}",
        TopicTreeNode n => $"topic:{n.Resource.Url}",
        SubscriptionTreeNode n => $"sub:{n.Resource.Url}",
        _ => string.Empty
    };
}
