using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using SkiaSharp;

namespace CoreWms
{

    public enum Format
    {
        Png
    }

    public class GetMap : Request
    {
        private readonly ILogger<GetMap> logger;
        private readonly IContext context;

        public string[] Layers { get; set; }
        public string[] Styles { get; set; }

        public string Crs { get; set; }

        public Envelope Bbox { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public Format Format { get; set; }

        bool Transparent { get; set; }

        public GetMap(ILogger<GetMap> logger, IContext context)
        {
            this.logger = logger;
            this.context = context;
        }

        public void Parse(
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

            Layers = layers.Split(",");
            Styles = styles.Split(",");

            Crs = crs;

            var bboxParts = bbox.Split(",").Select(p => float.Parse(p)).ToArray();
            Bbox = new Envelope(bboxParts[0], bboxParts[2], bboxParts[1], bboxParts[3]);

            Width = width;
            Height = height;

            Format = ParseFormat(format);

            Transparent = transparent;
        }

        private Format ParseFormat(string format)
        {
            if (format == "image/png")
                return Format.Png;
            throw new Exception($"Format {format} is not supported");
        }

        public async Task StreamResponseAsync(Stream stream)
        {
            var renderers = Layers
                .Select(l => ProcessLayer(l));

            var stopwatch = Stopwatch.StartNew();
            var renders = await Task.WhenAll(renderers);

            stopwatch = Stopwatch.StartNew();
            if (renders.Length == 1)
                renders[0].Bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
            logger.LogTrace($"Encoded {Format} ({stopwatch.ElapsedMilliseconds} ms)");
            // TODO: else blend into single bitmap and encode
        }

        async Task<LayerRenderer> ProcessLayer(string layer)
        {
            context.Layers.TryGetValue(layer, out Layer serverLayer);

            if (serverLayer == null)
                throw new Exception($"Layer {layer} not available");

            var renderer = new LayerRenderer(Width, Height, Bbox);

            var stopwatch = Stopwatch.StartNew();
            await foreach (var f in serverLayer.DataSource.FetchAsync(Bbox, renderer.Tolerance))
                renderer.Draw(serverLayer, f);
            logger.LogTrace($"Rendered layer {layer} ({stopwatch.ElapsedMilliseconds} ms)");

            return renderer;
        }
    }
}