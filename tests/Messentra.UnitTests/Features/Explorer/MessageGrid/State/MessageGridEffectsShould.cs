using Fluxor;
using Mediator;
using Messentra.Features.Explorer.MessageGrid;
using Messentra.Features.Explorer.MessageGrid.SaveMessageGridViews;
using Messentra.Features.Explorer.MessageGrid.State;
using Moq;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Explorer.MessageGrid.State;

public sealed class MessageGridEffectsShould
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IDispatcher> _dispatcher = new();
    private readonly Mock<IState<MessageGridState>> _state = new();
    private readonly MessageGridEffects _sut;

    public MessageGridEffectsShould()
    {
        var defaultView = DefaultColumns.DefaultView;
        _state.Setup(s => s.Value).Returns(new MessageGridState(
            Views: [defaultView],
            ActiveViewId: defaultView.Id,
            Columns: defaultView.Columns));
        _sut = new MessageGridEffects(_mediator.Object, _state.Object);
    }

    [Fact]
    public async Task SaveUserViewsAndDispatchSavedActionOnSaveCurrentView()
    {
        await _sut.HandleSaveCurrentView(_dispatcher.Object);

        _mediator.Verify(
            m => m.Send(It.Is<SaveMessageGridViewsCommand>(c => c.ActiveViewId == "default"), It.IsAny<CancellationToken>()),
            Times.Once);
        _dispatcher.Verify(d => d.Dispatch(new MessageGridViewsSavedAction()), Times.Once);
    }

    [Fact]
    public async Task SaveOnSaveViewAs()
    {
        await _sut.HandleSaveViewAs(_dispatcher.Object);

        _mediator.Verify(m => m.Send(It.IsAny<SaveMessageGridViewsCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        _dispatcher.Verify(d => d.Dispatch(new MessageGridViewsSavedAction()), Times.Once);
    }

    [Fact]
    public async Task SaveOnDeleteView()
    {
        await _sut.HandleDeleteView(_dispatcher.Object);

        _mediator.Verify(m => m.Send(It.IsAny<SaveMessageGridViewsCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        _dispatcher.Verify(d => d.Dispatch(new MessageGridViewsSavedAction()), Times.Once);
    }

    [Fact]
    public async Task SilentlyIgnoreSaveFailure()
    {
        _mediator
            .Setup(m => m.Send(It.IsAny<SaveMessageGridViewsCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));

        // Should not throw
        await _sut.HandleSaveCurrentView(_dispatcher.Object);
    }

    [Fact]
    public async Task ExcludeBuiltInViewsFromSavePayload()
    {
        await _sut.HandleSaveCurrentView(_dispatcher.Object);

        _mediator.Verify(
            m => m.Send(
                It.Is<SaveMessageGridViewsCommand>(c => c.UserViews.All(v => !v.IsBuiltIn)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
