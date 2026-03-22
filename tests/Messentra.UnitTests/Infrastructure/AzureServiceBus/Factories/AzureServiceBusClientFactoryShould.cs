using Azure.Core;
using Azure.Messaging.ServiceBus;
using Messentra.Infrastructure.AzureServiceBus.Factories;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Infrastructure.AzureServiceBus.Factories;

public sealed class AzureServiceBusClientFactoryShould
{
    private readonly Mock<IAzureServiceBusTokenCredentialFactory> _credentialFactory = new();
    private readonly Mock<ILogger<AzureServiceBusClientFactory>> _logger = new();
    private readonly AzureServiceBusClientFactory _sut;

    public AzureServiceBusClientFactoryShould()
    {
        _credentialFactory
            .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<TokenCredential>());
        
        _sut = new AzureServiceBusClientFactory(
            _credentialFactory.Object,
            _logger.Object);
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
        client.ShouldBeOfType<ServiceBusClient>();
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
        client.ShouldBeOfType<ServiceBusClient>();
    }
    
    [Fact]
    public async Task DisposeCachedClientsOnDisposeAsync()
    {
        // Arrange
        const string connectionString1 = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey";
        const string connectionString2 = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey2";
        
        var client1 = await _sut.CreateClient(connectionString1, CancellationToken.None);
        var client2 = await _sut.CreateClient(connectionString2, CancellationToken.None);
        
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
    public async Task CreateClientWithTokenCredential_CallsCredentialFactoryOnce()
    {
        // Arrange
        const string fullyQualifiedNamespace = "test.servicebus.windows.net";
        const string tenantId = "test-tenant-id";
        const string clientId = "test-client-id";

        // Act
        _ = await _sut.CreateClient(fullyQualifiedNamespace, tenantId, clientId, CancellationToken.None);
        _ = await _sut.CreateClient(fullyQualifiedNamespace, tenantId, clientId, CancellationToken.None);

        // Assert
        _credentialFactory.Verify(x => x.Create(tenantId, clientId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateClientWithTokenCredential_WhenFirstInitializationFails_RetriesOnNextCall()
    {
        // Arrange
        const string fullyQualifiedNamespace = "test.servicebus.windows.net";
        const string tenantId = "test-tenant-id";
        const string clientId = "test-client-id";

        _credentialFactory
            .SetupSequence(x => x.Create(tenantId, clientId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("auth failed"))
            .ReturnsAsync(Mock.Of<TokenCredential>());

        // Act
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _sut.CreateClient(fullyQualifiedNamespace, tenantId, clientId, CancellationToken.None));

        var client = await _sut.CreateClient(fullyQualifiedNamespace, tenantId, clientId, CancellationToken.None);

        // Assert
        client.ShouldNotBeNull();
        _credentialFactory.Verify(x => x.Create(tenantId, clientId, It.IsAny<CancellationToken>()), Times.Exactly(2));
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

    [Fact]
    public async Task CreateClientWithConnectionString_WhenEmulatorAndOnlyPortDiffers_ReturnsSameClient()
    {
        // Arrange
        const string connectionStringWithPort5300 =
            "Endpoint=sb://localhost:5300;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
        const string connectionStringWithPort5301 =
            "Endpoint=sb://localhost:5301;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

        // Act
        var client1 = await _sut.CreateClient(connectionStringWithPort5300, CancellationToken.None);
        var client2 = await _sut.CreateClient(connectionStringWithPort5301, CancellationToken.None);

        // Assert
        client1.ShouldBeSameAs(client2);
    }

    [Fact]
    public async Task CreateClientWithConnectionString_WhenNotEmulatorAndPortDiffers_ReturnsDifferentClients()
    {
        // Arrange
        const string connectionStringWithPort5300 =
            "Endpoint=sb://localhost:5300;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;";
        const string connectionStringWithPort5301 =
            "Endpoint=sb://localhost:5301;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;";

        // Act
        var client1 = await _sut.CreateClient(connectionStringWithPort5300, CancellationToken.None);
        var client2 = await _sut.CreateClient(connectionStringWithPort5301, CancellationToken.None);

        // Assert
        client1.ShouldNotBeSameAs(client2);
    }

    [Fact]
    public async Task CreateClientWithConnectionString_WhenConnectionStringBuilderThrows_LogsError()
    {
        // Arrange
        const string invalidConnectionString = "bad-connection-string-without-key-value-pairs";

        // Act
        await Should.ThrowAsync<Exception>(async () =>
            await _sut.CreateClient(invalidConnectionString, CancellationToken.None));

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("Failed to normalize connection string for emulator", StringComparison.Ordinal)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

