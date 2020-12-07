using System.Linq;
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

        // TODO: multilayered rendering
        public SKPaint Fill { get; set; }
        public SKPaint Stroke { get; set; }

        public SKBitmap Bitmap => bitmap;

        public double Tolerance => 0.5f * rx;

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

            Fill = new SKPaint
            {
                Color = new SKColor(0, 0, 255, 150),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            Stroke = new SKPaint
            {
                Color = new SKColor(0, 0, 255, 255),
                StrokeWidth = 1.2f,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };
        }

        public void ApplyRules(Layer l, IFeature f)
        {
            // TODO: cache rule lookup
            var rule = l.Rules?.FirstOrDefault(r => r.Filter.Literal.Equals(f.Attributes[r.Filter.PropertyName]));
            if (rule != null)
            {
                Fill = rule.Fill;
                Stroke = rule.Stroke;
            }
        }

        public void Draw(Layer l, IFeature f)
        {
            // TODO: determine if rules need multiple draw passes
            if (l.Rules?.Count > 0)
                ApplyRules(l, f);
            Draw(f.Geometry);
        }

        public void Draw(Geometry g)
        {
            if (g is LineString ls)
                Draw(ls);
            else if (g is MultiLineString mls)
                Draw(mls);
            else if (g is Polygon p)
                Draw(p);
            else if (g is MultiPolygon mp)
                Draw(mp);
        }

        public void Draw(LineString ls)
        {
            var path = new SKPath();
            Draw(ls, path);
            Draw(path);
        }

        public void Draw(Polygon p)
        {
            var path = new SKPath();
            Draw(p, path);
            Draw(path);
        }

        public void Draw(Polygon p, SKPath path)
        {
            Draw(p.ExteriorRing, path);
            foreach (var r in p.InteriorRings)
                Draw(r, path);
        }

        public void Draw(MultiPolygon mp)
        {
            foreach (var p in mp.Geometries)
                Draw(p as Polygon);
        }

        public void Draw(LineString ls, SKPath path)
        {
            var cs = ls.Coordinates;
            for (int i = 0; i < cs.Length; i++)
                if (i == 0)
                    path.MoveTo(ToScreenX(cs[i].X), ToScreenY(cs[i].Y));
                else
                    path.LineTo(ToScreenX(cs[i].X), ToScreenY(cs[i].Y));
        }

        public void Draw(MultiLineString mls)
        {
            var path = new SKPath();
            foreach (var ls in mls.Geometries)
                Draw(ls as LineString, path);
            Draw(path);
        }

        private void Draw(SKPath path)
        {
            if (Fill != null)
            {
                if (Fill.PathEffect != null)
                {
                    canvas.Save();
                    canvas.ClipPath(path);
                    var b = path.Bounds;
                    b.Inflate(10, 10);
                    canvas.DrawRect(b, Fill);
                    canvas.Restore();
                }
                else
                {
                    canvas.DrawPath(path, Fill);
                }
            }
            if (Stroke != null)
                canvas.DrawPath(path, Stroke);
        }

        private float ToScreenX(double x) => (float) ((x - ox) / rx);
        private float ToScreenY(double y) => (float) (height - ((y - oy) / ry));
    }
}