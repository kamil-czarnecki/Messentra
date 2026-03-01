using Messentra.Domain;
using Messentra.Features.Settings.Connections.UpdateConnection;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Settings.Connections.UpdateConnection;

public sealed class UpdateConnectionCommandValidatorShould
{
    private readonly UpdateConnectionCommandValidator _sut;

    public UpdateConnectionCommandValidatorShould()
    {
        _sut = new UpdateConnectionCommandValidator();
    }
    
    [Fact]
    public async Task PassValidation_WhenNameIsValid()
    {
        // Arrange
        var command = new UpdateConnectionCommand(
            1,
            "Test Connection",
            new ConnectionConfigDto(
                ConnectionType.ConnectionString,
                "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=testkey",
                null,
                null,
                null));

        // Act
        var result = await _sut.ValidateAsync(command, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task PassValidation_WhenConnectionStringTypeIsValid()
    {
        // Arrange
        var command = new UpdateConnectionCommand(
            1,
            "Test Connection",
            new ConnectionConfigDto(
                ConnectionType.ConnectionString,
                "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=testkey",
                null,
                null,
                null));

        // Act
        var result = await _sut.ValidateAsync(command, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task PassValidation_WhenEntraIdTypeIsValid()
    {
        // Arrange
        var command = new UpdateConnectionCommand(
            1,
            "Test Connection",
            new ConnectionConfigDto(
                ConnectionType.EntraId,
                null,
                "test.servicebus.windows.net",
                "00000000-0000-0000-0000-000000000000",
                "11111111-1111-1111-1111-111111111111"));
                

        // Act
        var result = await _sut.ValidateAsync(command, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }
    
    [Fact]
    public async Task FailValidation_WhenIdIsLessThan1()
    {
        // Arrange
        var command = new UpdateConnectionCommand(
            0,
            "Test Connection",
            new ConnectionConfigDto(
                ConnectionType.ConnectionString,
                "Endpoint=test",
                null,
                null,
                null));

        // Act
        var result = await _sut.ValidateAsync(command, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].PropertyName.ShouldBe("Id");
        result.Errors[0].ErrorCode.ShouldBe("GreaterThanValidator");
    }

    [Fact]
    public async Task FailValidation_WhenNameIsEmpty()
    {
        // Arrange
        var command = new UpdateConnectionCommand(
            1,
            string.Empty,
            new ConnectionConfigDto(
                ConnectionType.ConnectionString,
                "Endpoint=test",
                null,
                null,
                null));

        // Act
        var result = await _sut.ValidateAsync(command, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].PropertyName.ShouldBe("Name");
        result.Errors[0].ErrorMessage.ShouldBe("Name is required.");
    }

    [Fact]
    public async Task FailValidation_WhenNameIsNull()
    {
        // Arrange
        var command = new UpdateConnectionCommand(
            1,
            null!,
            new ConnectionConfigDto(
                ConnectionType.ConnectionString,
                "Endpoint=test",
                null,
                null,
                null));

        // Act
        var result = await _sut.ValidateAsync(command, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].PropertyName.ShouldBe("Name");
        result.Errors[0].ErrorMessage.ShouldBe("Name is required.");
    }

    [Fact]
    public async Task FailValidation_WhenConnectionTypeIsInvalid()
    {
        // Arrange
        var command = new UpdateConnectionCommand(
            1,
            "Test Connection",
            new ConnectionConfigDto(
                (ConnectionType)999,
                "Endpoint=test",
                null,
                null,
                null));

        // Act
        var result = await _sut.ValidateAsync(command, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ErrorCode.ShouldBe("EnumValidator");
        result.Errors[0].PropertyName.ShouldBe("ConnectionConfig.ConnectionType");
    }

    [Fact]
    public async Task FailValidation_WhenConnectionStringTypeButConnectionStringIsEmpty()
    {
        // Arrange
        var command = new UpdateConnectionCommand(
            1,
            "Test Connection",
            new ConnectionConfigDto(
                ConnectionType.ConnectionString,
                string.Empty,
                null,
                null,
                null));

        // Act
        var result = await _sut.ValidateAsync(command, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ErrorCode.ShouldBe("NotEmptyValidator");
        result.Errors[0].PropertyName.ShouldBe("ConnectionConfig.ConnectionString");
    }

    [Fact]
    public async Task FailValidation_WhenConnectionStringTypeButConnectionStringIsNull()
    {
        // Arrange
        var command = new UpdateConnectionCommand(
            1,
            "Test Connection",
            new ConnectionConfigDto(
                ConnectionType.ConnectionString,
                null,
                null,
                null,
                null));

        // Act
        var result = await _sut.ValidateAsync(command, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ErrorCode.ShouldBe("NotEmptyValidator");
        result.Errors[0].PropertyName.ShouldBe("ConnectionConfig.ConnectionString");
    }

    [Fact]
    public async Task FailValidation_WhenEntraIdTypeButNamespaceIsEmpty()
    {
        // Arrange
        var command = new UpdateConnectionCommand(
            1,
            "Test Connection",
            new ConnectionConfigDto(
                ConnectionType.EntraId,
                null,
                string.Empty,
                "tenant-id",
                "client-id"));

        // Act
        var result = await _sut.ValidateAsync(command, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ErrorCode.ShouldBe("NotEmptyValidator");
        result.Errors[0].PropertyName.ShouldBe("ConnectionConfig.Namespace");
    }

    [Fact]
    public async Task FailValidation_WhenEntraIdTypeButNamespaceIsNull()
    {
        // Arrange
        var command = new UpdateConnectionCommand(
            1,
            "Test Connection",
            new ConnectionConfigDto(
                ConnectionType.EntraId,
                null,
                null,
                "tenant-id",
                "client-id"));

        // Act
        var result = await _sut.ValidateAsync(command, CancellationToken.None);

        // Assert
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ErrorCode.ShouldBe("NotEmptyValidator");
        result.Errors[0].PropertyName.ShouldBe("ConnectionConfig.Namespace");
    }

    [Fact]
    public async Task FailValidation_WhenEntraIdTypeButTenantIdIsEmpty()
    {
        // Arrange
        var command = new UpdateConnectionCommand(
            1,
            "Test Connection",
            new ConnectionConfigDto(
                ConnectionType.EntraId,
                null,
                "namespace.servicebus.windows.net",
                string.Empty,
                "client-id"));

        // Act
        var result = await _sut.ValidateAsync(command, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ErrorCode.ShouldBe("NotEmptyValidator");
        result.Errors[0].PropertyName.ShouldBe("ConnectionConfig.TenantId");
    }

    [Fact]
    public async Task FailValidation_WhenEntraIdTypeButTenantIdIsNull()
    {
        // Arrange
        var command = new UpdateConnectionCommand(
            1,
            "Test Connection",
            new ConnectionConfigDto(
                ConnectionType.EntraId,
                null,
                "namespace.servicebus.windows.net",
                null,
                "client-id"));

        // Act
        var result = await _sut.ValidateAsync(command, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ErrorCode.ShouldBe("NotEmptyValidator");
        result.Errors[0].PropertyName.ShouldBe("ConnectionConfig.TenantId");
    }

    [Fact]
    public async Task FailValidation_WhenEntraIdTypeButClientIdIsEmpty()
    {
        // Arrange
        var command = new UpdateConnectionCommand(
            1,
            "Test Connection",
            new ConnectionConfigDto(
                ConnectionType.EntraId,
                null,
                "namespace.servicebus.windows.net",
                "tenant-id",
                string.Empty));

        // Act
        var result = await _sut.ValidateAsync(command, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ErrorCode.ShouldBe("NotEmptyValidator");
        result.Errors[0].PropertyName.ShouldBe("ConnectionConfig.ClientId");
    }

    [Fact]
    public async Task FailValidation_WhenEntraIdTypeButClientIdIsNull()
    {
        // Arrange
        var command = new UpdateConnectionCommand(
            1,
            "Test Connection",
            new ConnectionConfigDto(
                ConnectionType.EntraId,
                null,
                "namespace.servicebus.windows.net",
                "tenant-id",
                null));

        // Act
        var result = await _sut.ValidateAsync(command, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ErrorCode.ShouldBe("NotEmptyValidator");
        result.Errors[0].PropertyName.ShouldBe("ConnectionConfig.ClientId");
    }

    [Fact]
    public async Task FailValidation_WhenEntraIdTypeButAllFieldsAreMissing()
    {
        // Arrange
        var command = new UpdateConnectionCommand(
            1,
            "Test Connection",
            new ConnectionConfigDto(
                ConnectionType.EntraId,
                null,
                null,
                null,
                null));

        // Act
        var result = await _sut.ValidateAsync(command, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(3);
        result.Errors.ShouldContain(e => e.PropertyName == "ConnectionConfig.Namespace");
        result.Errors.ShouldContain(e => e.PropertyName == "ConnectionConfig.TenantId");
        result.Errors.ShouldContain(e => e.PropertyName == "ConnectionConfig.ClientId");
    }

    [Fact]
    public async Task NotValidateEntraIdFields_WhenConnectionTypeIsConnectionString()
    {
        // Arrange
        var command = new UpdateConnectionCommand(
            1,
            "Test Connection",
            new ConnectionConfigDto(
                ConnectionType.ConnectionString,
                "Endpoint=test",
                null,
                null,
                null));

        // Act
        var result = await _sut.ValidateAsync(command, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task NotValidateConnectionString_WhenConnectionTypeIsEntraId()
    {
        // Arrange
        var command = new UpdateConnectionCommand(
            1,
            "Test Connection",
            new ConnectionConfigDto(
                ConnectionType.EntraId,
                null,
                "namespace.servicebus.windows.net",
                "tenant-id",
                "client-id"));

        // Act
        var result = await _sut.ValidateAsync(command, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }
}