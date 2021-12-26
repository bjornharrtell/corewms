using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;

namespace CoreWms;

public class GetCapabilities
{
    private readonly ILogger<GetCapabilities> logger;
    private readonly IContext context;

    private static string? capabilitiesCache;

    public GetCapabilities(ILogger<GetCapabilities> logger, IContext context)
    {
        this.logger = logger;
        this.context = context;

        if (capabilitiesCache == null)
        {
            var serializer = new XmlSerializer(typeof(Ogc.Wms.Capabilities));
            var encoding = new UTF8Encoding(false, true);
            XmlWriterSettings settings = new()
            {
                Encoding = encoding,
                Indent = true,
                OmitXmlDeclaration = false
            };
            using StringWriter textWriter = new StringWriterWithEncoding(encoding);
            using XmlWriter xmlWriter = XmlWriter.Create(textWriter, settings);
            serializer.Serialize(xmlWriter, GenerateCapabilities());
            capabilitiesCache = textWriter.ToString();
        }
    }

    public sealed class StringWriterWithEncoding : StringWriter
    {
        public override Encoding Encoding { get; }

        public StringWriterWithEncoding(Encoding encoding)
        {
            Encoding = encoding;
        }
    }

    private static Ogc.Wms.Layer ToCapabilitiesLayer(Layer layer)
    {
        var epsg = "EPSG:" + layer.DataSource.GetEPSGCode();
        var capLayer = new Ogc.Wms.Layer
        {
            Name = layer.Name,
            Title = layer.Title
        };
        capLayer.CRS.Add(epsg);
        var e = layer.DataSource.GetExtent();
        var bbox = new Ogc.Wms.BoundingBox
        {
            CRS = epsg,
            minx = (float)e.MinX,
            miny = (float)e.MinY,
            maxx = (float)e.MaxX,
            maxy = (float)e.MaxY
        };
        capLayer.BoundingBox.Add(bbox);
        return capLayer;
    }

    Ogc.Wms.Capabilities GenerateCapabilities()
    {
        var capabilities = new Ogc.Wms.Capabilities();
        var rootLayer = new Ogc.Wms.Layer
        {
            LayerChildren = context.Layers.Values.Select(l => ToCapabilitiesLayer(l)).ToList()
        };
        capabilities.Capability.Layer = rootLayer;
        capabilities.Capability.Request.GetCapabilities.DCPType.HTTP.Get.OnlineResource.href = context.Config.Host ?? "http://localhost:5000/wms?service=WMS";
        capabilities.Capability.Request.GetMap.DCPType.HTTP.Get.OnlineResource.href = context.Config.Host ?? "http://localhost:5000/wms?service=WMS";
        return capabilities;
    }

    public async Task StreamResponseAsync(Stream stream)
    {
        logger.LogTrace("Streaming cached capabilities XML");
        using var streamWriter = new StreamWriter(stream);
        await streamWriter.WriteAsync(capabilitiesCache);
        await streamWriter.FlushAsync();
        await streamWriter.DisposeAsync();
    }
}
