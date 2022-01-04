using NetTopologySuite.Geometries;
using Xunit;

namespace CoreWms.UnitTests;

public class GridCellTests
{
    [Fact]
    public void BasicTest()
    {
        var p = new GridCell() {
            Bbox = new Envelope(0, 100, 0, 100),
            Width = 100,
            Height = 100
        };

        var parts = p.Split(1);

        Assert.Equal(2, parts.Length);
        Assert.Equal(50, parts[0].Height);
    }

    [Fact]
    public void WiderTest()
    {
        var p = new GridCell() {
            Bbox = new Envelope(0, 100, 0, 100),
            Width = 120,
            Height = 100
        };

        var parts = p.Split(1);

        Assert.Equal(2, parts.Length);
        Assert.Equal(60, parts[0].Width);
        Assert.Equal(50, parts[0].Bbox.Width);
    }

    [Fact]
    public void FourPartsTest()
    {
        var p = new GridCell() {
            Bbox = new Envelope(0, 100, 0, 100),
            Width = 120,
            Height = 100
        };

        var parts = p.Split(2);

        Assert.Equal(4, parts.Length);
        Assert.Equal(60, parts[0].Width);
        Assert.Equal(50, parts[0].Bbox.Width);
        Assert.Equal(50, parts[1].Bbox.Width);
        Assert.Equal(50, parts[2].Bbox.Width);
    }

    [Fact]
    public void EightPartsTest()
    {
        var p = new GridCell() {
            Bbox = new Envelope(0, 100, 0, 100),
            Width = 120,
            Height = 100
        };

        var parts = p.Split(3);

        Assert.Equal(8, parts.Length);
        Assert.Equal(30, parts[0].Width);
        Assert.Equal(25, parts[0].Bbox.Width);
        Assert.Equal(25, parts[1].Bbox.Width);
        Assert.Equal(25, parts[2].Bbox.Width);
        Assert.Equal(25, parts[3].Bbox.Width);
        Assert.Equal(25, parts[4].Bbox.Width);
        Assert.Equal(25, parts[5].Bbox.Width);
    }

    [Fact]
    public void SixteenPartsTest()
    {
        var p = new GridCell() {
            Bbox = new Envelope(0, 100, 0, 100),
            Width = 120,
            Height = 100
        };

        var parts = p.Split(4);

        Assert.Equal(16, parts.Length);
    }
}