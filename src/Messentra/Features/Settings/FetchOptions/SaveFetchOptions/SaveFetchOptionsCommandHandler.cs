using Mediator;
using Messentra.Infrastructure.Database;

namespace Messentra.Features.Settings.FetchOptions.SaveFetchOptions;

public sealed class SaveFetchOptionsCommandHandler(MessentraDbContext db)
    : ICommandHandler<SaveFetchOptionsCommand>
{
    public async ValueTask<Unit> Handle(SaveFetchOptionsCommand command, CancellationToken cancellationToken)
    {
        var settings = await db.Set<Domain.UserSettings>().FindAsync([1L], cancellationToken);

        if (settings is null)
        {
            db.Set<Domain.UserSettings>().Add(
                new Domain.UserSettings { Id = 1, DefaultMessageCount = command.DefaultMessageCount });
        }
        else
        {
            settings.DefaultMessageCount = command.DefaultMessageCount;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
