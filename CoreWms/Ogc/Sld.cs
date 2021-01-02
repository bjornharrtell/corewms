#nullable disable
using System.Collections.Generic;
using System.Xml.Serialization;
using CoreWms.Ogc.Se;

namespace CoreWms.Ogc.Sld
{
    public class UserStyle
    {
        [XmlElement(Namespace = "http://www.opengis.net/se")]
        public List<FeatureTypeStyle> FeatureTypeStyle;
    }

    public class NamedLayer
    {
        [XmlElement]
        public List<UserStyle> UserStyle;
    }

    [XmlRoot(Namespace = "http://www.opengis.net/sld")]
    public class StyledLayerDescriptor
    {
        [XmlElement]
        public List<NamedLayer> NamedLayer;
    }
}