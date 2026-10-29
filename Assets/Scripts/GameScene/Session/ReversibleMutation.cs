using System;
using System.Collections.Generic;

// A local change set. Server-authoritative operations and pending rewards are intentionally separate.
public sealed class ReversibleMutation : IDisposable
{
    private readonly List<Action> undo = new List<Action>();
    private bool completed;
    public void Apply(Action change, Action reverse)
    {
        if (completed) throw new InvalidOperationException("Mutation is already complete.");
        undo.Add(reverse);
        change();
    }
    public void Set<T>(Func<T> read, Action<T> write, T value)
    {
        T before = read();
        Apply(() => write(value), () => write(before));
    }
    public void Commit() { completed = true; undo.Clear(); }
    public void Rollback()
    {
        if (completed) return;
        completed = true;
        try { for (int i = undo.Count - 1; i >= 0; i--) undo[i](); }
        finally { undo.Clear(); }
    }
    public void Dispose() => Rollback();
}
