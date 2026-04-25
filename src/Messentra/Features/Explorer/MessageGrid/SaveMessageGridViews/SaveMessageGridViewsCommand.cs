using Mediator;

namespace Messentra.Features.Explorer.MessageGrid.SaveMessageGridViews;

public sealed record SaveMessageGridViewsCommand(
    IReadOnlyList<ColumnView> UserViews,
    string ActiveViewId) : ICommand;
