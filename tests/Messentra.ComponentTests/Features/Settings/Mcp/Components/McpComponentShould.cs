using Bunit;
using Messentra.Features.Settings.Mcp.Components;
using Messentra.Features.Settings.Mcp.SaveMcpSettings;
using Messentra.Features.Settings.UserSettings.GetUserSettings;
using Moq;
using MudBlazor;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Settings.Mcp.Components;

public sealed class McpComponentShould : ComponentTestBase
{
    public McpComponentShould()
    {
        MockMediator
            .Setup(m => m.Send(It.IsAny<GetUserSettingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettingsDto(IsDarkMode: false, IsMcpEnabled: false, DefaultMessageCount: 100));
    }

    [Fact]
    public void RenderSectionTitle()
    {
        var cut = Render<McpComponent>();

        cut.Markup.ShouldContain("MCP Server");
    }

    [Fact]
    public void RenderDescriptionText()
    {
        var cut = Render<McpComponent>();

        cut.Markup.ShouldContain("Model Context Protocol");
        cut.Markup.ShouldContain("read-only");
        cut.Markup.ShouldContain("Restart");
    }

    [Fact]
    public void RenderToggleAsDisabled_WhenSettingIsFalse()
    {
        var cut = Render<McpComponent>();

        cut.Markup.ShouldContain("Disabled");
        cut.Find("input[type=checkbox]").HasAttribute("checked").ShouldBeFalse();
    }

    [Fact]
    public void RenderToggleAsEnabled_WhenSettingIsTrue()
    {
        MockMediator
            .Setup(m => m.Send(It.IsAny<GetUserSettingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettingsDto(IsDarkMode: false, IsMcpEnabled: true, DefaultMessageCount: 100));

        var cut = Render<McpComponent>();

        cut.Markup.ShouldContain("Enabled");
        cut.Find("input[type=checkbox]").HasAttribute("checked").ShouldBeTrue();
    }

    [Fact]
    public void NotShowEndpoint_WhenDisabled()
    {
        var cut = Render<McpComponent>();

        cut.Markup.ShouldNotContain("/mcp");
    }

    [Fact]
    public void ShowEndpointWithCopyButton_WhenEnabled()
    {
        MockMediator
            .Setup(m => m.Send(It.IsAny<GetUserSettingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettingsDto(IsDarkMode: false, IsMcpEnabled: true, DefaultMessageCount: 100));

        var cut = Render<McpComponent>();

        cut.Markup.ShouldContain("/mcp");
        cut.FindComponents<MudIconButton>().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task SendsSaveMcpSettingsCommand_WhenToggled()
    {
        var cut = Render<McpComponent>();

        await cut.Find("input[type=checkbox]").ChangeAsync(true);

        MockMediator.Verify(
            m => m.Send(It.Is<SaveMcpSettingsCommand>(c => c.IsMcpEnabled == true), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ShowsRestartNotice_AfterToggling()
    {
        var cut = Render<McpComponent>();

        await cut.Find("input[type=checkbox]").ChangeAsync(true);

        cut.Markup.ShouldContain("Restart");
    }

    [Fact]
    public async Task CopyButton_InvokesClipboardWrite()
    {
        MockMediator
            .Setup(m => m.Send(It.IsAny<GetUserSettingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettingsDto(IsDarkMode: false, IsMcpEnabled: true, DefaultMessageCount: 100));

        var cut = Render<McpComponent>();
        var copyButton = cut.FindComponent<MudIconButton>();

        await copyButton.Find("button").ClickAsync();

        var invocations = JSInterop.Invocations["navigator.clipboard.writeText"];
        invocations.Count.ShouldBe(1);
        ((string)invocations[0].Arguments[0]!).ShouldContain("/mcp");
    }

    [Fact]
    public async Task CopyButton_ShowsCheckIcon_AfterCopy()
    {
        MockMediator
            .Setup(m => m.Send(It.IsAny<GetUserSettingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettingsDto(IsDarkMode: false, IsMcpEnabled: true, DefaultMessageCount: 100));

        var cut = Render<McpComponent>();
        var copyButton = cut.FindComponent<MudIconButton>();

        await copyButton.Find("button").ClickAsync();

        cut.FindComponent<MudIconButton>().Instance.Color.ShouldBe(Color.Success);
    }
}
