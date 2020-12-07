using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace CoreWms.Ogc.Wms
{
    public class BoundingBox
    {
        [XmlAttribute]
        public string CRS;
        [XmlAttribute]
        public float minx;
        [XmlAttribute]
        public float miny;
        [XmlAttribute]
        public float maxx;
        [XmlAttribute]
        public float maxy;
    }

    public class Layer
    {
        public string Name;
        public string Title = "";
        public string Abstract = "";
        [XmlElement]
        public List<string> CRS = new List<string>();
        [XmlElement("Layer")]
        public List<Layer> LayerChildren = new List<Layer>();
        [XmlElement]
        public List<BoundingBox> BoundingBox = new List<BoundingBox>();
    }

    public class OnlineResource
    {
        [XmlAttribute(Namespace = "http://www.w3.org/1999/xlink")]
        public string type = "simple";
        [XmlAttribute(Namespace = "http://www.w3.org/1999/xlink")]
        public string href;
    }

    public class Get
    {
        public OnlineResource OnlineResource = new OnlineResource();
    }

    public class HTTP
    {
        public Get Get = new Get();
    }

    public class DCPType
    {
        public HTTP HTTP = new HTTP();
    }

    public class GetCapabilities
    {
        public string Format = "text/xml";
        public DCPType DCPType = new DCPType();
    }

    public class GetMap
    {
        [XmlElement]
        public List<string> Format = new List<string>()
        {
            "image/png"
        };
        public DCPType DCPType = new DCPType();
    }

    public class Request
    {
        public GetCapabilities GetCapabilities = new GetCapabilities();
        public GetMap GetMap = new GetMap();
    }

    public class Exception
    {
        [XmlElement]
        public List<string> Format = new List<string>()
        {
            "XML"
        };
    }

    public class Capability
    {
        public Request Request = new Request();
        public Exception Exception = new Exception();
        public Layer Layer;
    }

    public class Service
    {
        public string Name = "WMS";
        public string Title = "";
    }

    [XmlRoot("WMS_Capabilities", Namespace = "http://www.opengis.net/wms")]
    public class Capabilities
    {
        [XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns = new XmlSerializerNamespaces(
            new[] { new XmlQualifiedName("xlink", "http://www.w3.org/1999/xlink") });

        public Service Service = new Service();
        public Capability Capability = new Capability();
    }
}