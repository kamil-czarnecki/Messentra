using Fluxor;

namespace Messentra.ComponentTests;

public sealed class TestState<T> : IState<T>
{
    public T Value { get; private set; }
    public event EventHandler? StateChanged;

    public TestState(T value)
    {
        Value = value;
    }

    public void SetState(T newValue)
    {
        Value = newValue;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}