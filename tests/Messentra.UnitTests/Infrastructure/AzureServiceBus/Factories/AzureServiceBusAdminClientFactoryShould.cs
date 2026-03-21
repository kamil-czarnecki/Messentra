using Azure.Core;
using Azure.Messaging.ServiceBus.Administration;
using Messentra.Infrastructure.AzureServiceBus.Factories;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Infrastructure.AzureServiceBus.Factories;

public sealed class AzureServiceBusAdminClientFactoryShould
{
    private readonly Mock<IAzureServiceBusTokenCredentialFactory> _credentialFactory = new();
    private readonly AzureServiceBusAdminClientFactory _sut;

    public AzureServiceBusAdminClientFactoryShould()
    {
        _credentialFactory
            .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<TokenCredential>());
        
        _sut = new AzureServiceBusAdminClientFactory(_credentialFactory.Object);
    }
 
    [Fact]
    public async Task CreateClientWithConnectionString()
    {
        // Arrange
        const string connectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey";
        
        // Act
        var client = await _sut.CreateClient(connectionString, CancellationToken.None);
        
        // Assert
        client.ShouldNotBeNull();
        client.ShouldBeOfType<ServiceBusAdministrationClient>();
    }
    
    [Fact]
    public async Task CreateClientWithTokenCredential()
    {
        // Arrange
        const string fullyQualifiedNamespace = "test.servicebus.windows.net";
        const string tenantId = "test-tenant-id";
        const string clientId = "test-client-id";
        
        // Act
        var client = await _sut.CreateClient(fullyQualifiedNamespace, tenantId, clientId, CancellationToken.None);
        
        // Assert
        client.ShouldNotBeNull();
        client.ShouldBeOfType<ServiceBusAdministrationClient>();
    }
    
    [Fact]
    public async Task CacheClientAcrossMixedAuthenticationMethods()
    {
        // Arrange
        const string connectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey";
        const string fullyQualifiedNamespace = "test.servicebus.windows.net";
        const string tenantId = "test-tenant-id";
        const string clientId = "test-client-id";
        
        // Act
        var clientWithConnectionString = await _sut.CreateClient(connectionString, CancellationToken.None);
        var clientWithToken = await _sut.CreateClient(fullyQualifiedNamespace, tenantId, clientId, CancellationToken.None);
        
        // Assert
        clientWithConnectionString.ShouldNotBeNull();
        clientWithToken.ShouldNotBeNull();
        clientWithConnectionString.ShouldNotBeSameAs(clientWithToken);
    }
    
    [Fact]
    public async Task ReturnSameClientForSameKeyWithConnectionString()
    {
        // Arrange
        const string connectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey";
        
        // Act
        var client1 = await _sut.CreateClient(connectionString, CancellationToken.None);
        var client2 = await _sut.CreateClient(connectionString, CancellationToken.None);
        
        // Assert
        client1.ShouldBeSameAs(client2);
    }
    
    [Fact]
    public async Task ReturnDifferentClientsForDifferentKeysWithConnectionString()
    {
        // Arrange
        const string connectionString1 = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey";
        const string connectionString2 = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey2";
        
        // Act
        var client1 = await _sut.CreateClient(connectionString1, CancellationToken.None);
        var client2 = await _sut.CreateClient(connectionString2, CancellationToken.None);
        
        // Assert
        client1.ShouldNotBeSameAs(client2);
    }
    
    [Fact]
    public async Task ReturnSameClientForSameKeyWithTokenCredential()
    {
        // Arrange
        const string fullyQualifiedNamespace = "test.servicebus.windows.net";
        const string tenantId = "test-tenant-id";
        const string clientId = "test-client-id";
        
        // Act
        var client1 = await _sut.CreateClient(fullyQualifiedNamespace, tenantId, clientId, CancellationToken.None);
        var client2 = await _sut.CreateClient(fullyQualifiedNamespace, tenantId, clientId, CancellationToken.None);
        
        // Assert
        client1.ShouldBeSameAs(client2);
    }

    [Fact]
    public async Task CreateClientWithTokenCredential_UsesCredentialFactoryOnlyWhenCreatingNewClient()
    {
        // Arrange
        const string fullyQualifiedNamespace = "test.servicebus.windows.net";
        const string tenantId = "test-tenant-id";
        const string clientId = "test-client-id";

        // Act
        _ = await _sut.CreateClient(fullyQualifiedNamespace, tenantId, clientId, CancellationToken.None);
        _ = await _sut.CreateClient(fullyQualifiedNamespace, tenantId, clientId, CancellationToken.None);

        // Assert
        _credentialFactory.Verify(
            x => x.Create(tenantId, clientId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReturnDifferentClientsForDifferentKeysWithTokenCredential()
    {
        // Arrange
        const string fullyQualifiedNamespace1 = "test.servicebus.windows.net";
        const string fullyQualifiedNamespace2 = "test2.servicebus.windows.net";
        const string tenantId = "test-tenant-id";
        const string clientId = "test-client-id";

        // Act
        var client1 = await _sut.CreateClient(fullyQualifiedNamespace1, tenantId, clientId, CancellationToken.None);
        var client2 = await _sut.CreateClient(fullyQualifiedNamespace2, tenantId, clientId, CancellationToken.None);

        // Assert
        client1.ShouldNotBeSameAs(client2);
    }
}

