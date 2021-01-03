using System;
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

        public SKBitmap Bitmap => bitmap;

        public double Tolerance => 0.5f * rx;

        Symbolizer defaultSymbolizer = new() {
            Fill = new SKPaint
            {
                Color = new SKColor(0, 0, 255, 150),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            },
            Stroke = new SKPaint
            {
                Color = new SKColor(0, 0, 255, 255),
                StrokeWidth = 1.2f,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            }
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

        public void Draw(ref Layer l, IFeature f)
        {
            for (int i = 0; i < l.Rules.Length; i++)
                for (int j = 0; j < l.Rules[i].Filters.Length; j++)
                    if (l.Rules[i].Filters[j].Literal.Equals(f.Attributes[l.Rules[i].Filters[j].PropertyName]))
                        for (int k = 0; k < l.Rules[i].Symbolizers.Length; k++)
                        {
                            Draw(f.Geometry, ref l.Rules[i].Symbolizers[k]);
                            return;
                        }
            Draw(f.Geometry, ref defaultSymbolizer);
        }

        public void Draw(Geometry g, ref Symbolizer symbolizer)
        {
            if (g is LineString ls)
                Draw(ls, ref symbolizer);
            else if (g is MultiLineString mls)
                Draw(mls, ref symbolizer);
            else if (g is Polygon p)
                Draw(p, ref symbolizer);
            else if (g is MultiPolygon mp)
                Draw(mp, ref symbolizer);
        }

        public void Draw(LineString ls, ref Symbolizer symbolizer)
        {
            var path = new SKPath();
            Draw(ls, path);
            Draw(path, ref symbolizer);
        }

        public void Draw(Polygon p, ref Symbolizer symbolizer)
        {
            var path = new SKPath();
            Draw(p, path);
            Draw(path, ref symbolizer);
        }

        public void Draw(Polygon p, SKPath path)
        {
            Draw(p.ExteriorRing, path);
            foreach (var r in p.InteriorRings)
                Draw(r, path);
        }

        public void Draw(MultiPolygon mp,ref Symbolizer symbolizer)
        {
            foreach (var g in mp.Geometries)
                if (g is Polygon p)
                    Draw(p, ref symbolizer);
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

        public void Draw(MultiLineString mls, ref Symbolizer symbolizer)
        {
            var path = new SKPath();
            foreach (var g in mls.Geometries)
                if (g is LineString ls)
                    Draw(ls, path);
            Draw(path, ref symbolizer);
        }

        private void Draw(SKPath path, ref Symbolizer symbolizer)
        {
            if (symbolizer.Fill != null)
            {
                if (symbolizer.Fill.PathEffect != null)
                {
                    canvas.Save();
                    canvas.ClipPath(path);
                    var b = path.Bounds;
                    b.Inflate(10, 10);
                    canvas.DrawRect(b, symbolizer.Fill);
                    canvas.Restore();
                }
                else
                {
                    canvas.DrawPath(path, symbolizer.Fill);
                }
            }
            if (symbolizer.Stroke != null)
                canvas.DrawPath(path, symbolizer.Stroke);
        }

        private float ToScreenX(double x) => (float) ((x - ox) / rx);
        private float ToScreenY(double y) => (float) (height - ((y - oy) / ry));
    }
}