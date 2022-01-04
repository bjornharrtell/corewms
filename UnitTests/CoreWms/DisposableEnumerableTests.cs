using Xunit;

namespace CoreWms.UnitTests;

public class DisposableEnumerableTests
{
    class Disposable : IDisposable
    {
        bool isDisposed = false;

        public bool IsDisposed { get => isDisposed; set => isDisposed = value; }

        public void Dispose()
        {
            isDisposed = true;
        }
    }

    [Fact]
    public void BasicTest()
    {
        var it = new DisposableEnumerable<Disposable>(new Disposable[] { new Disposable() });

        var c = 0;
        foreach (var e in it) {
            e.Dispose();
            c++;
        }
        Assert.Equal(1, c);
        Assert.True(it.First().IsDisposed);
    }
}
