using Bunit;
using Messentra.Features.Settings.FetchOptions.Components;
using Messentra.Features.Settings.FetchOptions.SaveFetchOptions;
using Messentra.Features.Settings.UserSettings.GetUserSettings;
using Microsoft.AspNetCore.Components;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.ComponentTests.Features.Settings.FetchOptions;

public sealed class FetchOptionsComponentShould : ComponentTestBase
{
    public FetchOptionsComponentShould()
    {
        MockMediator
            .Setup(m => m.Send(It.IsAny<GetUserSettingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettingsDto(IsDarkMode: false, IsMcpEnabled: false, DefaultMessageCount: 200));
    }

    [Fact]
    public async Task PrePopulateFieldWithSavedDefaultMessageCount()
    {
        // Act
        var cut = Render<FetchOptionsComponent>();
        await cut.InvokeAsync(() => { });

        // Assert
        var input = cut.Find("input[inputmode='numeric']");
        input.GetAttribute("value").ShouldBe("200");
    }

    [Fact]
    public async Task SaveDefaultMessageCountWhenValueChanges()
    {
        // Arrange
        var cut = Render<FetchOptionsComponent>();
        await cut.InvokeAsync(() => { });

        // Act
        var input = cut.Find("input[inputmode='numeric']");
        await input.InputAsync(new ChangeEventArgs { Value = "75" });

        // Assert
        await cut.WaitForAssertionAsync(() =>
            MockMediator.Verify(
                m => m.Send(new SaveFetchOptionsCommand(DefaultMessageCount: 75), It.IsAny<CancellationToken>()),
                Times.Once));
    }


}
