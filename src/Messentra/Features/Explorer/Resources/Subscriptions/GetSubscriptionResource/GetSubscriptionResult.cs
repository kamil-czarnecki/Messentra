using Messentra.Infrastructure.AzureServiceBus;
using OneOf;

namespace Messentra.Features.Explorer.Resources.Subscriptions.GetSubscriptionResource;

[GenerateOneOf]
public partial class GetSubscriptionResult : OneOfBase<Resource.Subscription, SubscriptionNotFound>;

