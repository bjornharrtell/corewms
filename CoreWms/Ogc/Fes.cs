#nullable disable
using System.Xml.Serialization;

namespace CoreWms.Ogc.Fes;

public class PropertyName
{
    [XmlText]
    public string Text;
}

public class Literal
{
    [XmlText]
    public string Text;
}

public class PropertyIsEqualTo
{
    public PropertyName PropertyName;
    public Literal Literal;
}

public class Filter
{
    // TODO: make generic
    [XmlElement("PropertyIsEqualTo")]
    public List<PropertyIsEqualTo> ComparisonOps;
}
