using Fluxor;
using Mediator;
using Messentra.Features.Explorer.MessageGrid.SaveMessageGridViews;

namespace Messentra.Features.Explorer.MessageGrid.State;

public sealed class MessageGridEffects(IMediator mediator, IState<MessageGridState> state)
{
    [EffectMethod(typeof(SaveCurrentMessageGridViewAction))]
    public async Task HandleSaveCurrentView(IDispatcher dispatcher)
        => await SaveAndDispatch(dispatcher);

    [EffectMethod(typeof(SaveMessageGridViewAsAction))]
    public async Task HandleSaveViewAs(IDispatcher dispatcher)
        => await SaveAndDispatch(dispatcher);

    [EffectMethod(typeof(DeleteMessageGridViewAction))]
    public async Task HandleDeleteView(IDispatcher dispatcher)
        => await SaveAndDispatch(dispatcher);

    private async Task SaveAndDispatch(IDispatcher dispatcher)
    {
        try
        {
            var userViews = state.Value.Views.Where(v => !v.IsBuiltIn).ToList();
            await mediator.Send(new SaveMessageGridViewsCommand(userViews, state.Value.ActiveViewId));
            dispatcher.Dispatch(new MessageGridViewsSavedAction());
        }
        catch
        {
            // best-effort — silent failure
        }
    }
}
