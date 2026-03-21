using AutoFixture;
using Azure.Core;
using Azure.Identity;
using Messentra.Infrastructure.AzureServiceBus;
using Messentra.Infrastructure.AzureServiceBus.Factories;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Infrastructure.AzureServiceBus.Factories;

public sealed class AzureServiceBusTokenCredentialFactoryShould
{
    private readonly Fixture _fixture = new();
    private readonly Mock<IAuthenticationRecordStore> _authenticationRecordStore = new();
    private readonly Mock<IInteractiveAuthBootstrapper> _bootstrapper = new();
    private readonly AzureServiceBusTokenCredentialFactory _sut;

    public AzureServiceBusTokenCredentialFactoryShould()
    {
        _bootstrapper
            .Setup(x => x.AuthenticateAsync(
                It.IsAny<InteractiveBrowserCredential>(),
                It.IsAny<TokenRequestContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityModelFactory.AuthenticationRecord(
                username: "test-user",
                authority: "https://login.microsoftonline.com/common",
                homeAccountId: "home-account-id",
                tenantId: "tenant-id",
                clientId: "client-id"));

        _sut = new AzureServiceBusTokenCredentialFactory(_authenticationRecordStore.Object, _bootstrapper.Object);
    }
    
    [Fact]
    public async Task CreateTokenCredential()
    {
        // Arrange
        var tenantId = _fixture.Create<string>();
        var clientId = _fixture.Create<string>();
        
        // Act
        var credential = await _sut.Create(tenantId, clientId, CancellationToken.None);
        
        // Assert
        credential.ShouldNotBeNull();
        credential.ShouldBeOfType<InteractiveBrowserCredential>();
    }
    
    [Fact]
    public async Task ReturnSameCredentialForSameParameters()
    {
        // Arrange
        var tenantId = _fixture.Create<string>();
        var clientId = _fixture.Create<string>();
        
        // Act
        var credential1 = await _sut.Create(tenantId, clientId, CancellationToken.None);
        var credential2 = await _sut.Create(tenantId, clientId, CancellationToken.None);
        
        // Assert
        credential1.ShouldBeSameAs(credential2);
    }
    
    [Fact]
    public async Task ReturnDifferentCredentialsForDifferentTenantIds()
    {
        // Arrange
        var tenantId1 = _fixture.Create<string>();
        var tenantId2 = _fixture.Create<string>();
        var clientId = _fixture.Create<string>();
        
        // Act
        var credential1 = await _sut.Create(tenantId1, clientId, CancellationToken.None);
        var credential2 = await _sut.Create(tenantId2, clientId, CancellationToken.None);
        
        // Assert
        credential1.ShouldNotBeSameAs(credential2);
    }
    
    [Fact]
    public async Task ReturnDifferentCredentialsForDifferentClientIds()
    {
        // Arrange
        var tenantId = _fixture.Create<string>();
        var clientId1 = _fixture.Create<string>();
        var clientId2 = _fixture.Create<string>();
        
        // Act
        var credential1 = await _sut.Create(tenantId, clientId1, CancellationToken.None);
        var credential2 = await _sut.Create(tenantId, clientId2, CancellationToken.None);
        
        // Assert
        credential1.ShouldNotBeSameAs(credential2);
    }
    
    [Fact]
    public async Task CacheCredentialsByTenantAndClient()
    {
        // Arrange
        var tenant1 = _fixture.Create<string>();
        var tenant2 = _fixture.Create<string>();
        var client1 = _fixture.Create<string>();
        var client2 = _fixture.Create<string>();
        
        // Act
        var credential1 = await _sut.Create(tenant1, client1, CancellationToken.None);
        var credential2 = await _sut.Create(tenant2, client2, CancellationToken.None);
        var credential3 = await _sut.Create(tenant1, client1, CancellationToken.None); // Same as credential1
        
        // Assert
        credential1.ShouldNotBeSameAs(credential2);
        credential1.ShouldBeSameAs(credential3);
    }
}

