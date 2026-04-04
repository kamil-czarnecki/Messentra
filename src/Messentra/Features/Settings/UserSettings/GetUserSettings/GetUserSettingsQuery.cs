using Mediator;

namespace Messentra.Features.Settings.UserSettings.GetUserSettings;

public sealed record GetUserSettingsQuery : IQuery<UserSettingsDto>;

public sealed record UserSettingsDto(bool IsDarkMode);
