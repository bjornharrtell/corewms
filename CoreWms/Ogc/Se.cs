#nullable disable
using System.Xml.Serialization;
using CoreWms.Ogc.Fes;

namespace CoreWms.Ogc.Se;

public class SvgParameter
{
    [XmlAttribute]
    public string name;
    [XmlTextAttribute]
    public string Text;
}

public class Mark
{
    public string WellKnownName;
    public Stroke Stroke;
}

public class Graphic
{
    [XmlElement]
    public List<Mark> Mark;
    public float Size;
}

public class GraphicFill
{
    public Graphic Graphic;
}

public class Fill
{
    public GraphicFill GraphicFill;
}

public class Stroke
{
    [XmlElement()]
    public List<SvgParameter> SvgParameter;
}

public class LineSymbolizer : Symbolizer
{

}

public class PolygonSymbolizer : Symbolizer
{

}

public class Symbolizer
{
    public Stroke Stroke;
    public Fill Fill;
}

public class Rule
{
    [XmlElement("LineSymbolizer", Type = typeof(LineSymbolizer))]
    [XmlElement("PolygonSymbolizer", Type = typeof(PolygonSymbolizer))]
    public List<Symbolizer> Symbolizer;
    [XmlElement(Namespace = "http://www.opengis.net/ogc")]
    public List<Filter> Filter;
}

public class FeatureTypeStyle
{
    [XmlElement]
    public List<Rule> Rule;
}
