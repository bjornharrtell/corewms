namespace CoreWms;

public sealed class DisposableList<T> : List<T>, IDisposable
    where T : IDisposable
{
    private readonly bool leaveFirstOpen;

    public DisposableList(IEnumerable<T> e, bool leaveFirstOpen = false) : base(e)
    {
        this.leaveFirstOpen = leaveFirstOpen;
    }

    public void Dispose()
    {
        for (int i = leaveFirstOpen ? 1 : 0; i < Count; i++)
            this[i].Dispose();
    }
}