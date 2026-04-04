using Mediator;

namespace Messentra.Features.Settings.UserSettings.SaveUserSettings;

public sealed record SaveUserSettingsCommand(bool IsDarkMode) : ICommand;
