using AutoFixture;
using Azure.Identity;
using Messentra.Infrastructure.AzureServiceBus.Factories;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Infrastructure.AzureServiceBus.Factories;

public sealed class AzureServiceBusTokenCredentialFactoryShould
{
    private readonly Fixture _fixture = new();
    
    [Fact]
    public void CreateTokenCredential()
    {
        // Arrange
        var factory = new AzureServiceBusTokenCredentialFactory();
        var fullyQualifiedNamespace = _fixture.Create<string>();
        var tenantId = _fixture.Create<string>();
        var clientId = _fixture.Create<string>();
        
        // Act
        var credential = factory.Create(fullyQualifiedNamespace, tenantId, clientId);
        
        // Assert
        credential.ShouldNotBeNull();
        credential.ShouldBeOfType<InteractiveBrowserCredential>();
    }
    
    [Fact]
    public void ReturnSameCredentialForSameParameters()
    {
        // Arrange
        var factory = new AzureServiceBusTokenCredentialFactory();
        var fullyQualifiedNamespace = _fixture.Create<string>();
        var tenantId = _fixture.Create<string>();
        var clientId = _fixture.Create<string>();
        
        // Act
        var credential1 = factory.Create(fullyQualifiedNamespace, tenantId, clientId);
        var credential2 = factory.Create(fullyQualifiedNamespace, tenantId, clientId);
        
        // Assert
        credential1.ShouldBeSameAs(credential2);
    }
    
    [Fact]
    public void ReturnDifferentCredentialsForDifferentNamespaces()
    {
        // Arrange
        var factory = new AzureServiceBusTokenCredentialFactory();
        var fullyQualifiedNamespace1 = _fixture.Create<string>();
        var fullyQualifiedNamespace2 = _fixture.Create<string>();
        var tenantId = _fixture.Create<string>();
        var clientId = _fixture.Create<string>();
        
        // Act
        var credential1 = factory.Create(fullyQualifiedNamespace1, tenantId, clientId);
        var credential2 = factory.Create(fullyQualifiedNamespace2, tenantId, clientId);
        
        // Assert
        credential1.ShouldNotBeSameAs(credential2);
    }
    
    [Fact]
    public void ReturnDifferentCredentialsForDifferentTenantIds()
    {
        // Arrange
        var factory = new AzureServiceBusTokenCredentialFactory();
        var fullyQualifiedNamespace = _fixture.Create<string>();
        var tenantId1 = _fixture.Create<string>();
        var tenantId2 = _fixture.Create<string>();
        var clientId = _fixture.Create<string>();
        
        // Act
        var credential1 = factory.Create(fullyQualifiedNamespace, tenantId1, clientId);
        var credential2 = factory.Create(fullyQualifiedNamespace, tenantId2, clientId);
        
        // Assert
        credential1.ShouldNotBeSameAs(credential2);
    }
    
    [Fact]
    public void ReturnDifferentCredentialsForDifferentClientIds()
    {
        // Arrange
        var factory = new AzureServiceBusTokenCredentialFactory();
        var fullyQualifiedNamespace = _fixture.Create<string>();
        var tenantId = _fixture.Create<string>();
        var clientId1 = _fixture.Create<string>();
        var clientId2 = _fixture.Create<string>();
        
        // Act
        var credential1 = factory.Create(fullyQualifiedNamespace, tenantId, clientId1);
        var credential2 = factory.Create(fullyQualifiedNamespace, tenantId, clientId2);
        
        // Assert
        credential1.ShouldNotBeSameAs(credential2);
    }
    
    [Fact]
    public void CacheCredentialsIndependentlyForDifferentCombinations()
    {
        // Arrange
        var factory = new AzureServiceBusTokenCredentialFactory();
        var namespace1 = _fixture.Create<string>();
        var namespace2 = _fixture.Create<string>();
        var tenant1 = _fixture.Create<string>();
        var tenant2 = _fixture.Create<string>();
        var client1 = _fixture.Create<string>();
        var client2 = _fixture.Create<string>();
        
        // Act
        var credential1 = factory.Create(namespace1, tenant1, client1);
        var credential2 = factory.Create(namespace2, tenant2, client2);
        var credential3 = factory.Create(namespace1, tenant1, client1); // Same as credential1
        
        // Assert
        credential1.ShouldNotBeSameAs(credential2);
        credential1.ShouldBeSameAs(credential3);
    }
}

