using Azure.Core;
using Azure.Messaging.ServiceBus;
using Messentra.Infrastructure.AzureServiceBus.Factories;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Infrastructure.AzureServiceBus.Factories;

public sealed class AzureServiceBusClientFactoryShould
{
    private readonly Mock<IAzureServiceBusTokenCredentialFactory> _credentialFactory = new();
    private readonly AzureServiceBusClientFactory _sut;

    public AzureServiceBusClientFactoryShould()
    {
        _credentialFactory
            .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Mock.Of<TokenCredential>());
        
        _sut = new AzureServiceBusClientFactory(_credentialFactory.Object);
    }
    
    [Fact]
    public void CreateClientWithConnectionString()
    {
        // Arrange
        const string connectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey";
        
        // Act
        var client = _sut.CreateClient(connectionString);
        
        // Assert
        client.ShouldNotBeNull();
        client.ShouldBeOfType<ServiceBusClient>();
    }
    
    [Fact]
    public void CreateClientWithTokenCredential()
    {
        // Arrange
        const string fullyQualifiedNamespace = "test.servicebus.windows.net";
        const string tenantId = "test-tenant-id";
        const string clientId = "test-client-id";
        
        // Act
        var client = _sut.CreateClient(fullyQualifiedNamespace, tenantId, clientId);
        
        // Assert
        client.ShouldNotBeNull();
        client.ShouldBeOfType<ServiceBusClient>();
    }
    
    [Fact]
    public async Task DisposeCachedClientsOnDisposeAsync()
    {
        // Arrange
        const string connectionString1 = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey";
        const string connectionString2 = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey2";
        
        var client1 = _sut.CreateClient(connectionString1);
        var client2 = _sut.CreateClient(connectionString2);
        
        // Act
        await _sut.DisposeAsync();
        
        // Assert
        client1.IsClosed.ShouldBeTrue();
        client2.IsClosed.ShouldBeTrue();
    }
    
    [Fact]
    public async Task HandleEmptyClientDictionaryOnDisposeAsync()
    {
        // Act & Assert
        await Should.NotThrowAsync(async () => await _sut.DisposeAsync());
    }
    
    [Fact]
    public void ReturnSameClientForSameKeyWithConnectionString()
    {
        // Arrange
        const string connectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey";
        
        // Act
        var client1 = _sut.CreateClient(connectionString);
        var client2 = _sut.CreateClient(connectionString);
        
        // Assert
        client1.ShouldBeSameAs(client2);
    }
    
    [Fact]
    public void ReturnDifferentClientsForDifferentKeysWithConnectionString()
    {
        // Arrange
        const string connectionString1 = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey";
        const string connectionString2 = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey2";
        
        // Act
        var client1 = _sut.CreateClient(connectionString1);
        var client2 = _sut.CreateClient(connectionString2);
        
        // Assert
        client1.ShouldNotBeSameAs(client2);
    }
    
    [Fact]
    public void ReturnSameClientForSameKeyWithTokenCredential()
    {
        // Arrange
        const string fullyQualifiedNamespace = "test.servicebus.windows.net";
        const string tenantId = "test-tenant-id";
        const string clientId = "test-client-id";
        
        // Act
        var client1 = _sut.CreateClient(fullyQualifiedNamespace, tenantId, clientId);
        var client2 = _sut.CreateClient(fullyQualifiedNamespace, tenantId, clientId);
        
        // Assert
        client1.ShouldBeSameAs(client2);
    }
    
    [Fact]
    public void ReturnDifferentClientsForDifferentKeysWithTokenCredential()
    {
        // Arrange
        const string fullyQualifiedNamespace1 = "test.servicebus.windows.net";
        const string fullyQualifiedNamespace2 = "test2.servicebus.windows.net";
        const string tenantId = "test-tenant-id";
        const string clientId = "test-client-id";
        
        // Act
        var client1 = _sut.CreateClient(fullyQualifiedNamespace1, tenantId, clientId);
        var client2 = _sut.CreateClient(fullyQualifiedNamespace2, tenantId, clientId);
        
        // Assert
        client1.ShouldNotBeSameAs(client2);
    }
}

