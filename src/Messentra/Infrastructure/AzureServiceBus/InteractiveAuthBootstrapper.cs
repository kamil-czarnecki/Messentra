using System.Diagnostics.CodeAnalysis;
using Azure.Core;
using Azure.Identity;

namespace Messentra.Infrastructure.AzureServiceBus;

public interface IInteractiveAuthBootstrapper
{
    Task<AuthenticationRecord> AuthenticateAsync(
        InteractiveBrowserCredential credential,
        TokenRequestContext context,
        CancellationToken cancellationToken = default);
}

[ExcludeFromCodeCoverage]
public sealed class InteractiveAuthBootstrapper : IInteractiveAuthBootstrapper
{
    public async Task<AuthenticationRecord> AuthenticateAsync(
        InteractiveBrowserCredential credential,
        TokenRequestContext context,
        CancellationToken cancellationToken = default) =>
        await credential.AuthenticateAsync(context, cancellationToken).ConfigureAwait(false);

}