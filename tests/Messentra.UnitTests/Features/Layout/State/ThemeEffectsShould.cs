using Fluxor;
using Mediator;
using Messentra.Features.Layout.State;
using Messentra.Features.Settings.UserSettings.SaveUserSettings;
using Moq;
using Xunit;

namespace Messentra.UnitTests.Features.Layout.State;

public sealed class ThemeEffectsShould
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IDispatcher> _dispatcher = new();
    private readonly Mock<IState<ThemeState>> _themeState = new();
    private readonly ThemeEffects _sut;

    public ThemeEffectsShould()
    {
        _themeState.Setup(s => s.Value).Returns(new ThemeState(IsDarkMode: false));
        _sut = new ThemeEffects(_mediator.Object, _themeState.Object);
    }

    [Fact]
    public async Task SaveCurrentThemeStateOnToggle()
    {
        // Arrange
        _themeState.Setup(s => s.Value).Returns(new ThemeState(IsDarkMode: true));

        // Act
        await _sut.HandleToggleTheme(_dispatcher.Object);

        // Assert
        _mediator.Verify(
            m => m.Send(It.Is<SaveUserSettingsCommand>(c => c.IsDarkMode == true), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SilentlyIgnoreFailureOnToggle()
    {
        // Arrange
        _mediator
            .Setup(m => m.Send(It.IsAny<SaveUserSettingsCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());

        // Act — should not throw
        await _sut.HandleToggleTheme(_dispatcher.Object);
    }
}
