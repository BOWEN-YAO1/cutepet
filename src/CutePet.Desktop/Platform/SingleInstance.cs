using System;
using System.Threading;

namespace CutePet.Desktop;

internal sealed class SingleInstance : IDisposable
{
    private readonly Mutex mutex;
    private readonly EventWaitHandle show;
    private RegisteredWaitHandle? listener;
    public bool IsFirst { get; }
    public SingleInstance(string name)
    {
        mutex = new Mutex(initiallyOwned: true, "Local\\" + name, out var created);
        IsFirst = created;
        show = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\" + name + ".Show");
    }
    public void Listen(Action action) => listener = ThreadPool.RegisterWaitForSingleObject(show,
        (_, _) => action(), null, Timeout.Infinite, executeOnlyOnce: false);
    public void RequestShow() => show.Set();
    public void Dispose()
    {
        listener?.Unregister(null);
        show.Dispose();
        if (IsFirst) mutex.ReleaseMutex();
        mutex.Dispose();
    }
}
