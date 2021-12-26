using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using SkiaSharp;

namespace CoreWms;

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

    public double Resolution => rx;
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
    }

    public LayerRenderer Merge(LayerRenderer r)
    {
        canvas.DrawBitmap(r.bitmap, 0f, 0f);
        return this;
    }

    public void Draw(IFeature f, Symbolizer[]? symbolizers)
    {
        for (int k = 0; k < symbolizers?.Length; k++)
            Draw(f.Geometry, ref symbolizers[k]);
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
        TransformToPath(ls, path);
        Draw(path, ref symbolizer);
    }

    public void Draw(Polygon p, ref Symbolizer symbolizer)
    {
        var path = new SKPath();
        TransformToPath(p, path);
        Draw(path, ref symbolizer);
    }

    private void TransformToPath(Polygon p, SKPath path)
    {
        TransformToPath(p.ExteriorRing, path);
        foreach (var r in p.InteriorRings)
            TransformToPath(r, path);
    }

    public void Draw(MultiPolygon mp, ref Symbolizer symbolizer)
    {
        foreach (var g in mp.Geometries)
            if (g is Polygon p)
                Draw(p, ref symbolizer);
    }

    private void TransformToPath(LineString ls, SKPath path)
    {
        var cs = ls.CoordinateSequence;
        for (int i = 0; i < cs.Count; i++)
            if (i == 0)
                path.MoveTo(ToScreenX(cs.GetX(i)), ToScreenY(cs.GetY(i)));
            else
                path.LineTo(ToScreenX(cs.GetX(i)), ToScreenY(cs.GetY(i)));
    }

    public void Draw(MultiLineString mls, ref Symbolizer symbolizer)
    {
        var path = new SKPath();
        foreach (var g in mls.Geometries)
            if (g is LineString ls)
                TransformToPath(ls, path);
        Draw(path, ref symbolizer);
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

    private void Draw(SKPath path, ref Symbolizer symbolizer)
    {
        if (symbolizer.Fill != null)
            DrawFill(path, symbolizer.Fill);
        if (symbolizer.Stroke != null)
            canvas.DrawPath(path, symbolizer.Stroke);
    }

    private float ToScreenX(double x) => (float)((x - ox) / rx);
    private float ToScreenY(double y) => (float)(height - ((y - oy) / ry));
}
