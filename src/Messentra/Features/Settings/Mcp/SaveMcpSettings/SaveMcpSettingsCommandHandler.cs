using Mediator;
using Messentra.Infrastructure.Database;

namespace Messentra.Features.Settings.Mcp.SaveMcpSettings;

public sealed class SaveMcpSettingsCommandHandler(MessentraDbContext db)
    : ICommandHandler<SaveMcpSettingsCommand>
{
    public async ValueTask<Unit> Handle(SaveMcpSettingsCommand command, CancellationToken cancellationToken)
    {
        var settings = await db.Set<Domain.UserSettings>().FindAsync([1L], cancellationToken);

        if (settings is null)
        {
            db.Set<Domain.UserSettings>().Add(new Domain.UserSettings { Id = 1, IsMcpEnabled = command.IsMcpEnabled });
        }
        else
        {
            settings.IsMcpEnabled = command.IsMcpEnabled;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
