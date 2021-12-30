using System.Collections;

namespace CoreWms;

public sealed class DisposableEnumerable<T> : IEnumerable<T>, IDisposable
    where T : IDisposable
{
    private readonly bool leaveFirstOpen;
    private readonly IEnumerable<T> e;

    public DisposableEnumerable(T[] e, bool leaveFirstOpen = false)
    {
        this.e = e;
        this.leaveFirstOpen = leaveFirstOpen;
    }

    public void Dispose()
    {
        foreach (var e in leaveFirstOpen ? this.Skip(1) : this)
            e.Dispose();
    }

    public IEnumerator<T> GetEnumerator() => e.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => e.GetEnumerator();
}