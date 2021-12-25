using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using SkiaSharp;

namespace CoreWms;

public enum Format
{
    Png
}

public struct GetMapParameters
{
    public string[] Layers { get; set; }
    public string[] Styles { get; set; }

    public string Crs { get; set; }

    public Envelope Bbox { get; set; }

    public int Width { get; set; }
    public int Height { get; set; }

    public double Resolution => (Bbox.MaxX - Bbox.MinX) / Width;
    public double Tolerance => 0.5f * Resolution;

    public Format Format { get; set; }

    public bool Transparent { get; set; }
}

public class GetMap : Request
{
    private readonly ILogger<GetMap> logger;
    private readonly IContext context;

    public GetMap(ILogger<GetMap> logger, IContext context)
    {
        this.logger = logger;
        this.context = context;
    }

    public GetMapParameters ParseQueryStringParams(
        string service,
        string version,
        string request,
        string layers,
        string styles,
        string crs,
        string bbox,
        int width,
        int height,
        string format,
        bool transparent)
    {
        base.Parse(service, version, request);

        var bboxParts = bbox.Split(",").Select(p => float.Parse(p)).ToArray();

        var parameters = new GetMapParameters()
        {
            Layers = layers.Split(","),
            Styles = styles.Split(","),
            Crs = crs,
            Bbox = new Envelope(bboxParts[0], bboxParts[2], bboxParts[1], bboxParts[3]),
            Width = width,
            Height = height,
            Format = ParseFormat(format),
            Transparent = transparent
        };

        return parameters;
    }

    private static Format ParseFormat(string format)
    {
        if (format == "image/png")
            return Format.Png;
        throw new Exception($"Format {format} is not supported");
    }

    public async Task StreamResponseAsync(GetMapParameters parameters, Stream stream)
    {
        var renderers = parameters.Layers
            .Select(l => ProcessLayer(parameters, l));

        var stopwatch = Stopwatch.StartNew();
        var bitmaps = await Task.WhenAll(renderers);

        stopwatch = Stopwatch.StartNew();
        if (bitmaps.Length == 1)
            bitmaps[0].Encode(stream, SKEncodedImageFormat.Png, 100);
        logger.LogTrace("Encoded {Format} ({ElapsedMilliseconds} ms)", parameters.Format, stopwatch.ElapsedMilliseconds);
        // TODO: else blend into single bitmap and encode
    }

    private static bool CheckResolution(GetMapParameters p, Rule r)
    {
        if (r.MaxResolution != null && p.Resolution > r.MaxResolution)
            return false;
        if (r.MinResolution != null && p.Resolution <= r.MinResolution)
            return false;
        return true;
    }

    async Task<SKBitmap> ProcessLayer(GetMapParameters parameters, string layer)
    {
        if (!context.Layers.TryGetValue(layer, out Layer serverLayer))
            throw new LayerNotDefinedException($"Layer {layer} is not defined");

        if (serverLayer.Rules == null || serverLayer.Rules.Length == 0)
            throw new Exception("Layer has no rules");

        if (parameters.Resolution > serverLayer.MaxResolution)
        {
            logger.LogTrace("Layer max resolution {} is lower than requested render resolution {}", serverLayer.MaxResolution, parameters.Resolution);
            return new LayerRenderer(parameters.Width, parameters.Height, parameters.Bbox).Bitmap;
        }

        var tuples = serverLayer.Rules
            .Select(r => (Rule: r, Renderer: new LayerRenderer(parameters.Width, parameters.Height, parameters.Bbox))).ToArray();

        var stopwatch = Stopwatch.StartNew();
        await foreach (var f in serverLayer.DataSource.FetchAsync(parameters.Bbox, parameters.Tolerance))
            foreach (var t in tuples)
                if (CheckResolution(parameters, t.Rule) && (t.Rule.Filter == null || t.Rule.Filter.Evaluate(f)))
                    t.Renderer.Draw(f, t.Rule.Symbolizers);
        logger.LogTrace("Fetched data and rendered layer {layer} ({ElapsedMilliseconds} ms)", layer, stopwatch.ElapsedMilliseconds);

        var first = tuples.First().Renderer;
        foreach (var t in tuples.Skip(1))
            first.Merge(t.Renderer);

        return first.Bitmap;
    }
}
