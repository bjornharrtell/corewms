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

    private static readonly SKPngEncoderOptions pngEncoderOptions = new()
    {
        ZLibLevel = 3
    };

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

    private static SKData Encode(IEnumerable<LayerRenderer> renderers)
    {
        if (renderers.Count() == 1)
            return renderers.First().Bitmap.PeekPixels().Encode(pngEncoderOptions);
        else
            return renderers.Aggregate((a, b) => a.Merge(b)).Bitmap.PeekPixels().Encode(pngEncoderOptions);
    }

    public async Task StreamResponseAsync(GetMapParameters parameters, Stream stream)
    {
        // TODO: optional concurrency by splitting into tiles?

        // TODO: make maxDegreeOfParallelism configurable
        var renderers = new DisposableEnumerable<LayerRenderer>((await parameters.Layers
            .SelectAsync(async l => await ProcessLayer(parameters, l), 4)).ToArray());

        var stopwatch = Stopwatch.StartNew();
        await Encode(renderers).AsStream().CopyToAsync(stream);
        logger.LogTrace("Encoded {Format} ({ElapsedMilliseconds} ms)", parameters.Format, stopwatch.ElapsedMilliseconds);
    }

    private static bool CheckResolution(GetMapParameters p, Rule r)
    {
        if (r.MaxResolution != null && p.Resolution > r.MaxResolution)
            return false;
        if (r.MinResolution != null && p.Resolution <= r.MinResolution)
            return false;
        return true;
    }

    struct RenderContext : IDisposable
    {
        public Rule Rule;
        public LayerRenderer Renderer;
        public bool IsVisible;
        public void Dispose() => Renderer.Dispose();
    }

    async Task<LayerRenderer> ProcessLayer(GetMapParameters parameters, string layer)
    {
        if (!context.Layers.TryGetValue(layer, out Layer serverLayer))
            throw new LayerNotDefinedException($"Layer {layer} is not defined");

        if (serverLayer.Rules == null || serverLayer.Rules.Length == 0)
            throw new Exception("Layer has no rules");

        if (parameters.Resolution > serverLayer.MaxResolution)
        {
            logger.LogTrace("Layer max resolution {} is lower than requested render resolution {}", serverLayer.MaxResolution, parameters.Resolution);
            return new LayerRenderer(parameters.Width, parameters.Height, parameters.Bbox);
        }

        // create render contexts and request first to be left undisposed
        using var renderContexts = new DisposableEnumerable<RenderContext>(serverLayer.Rules
            .Select(r => new RenderContext {
                Rule = r,
                Renderer = new LayerRenderer(parameters.Width, parameters.Height, parameters.Bbox),
                IsVisible = CheckResolution(parameters, r)
            }).ToArray(), true);

        var stopwatch = Stopwatch.StartNew();

        // fetch features and render in destination context
        // TODO: concurrency?
        await foreach (var f in serverLayer.DataSource.FetchAsync(parameters.Bbox, parameters.Tolerance))
            foreach (var renderContext in renderContexts)
                if (renderContext.IsVisible && (renderContext.Rule.Filter?.Evaluate(f) ?? true))
                    renderContext.Renderer.Draw(f, renderContext.Rule.Symbolizers);

        // merge all contexts into the first
        var first = renderContexts.First().Renderer;
        foreach (var t in renderContexts.Skip(1))
            first.Merge(t.Renderer);

        logger.LogTrace("Fetched data and rendered layer {layer} ({ElapsedMilliseconds} ms)", layer, stopwatch.ElapsedMilliseconds);

        return first;
    }
}
