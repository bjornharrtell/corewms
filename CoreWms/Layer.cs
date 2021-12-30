using CoreWms.DataSource;
using CoreWms.Ogc.Fes;
using SkiaSharp;

namespace CoreWms;

public readonly struct Symbolizer
{
    public SKPaint? Fill { get; init; }
    public SKPaint? Stroke { get; init; }
}

public readonly struct Style
{
    public Rule[] Rules { get; init; }
}

public readonly struct Rule
{
    public Filter? Filter { get; init; }
    public Symbolizer[] Symbolizers { get; init; }
    public double? MinResolution { get; init; }
    public double? MaxResolution { get; init; }
}

public struct Layer
{
    public string Name { get; init; }
    public string Title { get; init; }
    public string Schema { get; init; }
    public string Table { get; init; }
    public double[] Extent { get; init; }
    public Type GeometryType { get; init; }
    public Style[] Styles { get; init; }
    public IDataSource DataSource { get; set; }
    public double? MaxResolution { get; set; }
}
