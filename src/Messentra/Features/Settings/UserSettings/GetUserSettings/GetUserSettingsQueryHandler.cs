using Mediator;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Settings.UserSettings.GetUserSettings;

public sealed class GetUserSettingsQueryHandler(MessentraDbContext db)
    : IQueryHandler<GetUserSettingsQuery, UserSettingsDto>
{
    public async ValueTask<UserSettingsDto> Handle(GetUserSettingsQuery query, CancellationToken cancellationToken)
    {
        var settings = await db.Set<Domain.UserSettings>()
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        return settings is null
            ? new UserSettingsDto(IsDarkMode: false)
            : new UserSettingsDto(IsDarkMode: settings.IsDarkMode);
    }
}
