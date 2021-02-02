using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using SkiaSharp;

namespace CoreWms
{
    public class LayerRenderer
    {
        readonly int height;
        readonly double ox;
        readonly double oy;
        readonly double rx;
        readonly double ry;

        readonly SKImageInfo info;
        readonly SKBitmap bitmap;
        readonly SKCanvas canvas;

        public SKBitmap Bitmap => bitmap;

        public double Tolerance => 0.5f * rx;

        readonly Symbolizer defaultSymbolizer = new() {
            Fill = new Option<SKPaint>(new SKPaint
            {
                Color = new SKColor(0, 0, 255, 150),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            }),
            Stroke = new Option<SKPaint>(new SKPaint
            {
                Color = new SKColor(0, 0, 255, 255),
                StrokeWidth = 1.2f,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            })
        };

        public LayerRenderer(int width, int height, Envelope e)
        {
            this.height = height;
            ox = e.MinX;
            oy = e.MinY;
            rx = (e.MaxX - e.MinX) / width;
            ry = (e.MaxY - e.MinY) / height;

            info = new SKImageInfo(width, height);
            bitmap = new SKBitmap(info);
            canvas = new SKCanvas(bitmap);
        }

        public void Draw(in Layer l, IFeature f)
        {
            int i;
            for (i = 0; i < l.Rules.Length; i++)
            {
                int j;
                for (j = 0; j < l.Rules[i].Filters.Length; j++)
                {
                    // TODO: this isn't nice... and might cause duplicate draws
                    if (l.Rules[i].Filters[j].Literal.Equals(f.Attributes[l.Rules[i].Filters[j].PropertyName]))
                        for (int k = 0; k < l.Rules[i].Symbolizers.Length; k++)
                            Draw(f.Geometry, l.Rules[i].Symbolizers[k]);
                }
                // no filters draw all symbolizers
                if (j == 0)
                    for (int k = 0; k < l.Rules[i].Symbolizers.Length; k++)
                        Draw(f.Geometry, l.Rules[i].Symbolizers[k]);
            }
            // not drawn by any rule so draw with default symbolizer
            if (i == 0)
                Draw(f.Geometry, defaultSymbolizer);
        }

        public void Draw(Geometry g, in Symbolizer symbolizer)
        {
            if (g is LineString ls)
                Draw(ls, symbolizer);
            else if (g is MultiLineString mls)
                Draw(mls, symbolizer);
            else if (g is Polygon p)
                Draw(p, symbolizer);
            else if (g is MultiPolygon mp)
                Draw(mp, symbolizer);
        }

        public void Draw(LineString ls, in Symbolizer symbolizer)
        {
            var path = new SKPath();
            TransformToPath(ls, path);
            Draw(path, symbolizer);
        }

        public void Draw(Polygon p, in Symbolizer symbolizer)
        {
            var path = new SKPath();
            Draw(p, path);
            Draw(path, symbolizer);
        }

        public void Draw(Polygon p, SKPath path)
        {
            TransformToPath(p.ExteriorRing, path);
            for (int i = 0; i < p.InteriorRings.Length; i++)
                TransformToPath(p.InteriorRings[i], path);
        }

        public void Draw(MultiPolygon mp, in Symbolizer symbolizer)
        {
            for (int i = 0; i < mp.Geometries.Length; i++)
                if (mp.Geometries[i] is Polygon p)
                    Draw(p, symbolizer);
        }

        public void TransformToPath(LineString ls, SKPath path)
        {
            var cs = ls.Coordinates;
            for (int i = 0; i < cs.Length; i++)
                if (i == 0)
                    path.MoveTo(ToScreenX(cs[i].X), ToScreenY(cs[i].Y));
                else
                    path.LineTo(ToScreenX(cs[i].X), ToScreenY(cs[i].Y));
        }

        public void Draw(MultiLineString mls, in Symbolizer symbolizer)
        {
            var path = new SKPath();
            for (int i = 0; i < mls.Geometries.Length; i++)
                if (mls[i] is LineString ls)
                    TransformToPath(ls, path);
            Draw(path, symbolizer);
        }

        private void DrawFill(SKPath path, SKPaint fill)
        {
            if (fill.PathEffect != null)
                DrawFillEffect(path, fill);
            else
                canvas.DrawPath(path, fill);
        }

        private void DrawFillEffect(SKPath path, SKPaint fill)
        {
            canvas.Save();
            canvas.ClipPath(path);
            var b = path.Bounds;
            // need to overdraw path effect fill to avoid render artifacts
            b.Inflate(10, 10);
            canvas.DrawRect(b, fill);
            canvas.Restore();
        }

        private void Draw(SKPath path, in Symbolizer symbolizer)
        {
            if (symbolizer.Fill.IsSome)
                DrawFill(path, symbolizer.Fill.Value);
            if (symbolizer.Stroke.IsSome)
                canvas.DrawPath(path, symbolizer.Stroke.Value);
        }

        private float ToScreenX(double x) => (float) ((x - ox) / rx);
        private float ToScreenY(double y) => (float) (height - ((y - oy) / ry));
    }
}