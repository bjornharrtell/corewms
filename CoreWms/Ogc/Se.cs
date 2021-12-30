using System.Xml.Serialization;
using CoreWms.Ogc.Fes;

namespace CoreWms.Ogc.Se;

public class CssParameter
{
    [XmlAttribute]
    public string? name;
    [XmlText]
    public string? Text;
}

public class Mark
{
    public string? WellKnownName;
    public Stroke? Stroke;
}

public class Graphic
{
    [XmlElement]
    public Mark[]? Mark;
    public float Size;
}

public readonly struct GraphicFill
{
    public Graphic Graphic { get; init; }
}

public class Fill
{
    public GraphicFill? GraphicFill;
    [XmlElement]
    public CssParameter[]? CssParameter;
}

public class Stroke
{
    [XmlElement]
    public CssParameter[]? CssParameter;
}

public class LineSymbolizer : Symbolizer
{

}

public class PolygonSymbolizer : Symbolizer
{

}

public class Symbolizer
{
    public Stroke? Stroke;
    public Fill? Fill;
}

public readonly struct Rule
{
    [XmlElement("LineSymbolizer", Type = typeof(LineSymbolizer))]
    [XmlElement("PolygonSymbolizer", Type = typeof(PolygonSymbolizer))]
    public readonly Symbolizer[] Symbolizer { get; init; }
    public readonly Filter? Filter { get; init; }
    public readonly string? MinScaleDenominator { get; init; }
    public readonly string? MaxScaleDenominator { get; init; }
}

public readonly struct FeatureTypeStyle
{
    [XmlElement]
    public readonly Rule[] Rule { get; init; }
}
