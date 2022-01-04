using NetTopologySuite.Geometries;

namespace CoreWms;

public struct GridCell
{
    public Envelope Bbox { get; init; }

    public int X { get; init; }
    public int Y { get; init; }

    public int Width { get; init; }
    public int Height { get; init; }

    public GridCell[] Split(int depth)
    {
        return Split(depth, Array.Empty<GridCell>());
    }

    public GridCell[] Split(int depth, GridCell[] acc)
    {
        if (depth == 1)
            return Split();
        depth--;
        var parts = Split();
        var moreParts = parts[0].Split(depth, acc).Concat(parts[1].Split(depth, acc)).ToArray();
        if (depth == 1) {
            return acc.Concat(moreParts).ToArray();
        } else {
            return moreParts;
        }
    }

    public GridCell[] Split()
    {
        if (Width > Height)
        {
            int wf = (int) Math.Floor(Width/2d);
            int wc = (int) Math.Ceiling(Width/2d);
            double mpp = Bbox.Width / Width;
            var left = new GridCell() {
                X = X,
                Y = Y,
                Width = wf,
                Height = Height,
                Bbox = new Envelope(Bbox.MinX, Bbox.MinX + (wf*mpp), Bbox.MinY, Bbox.MaxY)
            };
            var right = new GridCell() {
                X = X + wf,
                Y = Y,
                Width = wc,
                Height = Height,
                Bbox = new Envelope(Bbox.MinX + (wf * mpp), Bbox.MaxX, Bbox.MinY, Bbox.MaxY)
            };
            return new GridCell[] { left, right };
        }
        else
        {
            int hf = (int) Math.Floor(Height/2d);
            int hc = (int) Math.Ceiling(Height/2d);
            double mpp = Bbox.Height / Height;
            var up = new GridCell() {
                X = X,
                Y = Y,
                Width = Width,
                Height = hf,
                Bbox = new Envelope(Bbox.MinX, Bbox.MaxX, Bbox.MinY, Bbox.MinY + (hf * mpp))
            };
            var down = new GridCell() {
                X = X,
                Y = Y + hf,
                Width = Width,
                Height = hc,
                Bbox = new Envelope(Bbox.MinX, Bbox.MaxX, Bbox.MinY + (hf * mpp), Bbox.MaxY)
            };
            return new GridCell[] { up, down };
        }
    }
}