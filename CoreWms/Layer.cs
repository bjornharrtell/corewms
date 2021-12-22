#nullable disable
using CoreWms.DataSource;
using SkiaSharp;

namespace CoreWms;

/*public readonly struct EqualsTo
{
    public string PropertyName { get; init; }
    public object Literal { get; init; }
}*/

public readonly struct Symbolizer
{
    public Option<SKPaint> Fill { get; init; }
    public Option<SKPaint> Stroke { get; init; }
}

public readonly struct Rule
{
    //public EqualsTo[] Filters { get; init; }
    public Symbolizer[] Symbolizers { get; init; }
}

public struct Layer
{
    public string Name { get; init; }
    public string Title { get; init; }
    public string Schema { get; init; }
    public string Table { get; init; }
    public string Where { get; init; }
    public double[] Extent { get; init; }
    public Type GeometryType { get; init; }
    public Rule[] Rules { get; init; }
    public IDataSource DataSource { get; set; }
}
