using System;

namespace Cubist.Germanium.CSharp;

public struct Scope : IDisposable
{
    private Action _action;

    public static Scope Empty { get; } = new();

    public static Scope Create(Action action) => new(action);

    private Scope(Action action) => _action = action;

    public void Dispose()
    {
        _action?.Invoke();
        _action = null;
    }
}
