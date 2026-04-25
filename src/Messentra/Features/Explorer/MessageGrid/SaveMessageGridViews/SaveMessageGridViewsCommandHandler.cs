using Mediator;
using Messentra.Infrastructure.Database;
using System.Text.Json;

namespace Messentra.Features.Explorer.MessageGrid.SaveMessageGridViews;

public sealed class SaveMessageGridViewsCommandHandler(MessentraDbContext db)
    : ICommandHandler<SaveMessageGridViewsCommand>
{
    public async ValueTask<Unit> Handle(SaveMessageGridViewsCommand command, CancellationToken cancellationToken)
    {
        var settings = await db.Set<Domain.UserSettings>().FindAsync([1L], cancellationToken);

        if (settings is null)
        {
            settings = new Domain.UserSettings { Id = 1 };
            db.Set<Domain.UserSettings>().Add(settings);
        }

        settings.MessageGridViewsJson = JsonSerializer.Serialize(command.UserViews);
        settings.ActiveMessageGridViewId = command.ActiveViewId;

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
