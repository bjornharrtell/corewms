using System.Xml.Serialization;
using CoreWms.Ogc.Se;

namespace CoreWms.Ogc.Sld;

public readonly struct UserStyle
{
    [XmlElement]
    public readonly FeatureTypeStyle[] FeatureTypeStyle { get; init; }
}

public readonly struct NamedLayer
{
    [XmlElement]
    public readonly UserStyle[] UserStyle { get; init; }
}

[XmlRoot(Namespace = "http://www.opengis.net/sld")]
public readonly struct StyledLayerDescriptor
{
    [XmlElement]
    public readonly NamedLayer[] NamedLayer { get; init; }
}
