namespace DnDInitiative.App.Services;

/// <summary>
/// Passes progress reports to a handler on the thread this object was created on.
/// </summary>
internal sealed class UiProgress<T> : IProgress<T>
{
    private readonly SynchronizationContext? _context = SynchronizationContext.Current;
    private readonly int _threadId = Environment.CurrentManagedThreadId;
    private readonly Action<T> _handler;

    public UiProgress(Action<T> handler)
    {
        _handler = handler;
    }

    public void Report(T value)
    {
        if (_context is null || Environment.CurrentManagedThreadId == _threadId)
        {
            // Already on the right thread (or there is no "right thread" to go back to).
            _handler(value);
        }
        else
        {
            // Queue the call on the original thread. Post does not wait for it to run.
            _context.Post(_ => _handler(value), null);
        }
    }
}