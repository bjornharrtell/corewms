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

    private readonly SKPngEncoderOptions pngEncoderOptions = new() {
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

    public async Task StreamResponseAsync(GetMapParameters parameters, Stream stream)
    {
        LayerRenderer[]? renderers = null;
        try
        {
            var rendererTasks = parameters.Layers
            .Select(async l => await ProcessLayer(parameters, l));

            var stopwatch = Stopwatch.StartNew();
            renderers = await Task.WhenAll(rendererTasks);

            stopwatch = Stopwatch.StartNew();
            // TODO: renderers could be run by concurrent thread pool?
            SKData data;
            if (renderers.Length == 1)
                data = renderers[0].Bitmap.PeekPixels().Encode(pngEncoderOptions);
            else
                data = renderers.Aggregate((a, b) => a.Merge(b)).Bitmap.PeekPixels().Encode(pngEncoderOptions);
            await data.AsStream().CopyToAsync(stream);
            logger.LogTrace("Encoded {Format} ({ElapsedMilliseconds} ms)", parameters.Format, stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            if (renderers != null)
                foreach (var renderer in renderers)
                    renderer.Dispose();
        }
    }

    private static bool CheckResolution(GetMapParameters p, Rule r)
    {
        if (r.MaxResolution != null && p.Resolution > r.MaxResolution)
            return false;
        if (r.MinResolution != null && p.Resolution <= r.MinResolution)
            return false;
        return true;
    }

    struct RenderContext
    {
        public Rule Rule;
        public LayerRenderer Renderer;
        public bool IsVisible;
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

        RenderContext[]? renderContexts = null;
        try {
            renderContexts = serverLayer.Rules
                .Select(r => new RenderContext {
                    Rule = r,
                    Renderer = new LayerRenderer(parameters.Width, parameters.Height, parameters.Bbox),
                    IsVisible = CheckResolution(parameters, r)
                }).ToArray();

            var stopwatch = Stopwatch.StartNew();
            // TODO: renderers could be run by concurrent thread pool?
            await foreach (var f in serverLayer.DataSource.FetchAsync(parameters.Bbox, parameters.Tolerance))
                foreach (var renderContext in renderContexts)
                    if (renderContext.IsVisible && (renderContext.Rule.Filter?.Evaluate(f) ?? true))
                        renderContext.Renderer.Draw(f, renderContext.Rule.Symbolizers);
            logger.LogTrace("Fetched data and rendered layer {layer} ({ElapsedMilliseconds} ms)", layer, stopwatch.ElapsedMilliseconds);

            var first = renderContexts.First().Renderer;
            foreach (var t in renderContexts.Skip(1))
                first.Merge(t.Renderer);

            return first;
        }
        finally {
            if (renderContexts != null)
                foreach (var renderContext in renderContexts.Skip(1))
                    renderContext.Renderer.Dispose();
        }
    }
}
