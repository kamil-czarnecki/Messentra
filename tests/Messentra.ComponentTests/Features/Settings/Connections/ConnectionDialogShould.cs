using AngleSharp.Dom;
using Bunit;
using Messentra.Domain;
using Messentra.Features.Settings.Connections.Components;
using Messentra.Features.Settings.Connections.GetConnections;
using MudBlazor;
using MudBlazor.Extensions;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Settings.Connections;

public sealed class ConnectionDialogShould : ComponentTestBase
{
    [Fact]
    public void RenderAddDialogWithDefaultValues()
    {
        // Arrange & Act
        var cut = RenderDialog<ConnectionDialog>();
        
        // Assert
        cut.Markup.ShouldContain("Connection Name");
        cut.Markup.ShouldContain("Connection Type");
        cut.Markup.ShouldContain("Add");
        cut.Markup.ShouldNotContain("Update");
        cut.Find("button:contains('Add')").ShouldNotBeNull();
        cut.Find("button:contains('Cancel')").ShouldNotBeNull();
    }

    [Fact]
    public void RenderEditDialogWithConnectionStringType()
    {
        // Arrange
        var connection = new ConnectionDto(
            Id: 1,
            Name: "Test Connection",
            ConnectionConfig: new ConnectionConfigDto(
                ConnectionType.ConnectionString,
                "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey",
                null,
                null,
                null));

        // Act
        var cut = RenderDialog<ConnectionDialog>(parameters =>
            {
                parameters["IsEdit"] = true;
                parameters["ExistingConnection"] = connection;
            });

        // Assert
        cut.Markup.ShouldContain("Test Connection");
        cut.Markup.ShouldContain("Update");
        cut.Markup.ShouldNotContain("Add");
        cut.Markup.ShouldContain("Connection String");
    }

    [Fact]
    public void RenderEditDialogWithEntraIdType()
    {
        // Arrange
        var connection = new ConnectionDto(
            Id: 2,
            Name: "EntraId Connection",
            ConnectionConfig: new ConnectionConfigDto(
                ConnectionType.EntraId,
                null,
                "testnamespace.servicebus.windows.net",
                "tenant-id-123",
                "client-id-456"));

        // Act
        var cut = RenderDialog<ConnectionDialog>(parameters =>
        {
            parameters["IsEdit"] = true;
            parameters["ExistingConnection"] = connection;
        });

        // Assert
        cut.Markup.ShouldContain("EntraId Connection");
        cut.Markup.ShouldContain("Update");
        cut.Markup.ShouldContain("Namespace");
        cut.Markup.ShouldContain("Tenant ID");
        cut.Markup.ShouldContain("Client ID");
    }

    [Fact]
    public void ShowConnectionStringFieldsWhenConnectionStringTypeSelected()
    {
        // Arrange & Act
        var cut = RenderDialog<ConnectionDialog>();

        // Assert
        cut.Markup.ShouldContain("Connection String");
        cut.Markup.ShouldNotContain("Namespace");
        cut.Markup.ShouldNotContain("Tenant ID");
        cut.Markup.ShouldNotContain("Client ID");
    }

    [Fact]
    public void ShowEntraIdFieldsWhenEntraIdTypeSelected()
    {
        // Arrange
        var cut = RenderDialog<ConnectionDialog>();

        // Act
        var select = cut.FindComponent<MudSelect<ConnectionType>>();
        cut.InvokeAsync(async () => await select.Instance.SelectOption(ConnectionType.EntraId));
        cut.Render();

        // Assert
        cut.Markup.ShouldContain("Namespace");
        cut.Markup.ShouldContain("Tenant ID");
        cut.Markup.ShouldContain("Client ID");
        cut.Markup.ShouldNotContain("Connection String");
    }

    [Fact]
    public void SwitchFromConnectionStringToEntraIdShowsCorrectFields()
    {
        // Arrange
        var cut = RenderDialog<ConnectionDialog>();

        // Act - Start with ConnectionString (default)
        cut.Markup.ShouldContain("Connection String");

        // Switch to EntraId
        var select = cut.FindComponent<MudSelect<ConnectionType>>();
        cut.InvokeAsync(async () => await select.Instance.SelectOption(ConnectionType.EntraId));
        cut.Render();

        // Assert
        cut.Markup.ShouldContain("Namespace");
        cut.Markup.ShouldContain("Tenant ID");
        cut.Markup.ShouldContain("Client ID");
        cut.Markup.ShouldNotContain("label>Connection String");
    }

    [Fact]
    public void SwitchFromEntraIdToConnectionStringShowsCorrectFields()
    {
        // Arrange
        var connection = new ConnectionDto(
            Id: 1,
            Name: "Test",
            ConnectionConfig: new ConnectionConfigDto(
                ConnectionType.EntraId,
                null,
                "namespace",
                "tenant",
                "client"));

        // Act
        var cut = RenderDialog<ConnectionDialog>(parameters =>
        {
            parameters["IsEdit"] = true;
            parameters["ExistingConnection"] = connection;
        });

        // Start with EntraId
        cut.Markup.ShouldContain("Namespace");

        // Switch to ConnectionString
        var select = cut.FindComponent<MudSelect<ConnectionType>>();
        cut.InvokeAsync(async () => await select.Instance.SelectOption(ConnectionType.ConnectionString));

        // Assert
        cut.Markup.ShouldContain("Connection String");
        cut.Markup.ShouldNotContain("Namespace");
        cut.Markup.ShouldNotContain("Tenant ID");
        cut.Markup.ShouldNotContain("Client ID");
    }

    [Fact]
    public void DisableSubmitButtonWhenFormIsInvalid()
    {
        // Arrange & Act
        var cut = RenderDialog<ConnectionDialog>();

        // Assert
        var addButton = cut.Find("button:contains('Add')");
        addButton.HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void EnableSubmitButtonWhenFormIsValid()
    {
        // Arrange
        var cut = RenderDialog<ConnectionDialog>();
        var nameField = cut.Find("input[id='ConnectionName']");
        var connectionStringField = cut.Find("textarea[id='ConnectionString']");

        // Act
        nameField.Input("Valid Name");
        connectionStringField.Input("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey");
        
        // Assert
        var addButton = cut.Find("button:contains('Add')");
        cut.WaitForAssertion(() =>
        {
            addButton
                .IsDisabled()
                .ShouldBeFalse();
        });
    }

    [Fact]
    public void AcceptValidConnectionString()
    {
        // Arrange
        var cut = RenderDialog<ConnectionDialog>();
        var nameField = cut.Find("input[id='ConnectionName']");
        var connectionStringField = cut.Find("textarea[id='ConnectionString']");

        // Act
        nameField.Input("Test Connection");
        connectionStringField.Input("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey");

        // Assert
        connectionStringField.IsValid().ShouldBeTrue();
        cut.Markup.ShouldNotContain("Incorrect ConnectionString");
    }

    [Fact]
    public void RequireNameField()
    {
        // Arrange
        var cut = RenderDialog<ConnectionDialog>();
        var nameField = cut.Find("input[id='ConnectionName']");

        // Act
        nameField.Input(string.Empty);
        nameField.Blur();

        // Assert
        nameField.IsValid().ShouldBeFalse();
        cut.Markup.ShouldContain("Connection name is required");
    }

    [Fact]
    public void RequireConnectionStringFieldForConnectionStringType()
    {
        // Arrange
        var cut = RenderDialog<ConnectionDialog>();
        var connectionStringField = cut.Find("textarea[id='ConnectionString']");

        // Act
        connectionStringField.Input(string.Empty);
        connectionStringField.Blur();

        // Assert
        connectionStringField.IsValid().ShouldBeFalse();
        cut.Markup.ShouldContain("Connection string is required");
    }

    [Fact]
    public async Task RequireNamespaceFieldForEntraIdType()
    {
        // Arrange
        var cut = RenderDialog<ConnectionDialog>();
        var connectionTypeSelect = cut
            .FindComponents<MudSelect<ConnectionType>>()
            .First(c => c.Instance.GetState(x => x.InputId) == "ConnectionType");
        await cut.InvokeAsync(async () => await connectionTypeSelect.Instance.SelectOption(ConnectionType.EntraId));
        var namespaceField = cut.Find("input[id='Namespace']");

        // Act
        await namespaceField.InputAsync(string.Empty);
        await namespaceField.BlurAsync();

        // Assert
        namespaceField.IsValid().ShouldBeFalse();
        cut.Markup.ShouldContain("Namespace is required");
    }

    [Fact]
    public async Task RequireTenantIdFieldForEntraIdType()
    {
        // Arrange
        var cut = RenderDialog<ConnectionDialog>();
        var connectionTypeSelect = cut
            .FindComponents<MudSelect<ConnectionType>>()
            .First(c => c.Instance.GetState(x => x.InputId) == "ConnectionType");
        await cut.InvokeAsync(async () => await connectionTypeSelect.Instance.SelectOption(ConnectionType.EntraId));
        var tenantIdField = cut.Find("input[id='TenantId']");

        // Act
        await tenantIdField.InputAsync(string.Empty);
        await tenantIdField.BlurAsync();

        // Assert
        tenantIdField.IsValid().ShouldBeFalse();
        cut.Markup.ShouldContain("Tenant ID is required");
    }

    [Fact]
    public async Task RequireClientIdFieldForEntraIdType()
    {
        // Arrange
        var cut = RenderDialog<ConnectionDialog>();
        var connectionTypeSelect = cut
            .FindComponents<MudSelect<ConnectionType>>()
            .First(c => c.Instance.GetState(x => x.InputId) == "ConnectionType");
        await cut.InvokeAsync(async () => await connectionTypeSelect.Instance.SelectOption(ConnectionType.EntraId));
        var clientIdField = cut.Find("input[id='ClientId']");

        // Act
        await clientIdField.InputAsync(string.Empty);
        await clientIdField.BlurAsync();

        // Assert
        clientIdField.IsValid().ShouldBeFalse();
        cut.Markup.ShouldContain("Client ID is required");
    }

    [Fact]
    public void ShowErrorWhenConnectionNameIsDuplicate()
    {
        // Arrange
        var existing = new ConnectionDto(1, "My Connection", new ConnectionConfigDto(ConnectionType.ConnectionString, null, null, null, null));
        var cut = RenderDialog<ConnectionDialog>(p => p["ExistingConnections"] = new[] { existing });
        var nameField = cut.Find("input[id='ConnectionName']");

        // Act
        nameField.Input("My Connection");
        nameField.Blur();

        // Assert
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("already exists"));
    }

    [Fact]
    public void ShowErrorWhenConnectionNameIsDuplicateCaseInsensitive()
    {
        // Arrange
        var existing = new ConnectionDto(1, "My Connection", new ConnectionConfigDto(ConnectionType.ConnectionString, null, null, null, null));
        var cut = RenderDialog<ConnectionDialog>(p => p["ExistingConnections"] = new[] { existing });
        var nameField = cut.Find("input[id='ConnectionName']");

        // Act
        nameField.Input("MY CONNECTION");
        nameField.Blur();

        // Assert
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("already exists"));
    }

    [Fact]
    public void AllowEditConnectionToKeepItsOwnName()
    {
        // Arrange
        var connection = new ConnectionDto(1, "My Connection", new ConnectionConfigDto(ConnectionType.ConnectionString, null, null, null, null));
        var cut = RenderDialog<ConnectionDialog>(p =>
        {
            p["IsEdit"] = true;
            p["ExistingConnection"] = connection;
            p["ExistingConnections"] = new[] { connection };
        });
        var nameField = cut.Find("input[id='ConnectionName']");

        // Act
        nameField.Input("My Connection");
        nameField.Blur();

        // Assert — no duplicate error shown
        cut.WaitForAssertion(() => cut.Markup.ShouldNotContain("already exists"));
    }

    [Fact]
    public void ShowErrorWhenEditConnectionTakesAnotherConnectionsName()
    {
        // Arrange
        var connectionA = new ConnectionDto(1, "Connection A", new ConnectionConfigDto(ConnectionType.ConnectionString, null, null, null, null));
        var connectionB = new ConnectionDto(2, "Connection B", new ConnectionConfigDto(ConnectionType.ConnectionString, null, null, null, null));
        var cut = RenderDialog<ConnectionDialog>(p =>
        {
            p["IsEdit"] = true;
            p["ExistingConnection"] = connectionB;
            p["ExistingConnections"] = new[] { connectionA, connectionB };
        });
        var nameField = cut.Find("input[id='ConnectionName']");

        // Act — try to rename B to A's name
        nameField.Input("Connection A");
        nameField.Blur();

        // Assert
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("already exists"));
    }

    [Fact]
    public async Task SubmitValidConnectionStringConnection()
    {
        // Arrange
        const string validConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey";
        var cut = RenderDialog<ConnectionDialog>(out var dialogReference);
        var nameField = cut.Find("input[id='ConnectionName']");
        var connectionStringField = cut.Find("textarea[id='ConnectionString']");
        await nameField.InputAsync("My Connection");
        await connectionStringField.InputAsync(validConnectionString);
        var addButton = cut.Find("button:contains('Add')");
        
       // Act
       await cut.WaitForStateAsync(() => !addButton.IsDisabled());
       await addButton.ClickAsync();
       var result = await dialogReference.Result;
       var connection = result!.Data as ConnectionDto;

        // Assert
        result.ShouldNotBeNull();
        result.Canceled.ShouldBeFalse();
        connection.ShouldNotBeNull();
        connection.Id.ShouldBe(0);
        connection.Name.ShouldBe("My Connection");
        connection.ConnectionConfig.ConnectionType.ShouldBe(ConnectionType.ConnectionString);
        connection.ConnectionConfig.ConnectionString.ShouldBe(validConnectionString);
        connection.ConnectionConfig.Namespace.ShouldBeNullOrEmpty();
        connection.ConnectionConfig.TenantId.ShouldBeNullOrEmpty();
        connection.ConnectionConfig.ClientId.ShouldBeNullOrEmpty();
    }

    [Fact]
    public async Task SubmitValidEntraIdConnection()
    {
        // Arrange
        var cut = RenderDialog<ConnectionDialog>(out var dialogReference);
        var connectionTypeSelect = cut
            .FindComponents<MudSelect<ConnectionType>>()
            .First(c => c.Instance.GetState(x => x.InputId) == "ConnectionType");
        await cut.InvokeAsync(async () => await connectionTypeSelect.Instance.SelectOption(ConnectionType.EntraId));
        var nameField = cut.Find("input[id='ConnectionName']");
        var namespaceField = cut.Find("input[id='Namespace']");
        var tenantIdField = cut.Find("input[id='TenantId']");
        var clientIdField = cut.Find("input[id='ClientId']");
        await nameField.InputAsync("EntraId Connection");
        await namespaceField.InputAsync("namespace.servicebus.windows.net");
        await tenantIdField.InputAsync("tenant-123");
        await clientIdField.InputAsync("client-456");
        var addButton = cut.Find("button:contains('Add')");

        // Act
        await cut.WaitForStateAsync(() => !addButton.IsDisabled());
        await addButton.ClickAsync();
        var result = await dialogReference.Result;
        var connection = result!.Data as ConnectionDto;

        // Assert
        result.ShouldNotBeNull();
        result.Canceled.ShouldBeFalse();
        connection.ShouldNotBeNull();
        connection.Id.ShouldBe(0);
        connection.Name.ShouldBe("EntraId Connection");
        connection.ConnectionConfig.ConnectionType.ShouldBe(ConnectionType.EntraId);
        connection.ConnectionConfig.Namespace.ShouldBe("namespace.servicebus.windows.net");
        connection.ConnectionConfig.TenantId.ShouldBe("tenant-123");
        connection.ConnectionConfig.ClientId.ShouldBe("client-456");
        connection.ConnectionConfig.ConnectionString.ShouldBeNullOrEmpty();
    }
}

