using Mediator;
using Messentra.Infrastructure.Database;

namespace Messentra.Features.Settings.UserSettings.SaveUserSettings;

public sealed class SaveUserSettingsCommandHandler(MessentraDbContext db)
    : ICommandHandler<SaveUserSettingsCommand>
{
    public async ValueTask<Unit> Handle(SaveUserSettingsCommand command, CancellationToken cancellationToken)
    {
        var settings = await db.Set<Domain.UserSettings>().FindAsync([1L], cancellationToken);

        if (settings is null)
        {
            db.Set<Domain.UserSettings>().Add(new Domain.UserSettings { Id = 1, IsDarkMode = command.IsDarkMode });
        }
        else
        {
            settings.IsDarkMode = command.IsDarkMode;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
