using Fluxor;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Messentra.Features.Explorer.MessageGrid.State;

public sealed class MessageGridFeature(IDbContextFactory<MessentraDbContext> dbFactory)
    : Feature<MessageGridState>
{
    public override string GetName() => "MessageGrid";

    protected override MessageGridState GetInitialState()
    {
        using var db = dbFactory.CreateDbContext();
        var settings = db.Set<Domain.UserSettings>().Find(1L);

        var userViews = TryDeserialize(settings?.MessageGridViewsJson);
        IReadOnlyList<ColumnView> allViews = [DefaultColumns.DefaultView, ..userViews];

        var activeView = allViews.FirstOrDefault(v => v.Id == settings?.ActiveMessageGridViewId)
            ?? DefaultColumns.DefaultView;

        return new MessageGridState(
            Views: allViews,
            ActiveViewId: activeView.Id,
            Columns: activeView.Columns);
    }

    private static IReadOnlyList<ColumnView> TryDeserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];
        try
        {
            return JsonSerializer.Deserialize<List<ColumnView>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
