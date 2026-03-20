using Fluxor;

namespace Messentra.Features.Explorer.Resources;

public static class ResourceReducers
{
    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, FetchResourcesAction action)
    {
        var entry = new NamespaceEntry(action.ConnectionName, action.ConnectionConfig, IsLoading: true, Queues: [], Topics: []);
        var expandedKeys = new HashSet<string>(state.ExpandedKeys) { $"ns:{action.ConnectionName}" };
        return state with { Namespaces = [..state.Namespaces, entry], ExpandedKeys = expandedKeys };
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, FetchResourcesSuccessAction action)
    {
        var queues = action.Queues.ToDictionary(
            q => q.Url,
            q => new QueueEntry(new QueueTreeNode(action.ConnectionName, q, action.ConnectionConfig), false));

        var topics = action.Topics.ToDictionary(
            t => t.Url,
            t =>
            {
                var subs = t.Subscriptions.ToDictionary(
                    s => s.Url,
                    s => new SubscriptionEntry(new SubscriptionTreeNode(action.ConnectionName, s, action.ConnectionConfig), false));
                return new TopicEntry(new TopicTreeNode(action.ConnectionName, t, action.ConnectionConfig), false, subs);
            });

        var updated = new NamespaceEntry(action.ConnectionName, action.ConnectionConfig, false, queues, topics);
        return state with
        {
            Namespaces = state.Namespaces
                .Select(n => n.ConnectionName == action.ConnectionName ? updated : n)
                .ToList()
        };
    }

    [ReducerMethod]
    public static ResourceState OnFetchResourcesFailure(ResourceState state, FetchResourcesFailureAction action)
    {
        return state with
        {
            Namespaces = state.Namespaces
                .Where(n => !(n.ConnectionName == action.ConnectionName && n.IsLoading))
                .ToList(),
            ExpandedKeys = state.ExpandedKeys
                .Where(k => k != $"ns:{action.ConnectionName}")
                .ToHashSet()
        };
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, SelectResourceAction action)
        => state with { SelectedResource = action.Node };

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, DisconnectResourceAction action)
    {
        var updatedSelected = state.SelectedResource switch
        {
            NamespaceTreeNode n when n.ConnectionName == action.ConnectionName => null,
            QueuesTreeNode n when n.ConnectionName == action.ConnectionName => null,
            TopicsTreeNode n when n.ConnectionName == action.ConnectionName => null,
            QueueTreeNode n when n.ConnectionName == action.ConnectionName => null,
            TopicTreeNode n when n.ConnectionName == action.ConnectionName => null,
            SubscriptionTreeNode n when n.ConnectionName == action.ConnectionName => null,
            _ => state.SelectedResource
        };

        return state with
        {
            Namespaces = state.Namespaces.Where(n => n.ConnectionName != action.ConnectionName).ToList(),
            SelectedResource = updatedSelected,
            ExpandedKeys = []
        };
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, SetSearchPhraseAction action)
        => state with { SearchPhrase = string.IsNullOrEmpty(action.Phrase) ? null : action.Phrase };

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, ToggleExpandedAction action)
    {
        var keys = new HashSet<string>(state.ExpandedKeys);
        
        if (action.Expanded) 
            keys.Add(action.NodeKey);
        else
            keys.Remove(action.NodeKey);
        
        return state with { ExpandedKeys = keys };
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshQueueAction action)
    {
        var url = action.Node.Resource.Url;

        return state with
        {
            Namespaces = state.Namespaces
                .Select(ns => ns.Queues.ContainsKey(url)
                    ? ns with { Queues = ns.Queues.With(url, e => e with { IsLoading = true }) }
                    : ns)
                .ToList(),
            SelectedResource = state.SelectedResource is QueueTreeNode q && q.Resource.Url == url
                ? q with { IsLoading = true }
                : state.SelectedResource
        };
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshQueueSuccessAction action)
    {
        var url = action.UpdatedNode.Resource.Url;
        return state with
        {
            Namespaces = state.Namespaces
                .Select(ns => ns.Queues.ContainsKey(url)
                    ? ns with { Queues = ns.Queues.With(url, _ => new QueueEntry(action.UpdatedNode, false)) }
                    : ns)
                .ToList(),
            SelectedResource = state.SelectedResource is QueueTreeNode q && q.Resource.Url == url
                ? action.UpdatedNode with { IsLoading = false }
                : state.SelectedResource
        };
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshQueueFailureAction action)
    {
        var url = action.Node.Resource.Url;
        return state with
        {
            Namespaces = state.Namespaces
                .Select(ns => ns.Queues.ContainsKey(url)
                    ? ns with { Queues = ns.Queues.With(url, e => e with { IsLoading = false }) }
                    : ns)
                .ToList(),
            SelectedResource = state.SelectedResource is QueueTreeNode q && q.Resource.Url == url
                ? q with { IsLoading = false }
                : state.SelectedResource
        };
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshTopicAction action)
    {
        var url = action.Node.Resource.Url;
        return state with
        {
            Namespaces = state.Namespaces
                .Select(ns => ns.Topics.ContainsKey(url)
                    ? ns with
                    {
                        Topics = ns.Topics.With(url, e =>
                            e with
                            {
                                IsLoading = true,
                                Subscriptions = e.Subscriptions.ToDictionary(kv => kv.Key,
                                    kv => kv.Value with { IsLoading = true })
                            })
                    }
                    : ns)
                .ToList(),
            SelectedResource = state.SelectedResource switch
            {
                TopicTreeNode t when t.Resource.Url == url => t with { IsLoading = true },
                SubscriptionTreeNode s when action.Node.Resource.Subscriptions.Any(sub => sub.Url == s.Resource.Url)
                    => s with { IsLoading = true },
                _ => state.SelectedResource
            }
        };
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshTopicSuccessAction action)
    {
        var url = action.UpdatedNode.Resource.Url;
        return state with
        {
            Namespaces = state.Namespaces
                .Select(ns => ns.Topics.ContainsKey(url)
                    ? ns with
                    {
                        Topics = ns.Topics.With(url, _ =>
                        {
                            var subs = action.UpdatedNode.Resource.Subscriptions.ToDictionary(
                                s => s.Url,
                                s => new SubscriptionEntry(
                                    new SubscriptionTreeNode(action.UpdatedNode.ConnectionName, s,
                                        action.UpdatedNode.ConnectionConfig), false));
                            return new TopicEntry(action.UpdatedNode with { IsLoading = false }, false, subs);
                        })
                    }
                    : ns)
                .ToList(),
            SelectedResource = state.SelectedResource is TopicTreeNode t && t.Resource.Url == url
                ? action.UpdatedNode with { IsLoading = false }
                : state.SelectedResource
        };
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshTopicFailureAction action)
    {
        var url = action.Node.Resource.Url;
        return state with
        {
            Namespaces = state.Namespaces
                .Select(ns => ns.Topics.ContainsKey(url)
                    ? ns with
                    {
                        Topics = ns.Topics.With(url, e =>
                            e with
                            {
                                IsLoading = false,
                                Subscriptions = e.Subscriptions.ToDictionary(kv => kv.Key,
                                    kv => kv.Value with { IsLoading = false })
                            })
                    }
                    : ns)
                .ToList(),
            SelectedResource = state.SelectedResource switch
            {
                TopicTreeNode t when t.Resource.Url == url => t with { IsLoading = false },
                SubscriptionTreeNode s when action.Node.Resource.Subscriptions.Any(sub => sub.Url == s.Resource.Url)
                    => s with { IsLoading = false },
                _ => state.SelectedResource
            }
        };
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshSubscriptionAction action)
    {
        var url = action.Node.Resource.Url;
        return state with
        {
            Namespaces = state.Namespaces
                .Select(ns =>
                {
                    var topicEntry = ns.Topics.Values.FirstOrDefault(t => t.Subscriptions.ContainsKey(url));
                    if (topicEntry is null) return ns;
                    return ns with
                    {
                        Topics = ns.Topics.With(topicEntry.Node.Resource.Url,
                            e => e with { Subscriptions = e.Subscriptions.With(url, sub => sub with { IsLoading = true }) })
                    };
                })
                .ToList(),
            SelectedResource = state.SelectedResource is SubscriptionTreeNode s && s.Resource.Url == url
                ? s with { IsLoading = true }
                : state.SelectedResource
        };
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshSubscriptionSuccessAction action)
    {
        var url = action.UpdatedNode.Resource.Url;
        return state with
        {
            Namespaces = state.Namespaces
                .Select(ns =>
                {
                    var topicEntry = ns.Topics.Values.FirstOrDefault(t => t.Subscriptions.ContainsKey(url));
                    if (topicEntry is null) return ns;
                    return ns with
                    {
                        Topics = ns.Topics.With(topicEntry.Node.Resource.Url,
                            e => e with
                            {
                                Subscriptions = e.Subscriptions.With(url,
                                    _ => new SubscriptionEntry(action.UpdatedNode with { IsLoading = false },
                                        false))
                            })
                    };
                })
                .ToList(),
            SelectedResource = state.SelectedResource is SubscriptionTreeNode s && s.Resource.Url == url
                ? action.UpdatedNode with { IsLoading = false }
                : state.SelectedResource
        };
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshSubscriptionFailureAction action)
    {
        var url = action.Node.Resource.Url;
        return state with
        {
            Namespaces = state.Namespaces
                .Select(ns =>
                {
                    var topicEntry = ns.Topics.Values.FirstOrDefault(t => t.Subscriptions.ContainsKey(url));
                    if (topicEntry is null) return ns;
                    return ns with
                    {
                        Topics = ns.Topics.With(topicEntry.Node.Resource.Url,
                            e => e with
                            {
                                Subscriptions = e.Subscriptions.With(url, sub => sub with { IsLoading = false })
                            })
                    };
                })
                .ToList(),
            SelectedResource = state.SelectedResource is SubscriptionTreeNode s && s.Resource.Url == url
                ? s with { IsLoading = false }
                : state.SelectedResource
        };
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshQueuesAction action)
    {
        var connName = action.Node.ConnectionName;
        return state with
        {
            Namespaces = state.Namespaces
                .Select(ns => ns.ConnectionName != connName
                    ? ns
                    : ns with
                    {
                        Queues = ns.Queues.ToDictionary(kv => kv.Key, kv => kv.Value with { IsLoading = true })
                    })
                .ToList(),
            SelectedResource = state.SelectedResource is QueueTreeNode q && q.ConnectionName == connName
                ? q with { IsLoading = true }
                : state.SelectedResource
        };
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshQueuesSuccessAction action)
    {
        var connName = action.Node.ConnectionName;
        return state with
        {
            Namespaces = state.Namespaces
                .Select(ns => ns.ConnectionName != connName
                    ? ns
                    : ns with
                    {
                        Queues = action.UpdatedNodes.ToDictionary(n => n.Resource.Url,
                            n => new QueueEntry(n with { IsLoading = false }, false))
                    })
                .ToList(),
            SelectedResource = state.SelectedResource is QueueTreeNode q && q.ConnectionName == connName
                ? (action.UpdatedNodes.FirstOrDefault(n => n.Resource.Url == q.Resource.Url) ?? q) with
                {
                    IsLoading = false
                }
                : state.SelectedResource
        };
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshQueuesFailureAction action)
    {
        var connName = action.Node.ConnectionName;
        return state with
        {
            Namespaces = state.Namespaces
                .Select(ns => ns.ConnectionName != connName
                    ? ns
                    : ns with
                    {
                        Queues = ns.Queues.ToDictionary(kv => kv.Key, kv => kv.Value with { IsLoading = false })
                    })
                .ToList(),
            SelectedResource = state.SelectedResource is QueueTreeNode q && q.ConnectionName == connName
                ? q with { IsLoading = false }
                : state.SelectedResource
        };
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshTopicsAction action)
    {
        var connName = action.Node.ConnectionName;
        return state with
        {
            Namespaces = state.Namespaces
                .Select(ns => ns.ConnectionName != connName
                    ? ns
                    : ns with
                    {
                        Topics = ns.Topics.ToDictionary(kv => kv.Key, kv =>
                            kv.Value with
                            {
                                IsLoading = true,
                                Subscriptions = kv.Value.Subscriptions.ToDictionary(s => s.Key,
                                    s => s.Value with { IsLoading = true })
                            })
                    })
                .ToList(),
            SelectedResource = state.SelectedResource switch
            {
                TopicTreeNode t when t.ConnectionName == connName => t with { IsLoading = true },
                SubscriptionTreeNode s when s.ConnectionName == connName => s with { IsLoading = true },
                _ => state.SelectedResource
            }
        };
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshTopicsSuccessAction action)
    {
        var connName = action.Node.ConnectionName;
        return state with
        {
            Namespaces = state.Namespaces
                .Select(ns => ns.ConnectionName != connName
                    ? ns
                    : ns with
                    {
                        Topics = action.UpdatedNodes.ToDictionary(t => t.Resource.Url, t =>
                        {
                            var subs = t.Resource.Subscriptions.ToDictionary(
                                s => s.Url,
                                s => new SubscriptionEntry(new SubscriptionTreeNode(connName, s, t.ConnectionConfig),
                                    false));
                            return new TopicEntry(t with { IsLoading = false }, false, subs);
                        })
                    })
                .ToList(),
            SelectedResource = state.SelectedResource switch
            {
                TopicTreeNode t when t.ConnectionName == connName =>
                    (action.UpdatedNodes.FirstOrDefault(n => n.Resource.Url == t.Resource.Url) ?? t) with
                    {
                        IsLoading = false
                    },
                SubscriptionTreeNode s when s.ConnectionName == connName =>
                    (action.UpdatedNodes
                            .SelectMany(t => t.Resource.Subscriptions
                                .Select(sub => new SubscriptionTreeNode(t.ConnectionName, sub, t.ConnectionConfig)))
                            .FirstOrDefault(n => n.Resource.Url == s.Resource.Url) ?? s) with
                        {
                            IsLoading = false
                        },
                _ => state.SelectedResource
            }
        };
    }

    [ReducerMethod]
    public static ResourceState Reduce(ResourceState state, RefreshTopicsFailureAction action)
    {
        var connName = action.Node.ConnectionName;
        return state with
        {
            Namespaces = state.Namespaces
                .Select(ns => ns.ConnectionName != connName
                    ? ns
                    : ns with
                    {
                        Topics = ns.Topics.ToDictionary(kv => kv.Key, kv =>
                            kv.Value with
                            {
                                IsLoading = false,
                                Subscriptions = kv.Value.Subscriptions.ToDictionary(s => s.Key,
                                    s => s.Value with { IsLoading = false })
                            })
                    })
                .ToList(),
            SelectedResource = state.SelectedResource switch
            {
                TopicTreeNode t when t.ConnectionName == connName => t with { IsLoading = false },
                SubscriptionTreeNode s when s.ConnectionName == connName => s with { IsLoading = false },
                _ => state.SelectedResource
            }
        };
    }
}

internal static class DictionaryExtensions
{
    internal static Dictionary<TKey, TValue> With<TKey, TValue>(
        this Dictionary<TKey, TValue> dict, TKey key, Func<TValue, TValue> update)
        where TKey : notnull
    {
        var copy = new Dictionary<TKey, TValue>(dict);
        if (copy.TryGetValue(key, out var existing))
            copy[key] = update(existing);
        return copy;
    }
}
