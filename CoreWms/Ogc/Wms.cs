#nullable disable
using System.Xml;
using System.Xml.Serialization;

namespace CoreWms.Ogc.Wms;

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
    public List<string> CRS = new();
    [XmlElement("Layer")]
    public List<Layer> LayerChildren = new();
    [XmlElement]
    public List<BoundingBox> BoundingBox = new();
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
    public OnlineResource OnlineResource = new();
}

public class HTTP
{
    public Get Get = new();
}

public class DCPType
{
    public HTTP HTTP = new();
}

public class GetCapabilities
{
    public string Format = "text/xml";
    public DCPType DCPType = new();
}

public class GetMap
{
    [XmlElement]
    public List<string> Format = new()
    {
        "image/png"
    };
    public DCPType DCPType = new();
}

public class Request
{
    public GetCapabilities GetCapabilities = new();
    public GetMap GetMap = new();
}

public class Exception
{
    [XmlElement]
    public List<string> Format = new()
    {
        "XML"
    };
}

public class Capability
{
    public Request Request = new();
    public Exception Exception = new();
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
    public XmlSerializerNamespaces xmlns = new(
        new[] { new XmlQualifiedName("xlink", "http://www.w3.org/1999/xlink") });

    public Service Service = new();
    public Capability Capability = new();
}

public class ServiceException
{
    [XmlAttribute]
    public string code;

    [XmlText]
    public string Text;
}
public class ServiceExceptionReport
{
    [XmlAttribute]
    public string version;

    [XmlElement]
    public List<ServiceException> ServiceException = new();
}
