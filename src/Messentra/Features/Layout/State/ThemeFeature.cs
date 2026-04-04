using Fluxor;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Layout.State;

public sealed class ThemeFeature(IDbContextFactory<MessentraDbContext> dbFactory) : Feature<ThemeState>
{
    public override string GetName() => "Theme";

    protected override ThemeState GetInitialState()
    {
        using var db = dbFactory.CreateDbContext();
        var settings = db.Set<Domain.UserSettings>().Find(1L);
        return new ThemeState(IsDarkMode: settings?.IsDarkMode ?? false);
    }
}
